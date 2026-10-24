"use client";
import { useActionState } from "react";
import Link from "next/link";
import { openTable } from "@/app/actions";

type State = { url?: string; error?: string };
async function action(_state: State, formData: FormData): Promise<State> {
  try { return await openTable(formData) as State; }
  catch (error) { return { error: error instanceof Error ? error.message : "เปิดโต๊ะไม่สำเร็จ" }; }
}
export function OpenTableButton({ tableId }: { tableId: number }) {
  const [state, submit, pending] = useActionState(action, {});
  return <div><form action={submit}><input type="hidden" name="tableId" value={tableId} /><button disabled={pending} className="rounded-xl bg-coffee-700 px-4 py-2 font-bold text-white disabled:opacity-50">{pending ? "กำลังเปิด…" : "เปิดโต๊ะ / QR"}</button></form>{state.error && <p className="mt-2 text-sm text-red-700">{state.error}</p>}{state.url && <><Link href={`/table/${tableId}/qr${new URL(state.url, "https://local.invalid").search}`} className="mt-2 inline-block break-all text-sm font-semibold text-coffee-700 underline">แสดงและพิมพ์ QR</Link><Link href={state.url} className="ml-3 inline-block text-sm font-semibold text-coffee-700 underline">เปิดเมนูลูกค้า</Link></>}</div>;
}
