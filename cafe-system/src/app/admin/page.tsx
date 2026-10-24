import Link from "next/link";
import { prisma } from "@/lib/prisma";
import { requireActor } from "@/lib/auth";
import { assignStaffRole, createMenuCategory, createMenuItem, createService, createTable, markBookingPaid, updateMenu } from "@/app/actions";

const money = (n: number) => new Intl.NumberFormat("th-TH", { style: "currency", currency: "THB", maximumFractionDigits: 0 }).format(n);
const todayInBangkok = new Intl.DateTimeFormat("en-CA", { timeZone: "Asia/Bangkok" }).format(new Date());
const todayStart = new Date(`${todayInBangkok}T00:00:00+07:00`);
const tomorrowStart = new Date(todayStart.getTime() + 86_400_000);
const roleOptions = ["Member", "Artist", "Waiter", "Kitchen", "Cashier", "Manager", "Owner", "IT Staff"];

export default async function AdminPage() {
  const actor = await requireActor(["manager", "owner"]);
  const isOwner = actor.profile.role.name.toLowerCase() === "owner";
  const navItems = [
    { href: "#overview", label: "ภาพรวม" },
    { href: "#bookings", label: "คิวจอง" },
    { href: "#menu", label: "จัดการเมนู" },
    { href: "#tables", label: "โต๊ะ" },
    { href: "#studio", label: "บริการสตูดิโอ" },
    ...(isOwner ? [{ href: "#staff", label: "จัดการทีมงาน" }] : []),
    { href: "/cashier", label: "แคชเชียร์" },
    { href: "/kds", label: "หน้าครัว KDS" },
    { href: "/artist-dashboard", label: "ตารางงานช่าง" },
    { href: "/booking", label: "หน้าจองลูกค้า" },
    { href: "/my-bookings", label: "ประวัติการจอง" },
    { href: "/account", label: "บัญชีของฉัน" },
  ];
  const [payments, completedBookings, activeTables, items, categories, services, tables, pendingBookings, staff] = await Promise.all([
    prisma.payment.findMany({ where: { status: "Success", paidAt: { gte: todayStart, lt: tomorrowStart } }, select: { referenceType: true, amount: true } }),
    prisma.booking.count({ where: { status: "Completed", bookingDate: { gte: todayStart, lt: tomorrowStart } } }),
    prisma.tableCafe.count({ where: { currentSession: { not: null } } }),
    prisma.menuItem.findMany({ include: { category: true }, orderBy: [{ categoryId: "asc" }, { id: "asc" }] }),
    prisma.menuCategory.findMany({ orderBy: { name: "asc" } }),
    prisma.service.findMany({ orderBy: { name: "asc" } }),
    prisma.tableCafe.findMany({ orderBy: { tableNumber: "asc" } }),
    prisma.booking.findMany({ where: { status: "Pending" }, include: { customer: true, service: true, staff: true }, orderBy: { bookingDate: "asc" } }),
    isOwner ? prisma.user.findMany({ include: { role: true }, orderBy: { fullName: "asc" } }) : Promise.resolve([]),
  ]);
  const cafeRevenue = payments.filter((payment) => payment.referenceType === "ORDER").reduce((total, payment) => total + payment.amount, 0);
  const studioRevenue = payments.filter((payment) => payment.referenceType === "BOOKING").reduce((total, payment) => total + payment.amount, 0);
  const totalRevenue = cafeRevenue + studioRevenue;
  return <main className="min-h-screen bg-coffee-50">
    <nav aria-label="เมนูผู้ดูแลร้าน" className="sticky top-0 z-40 -mb-1 bg-coffee-900 text-white shadow-lg">
      <div className="mx-auto max-w-7xl px-4 py-3 md:px-8">
        <div className="mb-2 flex items-center justify-between gap-3">
          <Link href="/" className="shrink-0 text-lg font-black tracking-tight text-white">Cafe<span className="text-amber-300">&</span>Studio <span className="text-xs font-semibold text-white">· จัดการร้าน</span></Link>
          <span className="hidden text-sm font-semibold text-white sm:block">{actor.profile.fullName} · {isOwner ? "เจ้าของร้าน" : "ผู้จัดการ"}</span>
        </div>
        <div className="-mx-1 overflow-x-auto pb-1">
          <div className="flex w-max gap-2 px-1">{navItems.map((item) => <Link key={item.href} href={item.href} className="rounded-full border border-white/30 px-3 py-2 text-sm font-bold text-white transition-colors hover:border-amber-300 hover:bg-amber-300 hover:text-coffee-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-amber-300">{item.label}</Link>)}</div>
        </div>
      </div>
    </nav>
    <div className="mx-auto max-w-7xl px-4 py-8 md:px-8"><div id="overview" className="scroll-mt-36"><div className="flex flex-wrap items-end justify-between gap-3"><div><h1 className="text-3xl font-black text-coffee-900">แดชบอร์ดร้าน</h1><p className="mt-1 text-coffee-700">{actor.profile.fullName} · {isOwner ? "เจ้าของร้าน" : "ผู้จัดการ"} · {todayInBangkok}</p></div><Link href="/cashier" className="rounded-xl bg-coffee-700 px-4 py-2 font-bold text-white">หน้าจอแคชเชียร์</Link></div>
    </div><section className={`mt-6 scroll-mt-36 grid gap-4 sm:grid-cols-2 ${isOwner ? "lg:grid-cols-5" : "lg:grid-cols-3"}`}><Kpi title="ยอดขายวันนี้" value={money(totalRevenue)} />{isOwner && <><Kpi title="รายได้คาเฟ่" value={money(cafeRevenue)} /><Kpi title="รายได้สตูดิโอ" value={money(studioRevenue)} /></>}<Kpi title="จองที่เสร็จแล้ว" value={`${completedBookings} คิว`} /><Kpi title="โต๊ะที่ใช้งาน" value={`${activeTables} โต๊ะ`} /></section>
    {isOwner && <section className="mt-5 rounded-2xl bg-white p-5 shadow-sm"><div className="flex justify-between"><h2 className="text-lg font-black text-coffee-900">สัดส่วนรายได้วันนี้</h2><span className="text-sm text-coffee-700">Cafe / Studio</span></div><div className="mt-4 h-4 overflow-hidden rounded-full bg-violet-100"><div className="h-full bg-coffee-700" style={{ width: `${totalRevenue ? Math.max(0, Math.min(100, cafeRevenue / totalRevenue * 100)) : 0}%` }} /></div><div className="mt-3 flex justify-between text-sm font-semibold text-coffee-700"><span>☕ คาเฟ่ {totalRevenue ? Math.round(cafeRevenue / totalRevenue * 100) : 0}%</span><span>📸 สตูดิโอ {totalRevenue ? Math.round(studioRevenue / totalRevenue * 100) : 0}%</span></div></section>}
    <section id="bookings" className="mt-8 scroll-mt-36"><div className="flex items-center justify-between"><h2 className="text-2xl font-black text-coffee-900">คำจองรอยืนยันการชำระ</h2><span className="rounded-full bg-amber-100 px-3 py-1 text-sm font-bold text-amber-900">{pendingBookings.length}</span></div><div className="mt-3 grid gap-3 lg:grid-cols-2">{pendingBookings.map((booking) => <article key={booking.id} className="rounded-2xl bg-white p-5 shadow-sm"><div className="flex flex-wrap justify-between gap-3"><div><h3 className="font-black text-coffee-900">{booking.customer.fullName} · {booking.service.name}</h3><p className="mt-1 text-sm text-coffee-700">{booking.staff.fullName} · {booking.bookingDate.toLocaleDateString("th-TH", { timeZone: "Asia/Bangkok" })} · {booking.timeSlot}</p></div><strong className="text-coffee-900">{money(booking.totalPrice)}</strong></div><form action={markBookingPaid} className="mt-4 flex flex-wrap items-center gap-3"><input type="hidden" name="bookingId" value={booking.id} /><select name="method" className="rounded-lg border border-coffee-100 bg-white px-3 py-2"><option value="CASH">เงินสด</option><option value="PROMPTPAY">PromptPay (ตรวจสอบแล้ว)</option></select><button className="rounded-lg bg-coffee-700 px-4 py-2 font-bold text-white">ยืนยันรับชำระเต็มจำนวน</button></form></article>)}{!pendingBookings.length && <p className="rounded-xl bg-white p-5 text-coffee-700">ไม่มีรายการค้างชำระ</p>}</div></section>
    <section id="menu" className="mt-10 scroll-mt-36"><h2 className="text-2xl font-black text-coffee-900">จัดการเมนู</h2><div className="mt-3 flex flex-wrap gap-3"><form action={createMenuCategory} className="flex gap-2 rounded-2xl bg-white p-4"><input name="name" required maxLength={60} placeholder="ชื่อหมวดหมู่ใหม่" className="rounded-lg border border-coffee-100 px-3 py-2" /><button className="rounded-lg border border-coffee-100 px-4 py-2 font-bold text-coffee-900">เพิ่มหมวดหมู่</button></form><form action={createMenuItem} className="flex flex-wrap items-end gap-3 rounded-2xl bg-white p-4"><label className="grid gap-1 text-sm font-semibold">ชื่อเมนู<input name="name" required maxLength={100} className="rounded-lg border border-coffee-100 px-3 py-2" /></label><label className="grid gap-1 text-sm font-semibold">หมวดหมู่<select name="categoryId" className="rounded-lg border border-coffee-100 bg-white px-3 py-2">{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label><label className="grid gap-1 text-sm font-semibold">ราคา<input name="price" type="number" min="0" step="1" required className="w-28 rounded-lg border border-coffee-100 px-3 py-2" /></label><button className="rounded-lg bg-coffee-700 px-4 py-2 font-bold text-white">เพิ่มเมนู</button></form></div><div className="mt-3 grid gap-3 md:grid-cols-2 xl:grid-cols-3">{items.map((item) => <form key={item.id} action={updateMenu} className="flex items-center justify-between gap-3 rounded-xl bg-white p-4"><input type="hidden" name="id" value={item.id} /><div><p className="font-bold text-coffee-900">{item.name}</p><p className="text-xs text-coffee-700">{item.category.name}</p></div><input aria-label={`ราคา ${item.name}`} name="price" type="number" min="0" step="1" defaultValue={item.price} className="w-24 rounded-lg border border-coffee-100 px-2 py-2" /><label className="flex items-center gap-1 text-xs font-semibold"><input type="checkbox" name="isAvailable" defaultChecked={item.isAvailable} /> พร้อมขาย</label><button className="rounded-lg border border-coffee-100 px-3 py-2 text-sm font-bold text-coffee-900">บันทึก</button></form>)}</div></section>
    <section className="mt-10 grid scroll-mt-36 gap-8 lg:grid-cols-2"><div id="tables" className="scroll-mt-36"><h2 className="text-2xl font-black text-coffee-900">โต๊ะ</h2><form action={createTable} className="mt-3 flex gap-2 rounded-2xl bg-white p-4"><input name="tableNumber" required placeholder="เช่น T01" className="min-w-0 flex-1 rounded-lg border border-coffee-100 px-3 py-2" /><button className="rounded-lg bg-coffee-700 px-4 py-2 font-bold text-white">เพิ่มโต๊ะ</button></form><div className="mt-3 flex flex-wrap gap-2">{tables.map((table) => <span key={table.id} className="rounded-full bg-white px-3 py-2 text-sm font-semibold text-coffee-900">{table.tableNumber} · {table.currentSession ? "ไม่ว่าง" : "ว่าง"}</span>)}</div></div><div id="studio" className="scroll-mt-36"><h2 className="text-2xl font-black text-coffee-900">บริการสตูดิโอ</h2><form action={createService} className="mt-3 flex flex-wrap gap-2 rounded-2xl bg-white p-4"><input name="name" required placeholder="ชื่อบริการ" className="min-w-0 flex-1 rounded-lg border border-coffee-100 px-3 py-2" /><input name="price" type="number" min="0" step="1" required placeholder="ราคา" className="w-28 rounded-lg border border-coffee-100 px-3 py-2" /><button className="rounded-lg bg-coffee-700 px-4 py-2 font-bold text-white">เพิ่มบริการ</button></form><div className="mt-3 space-y-2">{services.map((service) => <p key={service.id} className="rounded-xl bg-white p-3 font-semibold text-coffee-900">{service.name} · {money(service.price)}</p>)}</div></div></section>
    {isOwner && <section id="staff" className="mt-10 scroll-mt-36"><h2 className="text-2xl font-black text-coffee-900">จัดการบทบาทพนักงาน</h2><p className="mt-1 text-sm text-coffee-700">กำหนดบทบาทจากบัญชีที่ลงทะเบียนแล้ว ห้ามกำหนดบทบาทของบัญชีเจ้าของที่กำลังใช้งาน</p><div className="mt-3 grid gap-3 md:grid-cols-2">{staff.map((user) => <form key={user.id} action={assignStaffRole} className="flex items-center justify-between gap-3 rounded-xl bg-white p-4"><input type="hidden" name="userId" value={user.id} /><div className="min-w-0"><p className="truncate font-bold text-coffee-900">{user.fullName}</p><p className="truncate text-xs text-coffee-700">{user.email}</p></div><select name="role" defaultValue={user.role.name} disabled={user.id === actor.profile.id} className="rounded-lg border border-coffee-100 bg-white px-2 py-2 text-sm">{roleOptions.map((role) => <option key={role}>{role}</option>)}</select>{user.id !== actor.profile.id && <button className="rounded-lg border border-coffee-100 px-3 py-2 text-sm font-bold">บันทึก</button>}</form>)}</div></section>}
  </div></main>;
}

function Kpi({ title, value }: { title: string; value: string }) { return <article className="rounded-2xl bg-white p-5 shadow-sm"><p className="text-sm font-bold text-coffee-700">{title}</p><p className="mt-2 text-2xl font-black text-coffee-900">{value}</p></article>; }




