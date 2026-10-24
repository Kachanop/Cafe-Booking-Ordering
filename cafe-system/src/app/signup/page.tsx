import Link from "next/link";
import { registerMember } from "@/app/actions";

export default async function SignupPage({ searchParams }: { searchParams: Promise<{ message?: string; error?: string }> }) {
  const { message, error } = await searchParams;
  return <main className="flex min-h-screen flex-col items-center justify-center bg-coffee-50 p-4 font-sans">
    <Link href="/" className="absolute left-6 top-6 font-bold text-coffee-700">← กลับหน้าหลัก</Link>
    <section className="w-full max-w-md rounded-3xl border border-coffee-100 bg-white p-8 shadow-xl">
      <p className="text-sm font-bold text-coffee-500">CAFE & STUDIO</p><h1 className="mt-2 text-3xl font-black text-coffee-900">สมัครสมาชิก</h1><p className="mt-2 text-coffee-600">สร้างบัญชีเพื่อจองบริการและติดตามรายการของคุณ</p>
      {error && <p role="alert" className="mt-5 rounded-xl bg-red-50 p-3 text-sm font-semibold text-red-700">{error}</p>}{message && <p role="status" className="mt-5 rounded-xl bg-green-50 p-3 text-sm font-semibold text-green-700">{message}</p>}
      <form action={registerMember} className="mt-6 space-y-4">
        <label className="grid gap-2 font-semibold text-coffee-800">ชื่อที่ใช้ติดต่อ<input name="fullName" type="text" autoComplete="name" required maxLength={100} className="rounded-xl border border-coffee-200 px-4 py-3 font-normal" /></label>
        <label className="grid gap-2 font-semibold text-coffee-800">อีเมล<input name="email" type="email" autoComplete="email" required className="rounded-xl border border-coffee-200 px-4 py-3 font-normal" /></label>
        <label className="grid gap-2 font-semibold text-coffee-800">รหัสผ่าน<input name="password" type="password" autoComplete="new-password" minLength={8} required className="rounded-xl border border-coffee-200 px-4 py-3 font-normal" /><span className="text-xs font-normal text-coffee-500">อย่างน้อย 8 ตัวอักษร</span></label>
        <p className="text-xs text-coffee-600">บัญชีที่สมัครใหม่จะมีสิทธิ์ลูกค้าสมาชิก บทบาทพนักงานกำหนดโดยผู้ดูแลระบบ</p>
        <button type="submit" className="w-full rounded-xl bg-coffee-700 px-5 py-3 font-bold text-white hover:bg-coffee-900">สร้างบัญชี</button>
      </form>
      <p className="mt-6 text-center text-sm text-coffee-600">มีบัญชีแล้ว? <Link href="/login" className="font-bold text-coffee-800 underline">เข้าสู่ระบบ</Link></p>
    </section>
  </main>;
}
