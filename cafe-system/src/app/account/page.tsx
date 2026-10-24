import Link from "next/link";
import { redirect } from "next/navigation";
import { logout } from "@/app/actions";
import { getActor } from "@/lib/auth";
import { updateAccount } from "./actions";

export default async function AccountPage({ searchParams }: { searchParams: Promise<{ message?: string; error?: string }> }) {
  const actor = await getActor();
  if (!actor) redirect("/login?error=" + encodeURIComponent("กรุณาเข้าสู่ระบบก่อน"));
  const { message, error } = await searchParams;

  return <main className="min-h-screen bg-coffee-50 px-4 py-10 font-sans">
    <div className="mx-auto max-w-2xl">
      <Link href="/" className="font-bold text-coffee-700">← กลับหน้าหลัก</Link>
      <section className="mt-6 rounded-3xl bg-white p-6 shadow-sm sm:p-9">
        <div className="flex items-center gap-4"><div className="grid h-14 w-14 place-items-center rounded-full bg-coffee-100 text-2xl" aria-hidden="true">👤</div><div><p className="text-sm font-bold text-coffee-500">ตั้งค่าบัญชี</p><h1 className="text-2xl font-black text-coffee-900">ข้อมูลส่วนตัว</h1></div></div>
        {error && <p role="alert" className="mt-5 rounded-xl bg-red-50 p-4 text-sm font-semibold text-red-700">{error}</p>}
        {message && <p role="status" className="mt-5 rounded-xl bg-green-50 p-4 text-sm font-semibold text-green-800">{message}</p>}
        <form action={updateAccount} className="mt-7 space-y-5">
          <label className="grid gap-2 font-semibold text-coffee-800">ชื่อ<input name="fullName" type="text" autoComplete="name" required maxLength={100} defaultValue={actor.profile.fullName} className="rounded-xl border border-coffee-200 px-4 py-3 font-normal" /></label>
          <label className="grid gap-2 font-semibold text-coffee-800">อีเมล<input name="email" type="email" autoComplete="email" required defaultValue={actor.authUser.email ?? actor.profile.email ?? ""} className="rounded-xl border border-coffee-200 px-4 py-3 font-normal" /></label>
          <button type="submit" className="w-full rounded-xl bg-coffee-700 px-5 py-3 font-bold text-white hover:bg-coffee-900">บันทึกข้อมูล</button>
        </form>
        <div className="mt-8 border-t border-coffee-100 pt-6">
          <Link href="/my-bookings" className="font-semibold text-coffee-700 underline">ดูประวัติการจอง</Link>
          <form action={logout} className="mt-5"><button type="submit" className="rounded-xl border border-red-200 px-5 py-3 font-bold text-red-700 hover:bg-red-50">ออกจากระบบ</button></form>
        </div>
      </section>
    </div>
  </main>;
}
