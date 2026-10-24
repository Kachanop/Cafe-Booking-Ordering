"use client";
import { useState } from "react";
import { setKitchenStatus } from "@/app/actions";
import { RealtimeRefresh } from "@/components/RealtimeRefresh";

type KitchenOrderItem = { id: number; name: string; quantity: number; note: string | null; kdsStatus: string; tableNumber: string; orderId: number; createdAt: string };
const labels: Record<string, string> = { Pending: "รอทำ", Cooking: "กำลังทำ", Served: "เสร็จแล้ว" };

export function KitchenBoard({ items }: { items: KitchenOrderItem[] }) {
  const [view, setView] = useState<"queue" | "summary">("queue");
  const active = items.filter((item) => item.kdsStatus !== "Served");
  const summary = active.reduce<Record<string, { quantity: number; tables: Set<string>; ids: number[] }>>((all, item) => {
    const record = all[item.name] ??= { quantity: 0, tables: new Set(), ids: [] };
    record.quantity += item.quantity; record.tables.add(item.tableNumber); record.ids.push(item.id); return all;
  }, {});
  return <div><div className="flex flex-wrap items-center justify-between gap-3"><div className="flex gap-2"><button onClick={() => setView("queue")} className={`rounded-xl px-4 py-2 font-bold ${view === "queue" ? "bg-coffee-800 text-white" : "bg-white text-coffee-800"}`}>คิว FIFO</button><button onClick={() => setView("summary")} className={`rounded-xl px-4 py-2 font-bold ${view === "summary" ? "bg-coffee-800 text-white" : "bg-white text-coffee-800"}`}>รวมเมนู</button></div><RealtimeRefresh /></div>
    {view === "queue" ? <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">{active.map((item) => { const next = item.kdsStatus === "Pending" ? "Cooking" : "Served"; return <article key={item.id} className="rounded-2xl border border-coffee-100 bg-white p-5 shadow-sm"><div className="flex items-center justify-between"><h2 className="text-xl font-black text-coffee-900">{item.name} × {item.quantity}</h2><span className="rounded-full bg-coffee-50 px-3 py-1 text-xs font-bold text-coffee-700">โต๊ะ {item.tableNumber}</span></div><p className="mt-2 text-sm text-coffee-500">บิล #{item.orderId} · {new Date(item.createdAt).toLocaleTimeString("th-TH", { hour: "2-digit", minute: "2-digit" })} · {labels[item.kdsStatus]}</p>{item.note && <p className="mt-3 rounded-lg bg-amber-50 p-3 text-sm font-semibold text-amber-900">หมายเหตุ: {item.note}</p>}<form action={setKitchenStatus} className="mt-4"><input type="hidden" name="orderItemId" value={item.id} /><input type="hidden" name="status" value={next} /><button className="rounded-lg bg-coffee-800 px-4 py-2 text-sm font-bold text-white">{next === "Cooking" ? "เริ่มทำ" : "เสร็จพร้อมเสิร์ฟ"}</button></form></article>; })}</div> : <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">{Object.entries(summary).map(([name, row]) => <article key={name} className="rounded-2xl bg-white p-6 shadow-sm"><p className="text-sm font-bold text-coffee-500">สรุปรายการที่ต้องทำ</p><h2 className="mt-2 text-2xl font-black text-coffee-900">{name}</h2><p className="mt-3 text-4xl font-black text-coffee-700">× {row.quantity}</p><p className="mt-2 text-sm text-coffee-600">โต๊ะ {Array.from(row.tables).join(", ")}</p><div className="mt-4 flex flex-wrap gap-2">{row.ids.map((id) => { const item = active.find((entry) => entry.id === id)!; const next = item.kdsStatus === "Pending" ? "Cooking" : "Served"; return <form key={id} action={setKitchenStatus}><input type="hidden" name="orderItemId" value={id} /><input type="hidden" name="status" value={next} /><button className="rounded-lg bg-coffee-700 px-3 py-2 text-sm font-bold text-white">{next === "Cooking" ? "เริ่มทำ" : "เสร็จ"} #{id}</button></form>; })}</div></article>)}</div>}
    {active.length === 0 && <div className="mt-8 rounded-2xl bg-white p-12 text-center text-coffee-600">ยังไม่มีออเดอร์ที่ต้องทำ</div>}
  </div>;
}
