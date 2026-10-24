"use server";

import { randomUUID } from "node:crypto";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { prisma } from "@/lib/prisma";
import { getActor, requireActor, ensureMemberProfile, type StaffRole } from "@/lib/auth";
import { createClient } from "@/lib/supabase/server";
import { getBangkokDate } from "@/lib/date";

const managers: StaffRole[] = ["manager", "owner"];
const cashiers: StaffRole[] = ["cashier", "manager", "owner"];
const kitchen: StaffRole[] = ["kitchen", "manager", "owner"];

async function notifyRealtime(event: "refresh" | "bill" = "refresh") {
  try {
    const supabase = await createClient();
    const channel = supabase.channel("cafe-live-refresh");
    await channel.send({ type: "broadcast", event, payload: {} });
    await supabase.removeChannel(channel);
  } catch {
    // Refresh polling remains available if the Realtime service is offline.
  }
}

export async function logout() {
  const supabase = await createClient();
  await supabase.auth.signOut();
  revalidatePath("/", "layout");
  redirect("/");
}

export async function createBooking(formData: FormData) {
  const actor = await requireActor(["member", "artist", "waiter", "manager", "owner"]);
  const staffId = Number(formData.get("staffId"));
  const serviceId = Number(formData.get("serviceId"));
  const dateValue = String(formData.get("date") ?? "");
  const timeSlot = String(formData.get("timeSlot") ?? "");
  const allowedSlots = ["10:00-12:00", "13:00-15:00", "15:00-17:00", "17:00-19:00"];
  const date = new Date(`${dateValue}T00:00:00.000Z`);
  if (!Number.isInteger(staffId) || !Number.isInteger(serviceId) || !/^\d{4}-\d{2}-\d{2}$/.test(dateValue) || Number.isNaN(date.valueOf()) || date.toISOString().slice(0, 10) !== dateValue || dateValue < getBangkokDate() || !allowedSlots.includes(timeSlot)) {
    redirect("/booking?error=" + encodeURIComponent("ข้อมูลการจองไม่ถูกต้อง"));
  }
  const [staff, service] = await Promise.all([
    prisma.user.findFirst({ where: { id: staffId, staffProfile: { isNot: null }, role: { name: { equals: "Artist", mode: "insensitive" } } } }),
    prisma.service.findUnique({ where: { id: serviceId } }),
  ]);
  if (!staff || !service) redirect("/booking?error=" + encodeURIComponent("ไม่พบบริการหรือช่างที่เลือก"));
  try {
    await prisma.booking.create({ data: { customerId: actor.profile.id, staffId, serviceId, bookingDate: date, timeSlot, totalPrice: service.price, customerNote: String(formData.get("note") ?? "").slice(0, 500), status: "Pending" } });
  } catch (error) {
    if ((error as { code?: string }).code === "P2002") redirect("/booking?error=" + encodeURIComponent("รอบเวลานี้เพิ่งถูกจอง กรุณาเลือกเวลาอื่น"));
    throw error;
  }
  revalidatePath("/booking");
  revalidatePath("/my-bookings");
  await notifyRealtime();
  redirect("/my-bookings?message=" + encodeURIComponent("บันทึกคำขอจองแล้ว กรุณาชำระเงินเต็มจำนวนเพื่อยืนยันคิว"));
}

export async function createOrder(formData: FormData) {
  const tableId = Number(formData.get("tableId"));
  const session = String(formData.get("session") ?? "");
  const rawItems = String(formData.get("items") ?? "[]");
  let items: Array<{ id: number; quantity: number; note?: string }>;
  try { items = JSON.parse(rawItems); } catch { throw new Error("ตะกร้าสินค้าไม่ถูกต้อง"); }
  if (!Number.isInteger(tableId) || !session || !Array.isArray(items) || items.length < 1 || items.length > 30) throw new Error("ข้อมูลคำสั่งซื้อไม่ถูกต้อง");
  const normalized = items.map((item) => ({ id: Number(item.id), quantity: Number(item.quantity), note: String(item.note ?? "").slice(0, 180) }));
  if (normalized.some((item) => !Number.isInteger(item.id) || !Number.isInteger(item.quantity) || item.quantity < 1 || item.quantity > 20)) throw new Error("จำนวนสินค้าไม่ถูกต้อง");

  await prisma.$transaction(async (tx) => {
    const locked = await tx.$queryRaw<Array<{ id: number; currentSession: string | null }>>`SELECT id, "currentSession" FROM "TableCafe" WHERE id = ${tableId} FOR UPDATE`;
    if (!locked[0] || locked[0].currentSession !== session) throw new Error("QR Code หมดอายุแล้ว กรุณาเรียกพนักงาน");
    const menu = await tx.menuItem.findMany({ where: { id: { in: normalized.map((item) => item.id) }, isAvailable: true } });
    if (menu.length !== new Set(normalized.map((item) => item.id)).size) throw new Error("มีเมนูที่หมดหรือไม่พร้อมจำหน่ายในตะกร้า");
    let order = await tx.order.findFirst({ where: { tableId, status: { in: ["Dining", "Billing"] } }, orderBy: { id: "desc" } });
    if (!order) order = await tx.order.create({ data: { tableId, status: "Dining" } });
    for (const item of normalized) await tx.orderItem.create({ data: { orderId: order.id, menuItemId: item.id, quantity: item.quantity, note: item.note || null } });
    const currentItems = await tx.orderItem.findMany({ where: { orderId: order.id }, include: { menuItem: true } });
    const totalAmount = currentItems.reduce((sum, item) => sum + item.menuItem.price * item.quantity, 0);
    await tx.order.update({ where: { id: order.id }, data: { totalAmount, status: "Dining" } });
    await tx.tableCafe.update({ where: { id: tableId }, data: { billRequestedAt: null } });
  });
  revalidatePath("/kds");
  revalidatePath("/cashier");
  await notifyRealtime();
}

export async function requestBill(formData: FormData) {
  const tableId = Number(formData.get("tableId"));
  const session = String(formData.get("session") ?? "");
  if (!Number.isInteger(tableId) || !session) throw new Error("QR Code หมดอายุแล้ว");
  await prisma.$transaction(async (tx) => {
    const locked = await tx.$queryRaw<Array<{ id: number; currentSession: string | null }>>`SELECT id, "currentSession" FROM "TableCafe" WHERE id = ${tableId} FOR UPDATE`;
    if (!locked[0] || locked[0].currentSession !== session) throw new Error("QR Code หมดอายุแล้ว");
    const openOrders = await tx.order.count({ where: { tableId, status: "Dining" } });
    if (!openOrders) throw new Error("โต๊ะนี้ยังไม่มีรายการสั่งซื้อ");
    await tx.tableCafe.update({ where: { id: tableId }, data: { billRequestedAt: new Date() } });
    await tx.order.updateMany({ where: { tableId, status: "Dining" }, data: { status: "Billing" } });
  });
  revalidatePath("/cashier");
  await notifyRealtime("bill");
}

export async function setKitchenStatus(formData: FormData) {
  await requireActor(kitchen);
  const id = Number(formData.get("orderItemId"));
  const next = String(formData.get("status"));
  if (!Number.isInteger(id) || !["Pending", "Cooking", "Served"].includes(next)) throw new Error("สถานะไม่ถูกต้อง");
  const item = await prisma.orderItem.findUnique({ where: { id }, select: { kdsStatus: true } });
  const expectedNext = item?.kdsStatus === "Pending" ? "Cooking" : item?.kdsStatus === "Cooking" ? "Served" : null;
  if (!item || next !== expectedNext) throw new Error("เปลี่ยนสถานะได้ครั้งละหนึ่งขั้นเท่านั้น");
  const transition = await prisma.orderItem.updateMany({ where: { id, kdsStatus: item.kdsStatus }, data: { kdsStatus: next } });
  if (transition.count !== 1) throw new Error("สถานะออเดอร์เปลี่ยนไปแล้ว กรุณาโหลดหน้าใหม่");
  revalidatePath("/kds");
  revalidatePath("/cashier");
  await notifyRealtime();
}

export async function openTable(formData: FormData) {
  await requireActor(["waiter", "cashier", "manager", "owner"]);
  const id = Number(formData.get("tableId"));
  if (!Number.isInteger(id)) throw new Error("ไม่พบโต๊ะ");
  const token = randomUUID();
  const updated = await prisma.tableCafe.updateMany({ where: { id, currentSession: null }, data: { currentSession: token, openedAt: new Date(), billRequestedAt: null } });
  if (updated.count !== 1) throw new Error("โต๊ะไม่ว่างหรือไม่พบโต๊ะ");
  revalidatePath("/cashier");
  revalidatePath("/admin");
  await notifyRealtime();
  return { url: `/table/${id}?session=${token}` };
}

export async function closeTable(formData: FormData) {
  await requireActor(cashiers);
  const id = Number(formData.get("tableId"));
  const method = String(formData.get("method") ?? "CASH");
  if (!["CASH", "PROMPTPAY"].includes(method)) throw new Error("ช่องทางชำระเงินไม่ถูกต้อง");
  await prisma.$transaction(async (tx) => {
    const locked = await tx.$queryRaw<Array<{ id: number; currentSession: string | null }>>`SELECT id, "currentSession" FROM "TableCafe" WHERE id = ${id} FOR UPDATE`;
    if (!locked[0]?.currentSession) throw new Error("โต๊ะนี้ไม่มีเซสชันที่เปิดอยู่");
    const order = await tx.order.findFirst({ where: { tableId: id, status: "Billing" }, orderBy: { id: "desc" } });
    if (!order) throw new Error("โต๊ะนี้ยังไม่ได้เรียกเก็บเงิน");
    const unserved = await tx.orderItem.count({ where: { orderId: order.id, kdsStatus: { not: "Served" } } });
    if (unserved) throw new Error("ยังมีรายการอาหารที่ห้องครัวทำไม่เสร็จ");
    const changed = await tx.order.updateMany({ where: { id: order.id, status: "Billing" }, data: { status: "Paid" } });
    if (changed.count !== 1) throw new Error("บิลนี้ถูกปิดไปแล้ว");
    await tx.payment.create({ data: { referenceId: order.id, referenceType: "ORDER", amount: order.totalAmount, method, status: "Success" } });
    await tx.tableCafe.update({ where: { id }, data: { currentSession: null, openedAt: null, billRequestedAt: null } });
  });
  revalidatePath("/cashier");
  revalidatePath("/admin");
  await notifyRealtime();
}

export async function updateMenu(formData: FormData) {
  await requireActor(managers);
  const id = Number(formData.get("id"));
  const price = Number(formData.get("price"));
  const isAvailable = formData.get("isAvailable") === "on";
  if (!Number.isInteger(id) || !Number.isFinite(price) || price < 0) throw new Error("ราคาไม่ถูกต้อง");
  await prisma.menuItem.update({ where: { id }, data: { price, isAvailable } });
  revalidatePath("/"); revalidatePath("/admin");
  await notifyRealtime();
}

export async function createMenuItem(formData: FormData) {
  await requireActor(managers);
  const categoryId = Number(formData.get("categoryId"));
  const name = String(formData.get("name") ?? "").trim();
  const price = Number(formData.get("price"));
  if (!Number.isInteger(categoryId) || !name || name.length > 100 || !Number.isFinite(price) || price < 0) throw new Error("ข้อมูลเมนูไม่ถูกต้อง");
  await prisma.menuItem.create({ data: { categoryId, name, price } });
  revalidatePath("/"); revalidatePath("/admin");
}

export async function createMenuCategory(formData: FormData) {
  await requireActor(managers);
  const name = String(formData.get("name") ?? "").trim();
  if (!name || name.length > 60) throw new Error("ชื่อหมวดหมู่ไม่ถูกต้อง");
  const existing = await prisma.menuCategory.findFirst({ where: { name: { equals: name, mode: "insensitive" } } });
  if (!existing) await prisma.menuCategory.create({ data: { name } });
  revalidatePath("/"); revalidatePath("/admin");
  await notifyRealtime();
}

export async function createTable(formData: FormData) {
  await requireActor(managers);
  const tableNumber = String(formData.get("tableNumber") ?? "").trim();
  if (!/^[A-Za-z0-9-]{1,12}$/.test(tableNumber)) throw new Error("หมายเลขโต๊ะไม่ถูกต้อง");
  await prisma.tableCafe.create({ data: { tableNumber } });
  revalidatePath("/admin"); revalidatePath("/cashier");
  await notifyRealtime();
}

export async function createService(formData: FormData) {
  await requireActor(managers);
  const name = String(formData.get("name") ?? "").trim();
  const price = Number(formData.get("price"));
  if (!name || name.length > 100 || !Number.isFinite(price) || price < 0) throw new Error("ข้อมูลบริการไม่ถูกต้อง");
  await prisma.service.create({ data: { name, price } });
  revalidatePath("/booking"); revalidatePath("/admin");
  await notifyRealtime();
}

export async function cancelBooking(formData: FormData) {
  const actor = await requireActor();
  const id = Number(formData.get("bookingId"));
  await prisma.booking.updateMany({ where: { id, customerId: actor.profile.id, status: "Pending" }, data: { status: "Cancelled" } });
  revalidatePath("/my-bookings"); revalidatePath("/booking");
  await notifyRealtime();
}

export async function markBookingPaid(formData: FormData) {
  await requireActor(["cashier", "manager", "owner"]);
  const id = Number(formData.get("bookingId"));
  const method = String(formData.get("method") ?? "CASH");
  if (!["CASH", "PROMPTPAY"].includes(method)) throw new Error("ช่องทางชำระเงินไม่ถูกต้อง");
  const booking = await prisma.booking.findUnique({ where: { id } });
  if (!booking || booking.status !== "Pending") throw new Error("ไม่พบรายการค้างชำระ");
  await prisma.$transaction(async (tx) => {
    const changed = await tx.booking.updateMany({ where: { id, status: "Pending" }, data: { status: "Paid" } });
    if (changed.count !== 1) throw new Error("รายการนี้ถูกยืนยันหรือยกเลิกไปแล้ว");
    await tx.payment.create({ data: { referenceId: id, referenceType: "BOOKING", amount: booking.totalPrice, method, status: "Success" } });
  });
  revalidatePath("/admin"); revalidatePath("/my-bookings"); revalidatePath("/artist-dashboard");
  await notifyRealtime();
}

export async function completeBooking(formData: FormData) {
  const actor = await requireActor(["artist", "manager", "owner"]);
  const id = Number(formData.get("bookingId"));
  await prisma.booking.updateMany({ where: { id, staffId: actor.profile.id, status: "Paid" }, data: { status: "Completed" } });
  revalidatePath("/artist-dashboard"); revalidatePath("/admin");
  await notifyRealtime();
}

export async function assignStaffRole(formData: FormData) {
  const actor = await requireActor(["owner"]);
  const userId = Number(formData.get("userId"));
  const roleName = String(formData.get("role") ?? "");
  const allowed = ["Member", "Artist", "Waiter", "Kitchen", "Cashier", "Manager", "Owner", "IT Staff"];
  if (!Number.isInteger(userId) || userId === actor.profile.id || !allowed.includes(roleName)) throw new Error("กำหนดสิทธิ์ไม่ถูกต้อง");
  const [user, role] = await Promise.all([prisma.user.findUnique({ where: { id: userId } }), prisma.role.upsert({ where: { name: roleName }, update: {}, create: { name: roleName } })]);
  if (!user) throw new Error("ไม่พบบัญชีผู้ใช้");
  await prisma.user.update({ where: { id: userId }, data: { roleId: role.id } });
  revalidatePath("/admin");
  await notifyRealtime();
}

export async function registerMember(formData: FormData) {
  const email = String(formData.get("email") ?? "").trim().toLowerCase();
  const password = String(formData.get("password") ?? "");
  const fullName = String(formData.get("fullName") ?? "").trim();
  if (!email || password.length < 8 || !fullName) redirect("/signup?error=" + encodeURIComponent("กรอกชื่อ อีเมล และรหัสผ่านอย่างน้อย 8 ตัวอักษร"));
  const supabase = await createClient();
  const { data, error } = await supabase.auth.signUp({ email, password, options: { data: { full_name: fullName, role: "member" } } });
  if (error) redirect("/signup?error=" + encodeURIComponent(error.message));
  if (data.user && data.session) await ensureMemberProfile(data.user);
  revalidatePath("/", "layout");
  redirect(data.session ? "/my-bookings" : "/login?message=" + encodeURIComponent("สมัครสมาชิกแล้ว กรุณายืนยันอีเมลก่อนเข้าสู่ระบบ"));
}

export async function currentUserRole() {
  const actor = await getActor();
  return actor?.profile.role.name.toLowerCase() ?? null;
}

