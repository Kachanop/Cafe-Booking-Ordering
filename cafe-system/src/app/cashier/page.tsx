import Link from "next/link";
import { prisma } from "@/lib/prisma";
import { requireActor } from "@/lib/auth";
import { closeTable } from "@/app/actions";
import { RealtimeRefresh } from "@/components/RealtimeRefresh";
import { OpenTableButton } from "@/components/OpenTableButton";
import { CashierAlerts } from "@/components/CashierAlerts";
import { normalizeRole } from "@/lib/auth";

const money = (value: number) => new Intl.NumberFormat("th-TH", { style: "currency", currency: "THB", maximumFractionDigits: 0 }).format(value);

export default async function CashierPage() {
  const actor = await requireActor(["cashier", "waiter", "manager", "owner"]);
  const isCashier = normalizeRole(actor.profile.role.name) !== "waiter";
  const [tables, bills] = await Promise.all([
    prisma.tableCafe.findMany({ orderBy: { tableNumber: "asc" } }),
    prisma.order.findMany({ where: { status: "Billing", table: { billRequestedAt: { not: null } } }, include: { table: true, orderItems: { include: { menuItem: true } } }, orderBy: { createdAt: "asc" } }),
  ]);

  return <main className="min-h-screen bg-coffee-50 px-4 py-8 md:px-8"><div className="mx-auto max-w-7xl">
    <div className="flex flex-wrap items-center justify-between gap-4"><div><Link href="/" className="text-sm font-semibold text-coffee-600">Cafe & Studio</Link><h1 className="mt-2 text-3xl font-black text-coffee-900">แคชเชียร์และจัดการโต๊ะ</h1><p className="mt-1 text-coffee-600">สวัสดี {actor.profile.fullName}</p></div><div className="flex flex-wrap items-center gap-3"><CashierAlerts /><RealtimeRefresh /></div></div>
    {isCashier && bills.length > 0 && <section className="mt-7"><h2 className="text-xl font-black text-coffee-900">🔔 เรียกเก็บเงิน ({bills.length})</h2><div className="mt-3 grid gap-4 lg:grid-cols-2">{bills.map((order) => <article key={order.id} className="rounded-2xl border-2 border-amber-300 bg-amber-50 p-5"><div className="flex items-start justify-between"><div><h3 className="text-xl font-black text-coffee-900">โต๊ะ {order.table.tableNumber}</h3><p className="text-sm text-amber-900">เรียกเมื่อ {order.table.billRequestedAt?.toLocaleTimeString("th-TH", { hour: "2-digit", minute: "2-digit" })}</p></div><strong className="text-2xl text-coffee-900">{money(order.totalAmount)}</strong></div><ul className="mt-4 space-y-1 text-sm text-coffee-800">{order.orderItems.map((item) => <li key={item.id}>{item.menuItem.name} × {item.quantity}{item.note ? ` — ${item.note}` : ""}</li>)}</ul><form action={closeTable} className="mt-5 flex flex-wrap items-center gap-3"><input type="hidden" name="tableId" value={order.tableId} /><label className="grid gap-1 text-sm font-semibold">รับชำระด้วย<select name="method" className="rounded-lg border border-coffee-200 bg-white px-3 py-2"><option value="CASH">เงินสด</option><option value="PROMPTPAY">PromptPay (ตรวจสอบแล้ว)</option></select></label><button className="rounded-xl bg-coffee-800 px-5 py-3 font-bold text-white">รับชำระและปิดบิล</button></form></article>)}</div></section>}
    <section className="mt-9"><h2 className="text-xl font-black text-coffee-900">โต๊ะทั้งหมด</h2><div className="mt-3 grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">{tables.map((table) => <article key={table.id} className="rounded-2xl bg-white p-5 shadow-sm"><div className="flex items-center justify-between"><h3 className="text-xl font-black text-coffee-900">โต๊ะ {table.tableNumber}</h3><span className={`rounded-full px-3 py-1 text-xs font-bold ${table.currentSession ? "bg-emerald-100 text-emerald-800" : "bg-stone-100 text-stone-600"}`}>{table.currentSession ? "กำลังใช้งาน" : "ว่าง"}</span></div>{table.currentSession ? <><Link href={`/table/${table.id}/qr?session=${encodeURIComponent(table.currentSession)}`} className="mt-4 inline-block text-sm font-semibold text-coffee-700 underline">แสดงและพิมพ์ QR</Link><br /><Link href={`/table/${table.id}?session=${encodeURIComponent(table.currentSession)}`} className="mt-2 inline-block text-sm font-semibold text-coffee-700 underline">เปิดเมนูลูกค้า</Link></> : <div className="mt-4"><OpenTableButton tableId={table.id} /></div>}</article>)}</div>{tables.length === 0 && <p className="mt-4 rounded-xl bg-white p-6 text-coffee-600">ยังไม่มีโต๊ะ กำหนดโต๊ะได้ที่หน้าผู้จัดการ</p>}</section>
  </div></main>;
}
