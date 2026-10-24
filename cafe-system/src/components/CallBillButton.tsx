"use client";
import { useActionState } from "react";
import { requestBill } from "@/app/actions";

type State = { message?: string; error?: string };
async function submit(_state: State, formData: FormData): Promise<State> {
  try { await requestBill(formData); return { message: "แจ้งพนักงานแล้ว กรุณารอสักครู่" }; }
  catch (error) { return { error: error instanceof Error ? error.message : "ส่งคำขอไม่สำเร็จ" }; }
}

export function CallBillButton({ tableId, session }: { tableId: number; session: string }) {
  const [state, action, pending] = useActionState(submit, {});
  return <div className="text-right"><form action={action}><input type="hidden" name="tableId" value={tableId} /><input type="hidden" name="session" value={session} /><button disabled={pending || Boolean(state.message)} className="rounded-xl bg-amber-300 px-5 py-3 font-black text-coffee-900 disabled:opacity-60">{pending ? "กำลังแจ้ง…" : state.message ? "เรียกเก็บเงินแล้ว" : "เรียกเก็บเงิน"}</button></form>{state.error && <p role="alert" className="mt-2 text-xs font-semibold text-red-100">{state.error}</p>}{state.message && <p role="status" className="mt-2 text-xs text-coffee-100">{state.message}</p>}</div>;
}
