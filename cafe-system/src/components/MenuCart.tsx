"use client";

import Image from "next/image";
import { useActionState, useState } from "react";
import { createOrder } from "@/app/actions";

type Menu = { id: number; name: string; price: number; isAvailable: boolean; category: string };
type CartLine = { id: number; quantity: number; note: string };
type State = { message?: string; error?: string };

async function submitOrder(_state: State, formData: FormData): Promise<State> {
  try {
    await createOrder(formData);
    return { message: "ส่งรายการไปที่ครัวแล้ว" };
  } catch (error) {
    return { error: error instanceof Error ? error.message : "สั่งอาหารไม่สำเร็จ" };
  }
}

function artworkFor(category: string) {
  const name = category.toLowerCase();
  if (name.includes("bakery") || name.includes("dessert") || name.includes("เบเกอรี่") || name.includes("ของหวาน")) return "/menu/bakery.svg";
  if (name.includes("coffee") || name.includes("กาแฟ")) return "/menu/coffee.svg";
  return "/menu/drinks.svg";
}

function OrderFields({ tableId, session, cart }: { tableId: number; session: string; cart: CartLine[] }) {
  return <><input type="hidden" name="tableId" value={tableId} /><input type="hidden" name="session" value={session} /><input type="hidden" name="items" value={JSON.stringify(cart)} /></>;
}

export function MenuCart({ tableId, session, items }: { tableId: number; session: string; items: Menu[] }) {
  const [cart, setCart] = useState<CartLine[]>([]);
  const [state, action, pending] = useActionState(submitOrder, {});
  const add = (id: number) => setCart((lines) => {
    const current = lines.find((line) => line.id === id);
    return current
      ? lines.map((line) => line.id === id ? { ...line, quantity: Math.min(20, line.quantity + 1) } : line)
      : [...lines, { id, quantity: 1, note: "" }];
  });
  const change = (id: number, value: number) => setCart((lines) => value <= 0
    ? lines.filter((line) => line.id !== id)
    : lines.map((line) => line.id === id ? { ...line, quantity: Math.min(20, value) } : line));
  const total = cart.reduce((sum, line) => sum + (items.find((item) => item.id === line.id)?.price ?? 0) * line.quantity, 0);
  const count = cart.reduce((sum, line) => sum + line.quantity, 0);
  const grouped = items.reduce<Record<string, Menu[]>>((all, item) => { (all[item.category] ??= []).push(item); return all; }, {});
  const orderLabel = cart.length ? `ยืนยันออเดอร์ · ${count} รายการ · ฿${total}` : "ยืนยันออเดอร์ · เลือกเมนูก่อน";

  return <div className="mt-7 grid items-start gap-6 pb-24 lg:grid-cols-[1fr_360px] lg:pb-0">
    <section className="space-y-8">
      {Object.entries(grouped).map(([category, menus]) => <div key={category}>
        <h2 className="border-b-2 border-coffee-200 pb-2 text-2xl font-black text-coffee-900">{category}</h2>
        <div className="mt-4 grid gap-4 sm:grid-cols-2">
          {menus.map((item) => <article key={item.id} className="overflow-hidden rounded-2xl bg-white shadow-sm">
            <Image src={artworkFor(category)} alt={`ภาพประกอบเมนูหมวด${category}`} width={640} height={400} className="aspect-[16/8] w-full object-cover" />
            <div className="flex items-center justify-between gap-4 p-4">
              <div><h3 className="font-bold text-coffee-900">{item.name}</h3><p className="mt-1 font-semibold text-coffee-700">฿{item.price}</p>{!item.isAvailable && <p className="text-xs font-bold text-red-600">หมดชั่วคราว</p>}</div>
              <button type="button" disabled={!item.isAvailable} onClick={() => add(item.id)} className="shrink-0 rounded-full bg-coffee-700 px-4 py-2 font-black text-white disabled:bg-stone-300">เพิ่ม +</button>
            </div>
          </article>)}
        </div>
      </div>)}
    </section>

    <aside id="cafe-cart" className="rounded-3xl bg-white p-5 shadow-lg lg:sticky lg:top-5">
      <h2 className="text-xl font-black text-coffee-900">ตะกร้าของคุณ</h2>
      <form action={action}>
        <OrderFields tableId={tableId} session={session} cart={cart} />
        {cart.length === 0 ? <p className="mt-4 text-sm text-coffee-500">เลือกเมนูเพื่อเริ่มสั่ง</p> : <div className="mt-4 space-y-4">{cart.map((line) => {
          const item = items.find((menu) => menu.id === line.id)!;
          return <div key={line.id} className="border-b border-coffee-100 pb-3">
            <div className="flex items-start justify-between gap-2"><span className="font-semibold text-coffee-900">{item.name}</span><span className="font-bold text-coffee-800">฿{item.price * line.quantity}</span></div>
            <div className="mt-2 flex items-center gap-2"><button type="button" aria-label={`ลด ${item.name}`} onClick={() => change(line.id, line.quantity - 1)} className="h-8 w-8 rounded-lg bg-coffee-50 font-bold">−</button><span className="w-6 text-center font-bold">{line.quantity}</span><button type="button" aria-label={`เพิ่ม ${item.name}`} onClick={() => change(line.id, line.quantity + 1)} className="h-8 w-8 rounded-lg bg-coffee-50 font-bold">+</button></div>
            <input aria-label={`หมายเหตุ ${item.name}`} placeholder="หมายเหตุ เช่น หวานน้อย" value={line.note} onChange={(event) => setCart((lines) => lines.map((row) => row.id === line.id ? { ...row, note: event.target.value.slice(0, 180) } : row))} className="mt-2 w-full rounded-lg border border-coffee-100 px-3 py-2 text-sm" />
          </div>;
        })}</div>}
        <div className="mt-5 flex justify-between border-t border-coffee-100 pt-4 font-black text-coffee-900"><span>รวม</span><span>฿{total}</span></div>
        <button type="submit" disabled={!cart.length || pending} className="mt-4 hidden w-full rounded-xl bg-coffee-800 px-4 py-3 font-bold text-white disabled:cursor-not-allowed disabled:opacity-60 lg:block">{pending ? "กำลังส่ง…" : orderLabel}</button>
        {state.error && <p role="alert" className="mt-3 text-sm font-semibold text-red-700">{state.error}</p>}
        {state.message && <p role="status" className="mt-3 text-sm font-semibold text-emerald-700">{state.message}</p>}
      </form>
    </aside>

    <div className="fixed inset-x-0 bottom-0 z-40 border-t border-coffee-100 bg-white/95 p-3 shadow-[0_-8px_24px_rgba(69,40,20,0.12)] backdrop-blur lg:hidden">
      <form action={action} className="mx-auto flex max-w-2xl items-center gap-3">
        <OrderFields tableId={tableId} session={session} cart={cart} />
        <p className="min-w-20 text-sm font-bold text-coffee-800">{count} รายการ<br />฿{total}</p>
        <button type="submit" disabled={!cart.length || pending} className="flex-1 rounded-xl bg-coffee-800 px-4 py-3 font-bold text-white disabled:cursor-not-allowed disabled:opacity-60">{pending ? "กำลังส่ง…" : orderLabel}</button>
      </form>
    </div>
  </div>;
}
