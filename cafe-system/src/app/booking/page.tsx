import Link from "next/link";
import { prisma } from "@/lib/prisma";
import { getActor } from "@/lib/auth";
import { createBooking } from "@/app/actions";
import { getBangkokDate } from "@/lib/date";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";

const slots = ["10:00-12:00", "13:00-15:00", "15:00-17:00", "17:00-19:00"];
const money = (amount: number) => new Intl.NumberFormat("th-TH", { style: "currency", currency: "THB", maximumFractionDigits: 0 }).format(amount);

export default async function BookingPage({ searchParams }: PageProps<"/booking">) {
  const params = await searchParams;
  const candidateDate = typeof params.date === "string" ? params.date : "";
  const selectedDate = /^\d{4}-\d{2}-\d{2}$/.test(candidateDate) && !Number.isNaN(new Date(`${candidateDate}T00:00:00.000Z`).valueOf()) && new Date(`${candidateDate}T00:00:00.000Z`).toISOString().slice(0, 10) === candidateDate ? candidateDate : getBangkokDate();
  const [artists, services, bookings, actor] = await Promise.all([
    prisma.user.findMany({ where: { role: { name: { equals: "Artist", mode: "insensitive" } }, staffProfile: { isNot: null } }, include: { staffProfile: true } }),
    prisma.service.findMany({ orderBy: { id: "asc" } }),
    prisma.booking.findMany({ where: { bookingDate: new Date(`${selectedDate}T00:00:00.000Z`), status: { in: ["Pending", "Paid", "Completed"] } }, select: { staffId: true, timeSlot: true } }),
    getActor(),
  ]);
  return <main className="min-h-screen bg-coffee-50 font-sans"><Navbar />
    <section className="mx-auto max-w-6xl px-4 py-12">
      <Link href="/" className="text-coffee-700 font-semibold">← กลับหน้าหลัก</Link>
      <div className="mt-6 rounded-3xl bg-coffee-900 p-8 text-white md:p-12"><p className="text-coffee-300 font-bold">CAFE & STUDIO</p><h1 className="mt-2 text-4xl font-black">จองคิวช่างของคุณ</h1><p className="mt-3 max-w-2xl text-coffee-100">เลือกช่าง บริการ และรอบเวลา 2 ชั่วโมง ระบบจะล็อกคิวให้ทันทีเมื่อส่งคำขอ</p></div>
      {params.error && <p role="alert" className="mt-5 rounded-xl bg-red-50 p-4 font-semibold text-red-700">{params.error}</p>}
      {artists.length === 0 || services.length === 0 ? <div className="mt-8 rounded-2xl bg-white p-8 text-coffee-700">ยังไม่มีข้อมูลช่างหรือบริการ กรุณาติดต่อผู้จัดการร้าน</div> : <>
        <form method="get" className="mt-8 flex flex-wrap items-end gap-3 rounded-2xl bg-white p-5 shadow-sm"><label className="grid gap-2 font-semibold">วันที่ต้องการจอง<input type="date" name="date" defaultValue={selectedDate} min={getBangkokDate()} className="rounded-xl border border-coffee-200 px-4 py-3" /></label><button className="rounded-xl bg-coffee-700 px-5 py-3 font-bold text-white">ดูคิววันที่เลือก</button>{!actor && <Link href="/login" className="text-coffee-700 font-semibold">เข้าสู่ระบบเพื่อจอง</Link>}</form>
        <div className="mt-8 grid gap-6 md:grid-cols-2">{artists.map((artist) => <article key={artist.id} className="rounded-3xl bg-white p-6 shadow-sm"><div className="flex items-start justify-between gap-4"><div><h2 className="text-2xl font-extrabold text-coffee-900">{artist.fullName}</h2><p className="mt-1 text-sm text-coffee-600">{artist.staffProfile?.bio || "ช่างภาพและช่างแต่งหน้า"}</p>{artist.staffProfile?.specialties?.length ? <p className="mt-3 text-sm font-semibold text-coffee-700">{artist.staffProfile.specialties.join(" · ")}</p> : null}</div>{artist.staffProfile?.portfolioUrl && <a href={artist.staffProfile.portfolioUrl} target="_blank" rel="noreferrer" className="text-sm font-bold text-coffee-700 underline">Portfolio</a>}</div>
          {services.map((service) => <div key={service.id} className="mt-5 border-t border-coffee-100 pt-4"><div className="flex justify-between gap-3"><h3 className="font-bold text-coffee-900">{service.name}</h3><span className="font-bold text-coffee-700">{money(service.price)}</span></div>
            <div className="mt-3 grid grid-cols-2 gap-2">{slots.map((slot) => { const full = bookings.some((booking) => booking.staffId === artist.id && booking.timeSlot === slot); return <form key={slot} action={createBooking} className="contents"><input type="hidden" name="staffId" value={artist.id} /><input type="hidden" name="serviceId" value={service.id} /><input type="hidden" name="date" value={selectedDate} /><input type="hidden" name="timeSlot" value={slot} /><button disabled={full || !actor} className="rounded-xl border border-coffee-200 px-3 py-3 text-sm font-bold text-coffee-800 hover:bg-coffee-50 disabled:cursor-not-allowed disabled:bg-stone-100 disabled:text-stone-400">{slot.replace("-", " – ")}{full ? " · คิวเต็ม" : " · จองรอบนี้"}</button></form>; })}</div>
          </div>)}</article>)}</div>
      </>}
      <p className="mt-8 text-sm text-coffee-600">การจองจะอยู่สถานะรอชำระจนกว่าจะยืนยันการรับชำระเงินเต็มจำนวน</p>
    </section><Footer /></main>;
}

