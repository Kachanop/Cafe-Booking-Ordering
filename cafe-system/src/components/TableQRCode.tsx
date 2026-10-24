"use client";
import { useEffect, useState } from "react";
import Image from "next/image";
import QRCode from "qrcode";

export function TableQRCode({ path, tableNumber }: { path: string; tableNumber: string }) {
  const [image, setImage] = useState("");
  useEffect(() => { void QRCode.toDataURL(`${window.location.origin}${path}`, { errorCorrectionLevel: "M", margin: 2, width: 320 }).then(setImage); }, [path]);
  return <div className="mx-auto max-w-sm rounded-3xl bg-white p-8 text-center shadow-lg print:shadow-none"><p className="text-sm font-bold uppercase tracking-[0.2em] text-coffee-500">Cafe & Studio</p><h1 className="mt-2 text-3xl font-black text-coffee-900">โต๊ะ {tableNumber}</h1>{image ? <Image src={image} alt={`QR สั่งอาหาร โต๊ะ ${tableNumber}`} width={320} height={320} unoptimized className="mx-auto mt-5 h-72 w-72" /> : <div className="mx-auto mt-5 h-72 w-72 animate-pulse rounded-xl bg-coffee-50" />}<p className="mt-4 text-sm text-coffee-600">สแกนเพื่อดูเมนูและสั่งอาหาร · QR ใช้ได้เฉพาะรอบนี้</p><a href={path} className="mt-4 block break-all text-xs text-coffee-600 underline">{path}</a><button onClick={() => window.print()} className="mt-5 rounded-xl bg-coffee-800 px-6 py-3 font-bold text-white print:hidden">พิมพ์ QR</button></div>;
}
