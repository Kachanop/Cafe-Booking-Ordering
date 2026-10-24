import Link from "next/link";
import { prisma } from "@/lib/prisma";
import { requireActor } from "@/lib/auth";
import { KitchenBoard } from "@/components/KitchenBoard";

export default async function KitchenPage() {
  const actor = await requireActor(["kitchen", "manager", "owner"]);
  const rows = await prisma.orderItem.findMany({
    where: { order: { status: { in: ["Dining", "Billing"] } } },
    include: { menuItem: true, order: { include: { table: true } } },
    orderBy: [{ order: { createdAt: "asc" } }, { id: "asc" }],
  });
  const items = rows.map((row) => ({ id: row.id, name: row.menuItem.name, quantity: row.quantity, note: row.note, kdsStatus: row.kdsStatus, tableNumber: row.order.table.tableNumber, orderId: row.orderId, createdAt: row.order.createdAt.toISOString() }));
  return <main className="min-h-screen bg-coffee-50 px-4 py-8 md:px-8"><div className="mx-auto max-w-7xl"><div className="mb-8 flex items-center justify-between"><div><Link href="/" className="text-sm font-semibold text-coffee-600">Cafe & Studio</Link><h1 className="mt-2 text-3xl font-black text-coffee-900">Kitchen Display System</h1><p className="mt-1 text-coffee-600">สวัสดี {actor.profile.fullName} · เรียงคิวตามเวลาสั่ง</p></div><Link href="/cashier" className="rounded-xl bg-white px-4 py-2 font-bold text-coffee-800">แคชเชียร์</Link></div><KitchenBoard items={items} /></div></main>;
}
