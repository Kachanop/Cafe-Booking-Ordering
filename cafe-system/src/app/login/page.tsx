import Link from "next/link";
import { login } from "./actions";

export default async function LoginPage({ searchParams }: { searchParams: Promise<{ message?: string; error?: string }> }) {
  const { message, error } = await searchParams;
  return <main className="flex min-h-screen flex-col items-center justify-center bg-coffee-50 p-4 font-sans">
    <Link href="/" className="absolute left-6 top-6 font-bold text-coffee-700">← กลับหน้าหลัก</Link>
    <section className="w-full max-w-md rounded-3xl border border-coffee-100 bg-white p-8 shadow-xl">
      <p className="text-sm font-bold text-coffee-500">CAFE & STUDIO</p><h1 className="mt-2 text-3xl font-black text-coffee-900">เข้าสู่ระบบ</h1><p className="mt-2 text-coffee-600">เข้าสู่ระบบเพื่อจัดการบัญชีและการจองของคุณ</p>
      {error && <p role="alert" className="mt-5 rounded-xl bg-red-50 p-3 text-sm font-semibold text-red-700">{error}</p>}{message && <p role="status" className="mt-5 rounded-xl bg-green-50 p-3 text-sm font-semibold text-green-700">{message}</p>}
      <form action={login} className="mt-6 space-y-4">
        <label className="grid gap-2 font-semibold text-coffee-800">อีเมล<input name="email" type="email" autoComplete="email" required className="rounded-xl border border-coffee-200 px-4 py-3 font-normal" /></label>
        <label className="grid gap-2 font-semibold text-coffee-800">รหัสผ่าน<input name="password" type="password" autoComplete="current-password" required className="rounded-xl border border-coffee-200 px-4 py-3 font-normal" /></label>
        <button type="submit" className="w-full rounded-xl bg-coffee-700 px-5 py-3 font-bold text-white hover:bg-coffee-900">เข้าสู่ระบบ</button>
      </form>
      <p className="mt-6 text-center text-sm text-coffee-600">ยังไม่มีบัญชี? <Link href="/signup" className="font-bold text-coffee-800 underline">สมัครสมาชิก</Link></p>
    </section>
  </main>;
}
