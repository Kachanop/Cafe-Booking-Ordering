import Link from "next/link";
import { notFound } from "next/navigation";
import { prisma } from "@/lib/prisma";
import { requireActor } from "@/lib/auth";
import { TableQRCode } from "@/components/TableQRCode";

export default async function TableQRPage({ params, searchParams }: PageProps<"/table/[id]/qr">) {
  await requireActor(["waiter", "cashier", "manager", "owner"]);
  const { id: rawId } = await params;
  const query = await searchParams;
  const id = Number(rawId);
  const session = typeof query.session === "string" ? query.session : "";
  if (!Number.isInteger(id) || !session) notFound();
  const table = await prisma.tableCafe.findUnique({ where: { id } });
  if (!table || table.currentSession !== session) notFound();
  const orderPath = `/table/${id}?session=${encodeURIComponent(session)}`;
  return <main className="min-h-screen bg-coffee-50 px-4 py-10 print:bg-white"><div className="mx-auto max-w-xl"><Link href="/cashier" className="mb-5 inline-block font-semibold text-coffee-700 print:hidden">← กลับหน้าแคชเชียร์</Link><TableQRCode path={orderPath} tableNumber={table.tableNumber} /></div></main>;
}
