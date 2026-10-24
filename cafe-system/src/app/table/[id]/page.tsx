import Link from "next/link";
import { notFound } from "next/navigation";
import { prisma } from "@/lib/prisma";
import { MenuCart } from "@/components/MenuCart";
import { CallBillButton } from "@/components/CallBillButton";

export default async function TableOrderingPage({ params, searchParams }: PageProps<"/table/[id]">) {
  const { id: rawId } = await params;
  const query = await searchParams;
  const id = Number(rawId);
  const session = typeof query.session === "string" ? query.session : "";
  if (!Number.isInteger(id) || !session) notFound();
  const [table, categories] = await Promise.all([
    prisma.tableCafe.findUnique({ where: { id } }),
    prisma.menuCategory.findMany({ include: { menuItems: { orderBy: { id: "asc" } } }, orderBy: { id: "asc" } }),
  ]);
  if (!table || table.currentSession !== session) return <main className="grid min-h-screen place-items-center bg-coffee-50 p-5"><div className="max-w-lg rounded-3xl bg-white p-8 text-center shadow"><h1 className="text-2xl font-black text-coffee-900">QR นี้หมดอายุแล้ว</h1><p className="mt-3 text-coffee-600">เซสชันโต๊ะสิ้นสุดแล้ว กรุณาเรียกพนักงานเพื่อเปิด QR ใหม่</p></div></main>;
  const items = categories.flatMap((category) => category.menuItems.map((item) => ({ id: item.id, name: item.name, price: item.price, isAvailable: item.isAvailable, category: category.name })));
  return <main className="min-h-screen bg-coffee-50 px-4 py-7 md:px-8"><div className="mx-auto max-w-7xl"><div className="flex flex-wrap items-center justify-between gap-3 rounded-3xl bg-coffee-900 p-6 text-white"><div><p className="text-sm font-bold text-coffee-300">CAFE & STUDIO</p><h1 className="mt-1 text-3xl font-black">ยินดีต้อนรับ · โต๊ะ {table.tableNumber}</h1><p className="mt-2 text-sm text-coffee-100">เลือกเมนู ระบุหมายเหตุ และส่งออเดอร์เข้าครัวได้เลย</p></div><CallBillButton tableId={id} session={session} /></div>
    {items.length ? <MenuCart tableId={id} session={session} items={items} /> : <div className="mt-8 rounded-2xl bg-white p-8 text-coffee-600">ขณะนี้ยังไม่มีเมนูให้สั่ง</div>}
    <div className="mt-8 text-center"><Link href="/" className="text-sm font-semibold text-coffee-600 underline">ดูข้อมูลร้าน</Link></div></div></main>;
}
