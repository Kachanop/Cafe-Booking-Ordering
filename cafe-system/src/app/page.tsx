import Image from "next/image";
import Link from "next/link";
import { Button } from "@/components/ui/button";

export default function Home() {
  return (
    <main className="min-h-screen bg-coffee-50 font-sans">
      
      {/* 1. Sticky Navigation Menu */}
      <nav className="sticky top-0 z-50 w-full bg-white/95 backdrop-blur-sm border-b border-coffee-100 shadow-sm transition-all duration-300">
        <div className="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Link href="/">
              <span className="text-2xl font-extrabold text-coffee-900 tracking-tight cursor-pointer">Cafe<span className="text-coffee-300">&</span>Studio</span>
            </Link>
          </div>
          <div className="hidden md:flex gap-8 text-sm font-semibold text-coffee-700">
            <Link href="/" className="hover:text-coffee-900 transition-colors">หน้าแรก</Link>
            <a href="#" className="hover:text-coffee-900 transition-colors">ทีมช่างภาพ</a>
            <a href="#" className="hover:text-coffee-900 transition-colors">เมนูคาเฟ่</a>
            <a href="#" className="hover:text-coffee-900 transition-colors">ติดต่อเรา</a>
          </div>
          <Button variant="outline" className="border-coffee-500 text-coffee-900 hover:bg-coffee-100 rounded-full px-6">
            เข้าสู่ระบบ
          </Button>
        </div>
      </nav>

      {/* 2. Hero Section with Generated Image Background */}
      <section className="relative h-[calc(100vh-64px)] min-h-[600px] flex flex-col items-center justify-center">
        <Image
          src="/cafe-hero.jpg"
          alt="Cafe and Studio Background"
          fill
          className="object-cover"
          priority
        />
        {/* Dark overlay for text readability */}
        <div className="absolute inset-0 bg-coffee-900/50"></div>
        
        {/* 3. Hero Content */}
        <div className="relative z-10 text-center px-4 max-w-3xl mt-[-20px]">
          <h1 className="text-6xl md:text-8xl font-black text-white mb-6 drop-shadow-lg tracking-tight">
            จอง<span className="text-coffee-300">คิว</span>
          </h1>
          <p className="text-lg md:text-2xl text-coffee-50 mb-20 drop-shadow-md font-light">
            ช่างภาพและช่างแต่งหน้ามืออาชีพ พร้อมสัมผัสประสบการณ์คาเฟ่สุดพิเศษของเรา
          </p>
          <div className="mt-12 flex flex-col sm:flex-row items-center justify-center gap-6">
            <Link href="/booking">
              <Button className="bg-coffee-500 hover:bg-coffee-700 text-white text-xl px-12 py-8 rounded-full shadow-xl transition-all hover:scale-105 border border-coffee-300 cursor-pointer">
                เริ่มต้นจองรอบเวลา
              </Button>
            </Link>
            <Link href="#">
              <Button variant="outline" className="bg-white hover:bg-gray-50 text-coffee-900 text-xl px-12 py-8 rounded-full shadow-xl transition-all hover:scale-105 border-2 border-white cursor-pointer font-bold">
                เมนูคาเฟ่
              </Button>
            </Link>
          </div>
        </div>
      </section>

      {/* Cafe Menu Teaser */}
      <section className="max-w-6xl mx-auto px-4 mt-16 mb-16">
        <div className="bg-coffee-900 rounded-3xl p-12 text-center shadow-2xl relative overflow-hidden border border-coffee-700">
          {/* Decorative Elements */}
          <div className="absolute -left-10 -bottom-10 w-40 h-40 bg-coffee-700 rounded-full blur-2xl opacity-50"></div>
          <div className="absolute -right-10 -top-10 w-40 h-40 bg-coffee-700 rounded-full blur-2xl opacity-50"></div>
          
          <div className="relative z-10">
            <h2 className="text-3xl md:text-4xl font-extrabold text-coffee-50 mb-4">หิวไหม? สั่งอาหารจากโต๊ะของคุณได้เลย</h2>
            <p className="text-coffee-100 mb-8 text-lg md:text-xl font-light">
              สำหรับลูกค้าที่นั่งอยู่ในคาเฟ่ สามารถสแกน QR Code ที่โต๊ะเพื่อสั่งอาหารได้ทันที
            </p>
            <Button variant="outline" className="bg-white/10 hover:bg-white/20 text-white border-coffee-300 backdrop-blur-md transition-all rounded-full px-8 py-6 text-lg font-semibold">
              ดูตัวอย่างเมนูอาหาร (QR Menu)
            </Button>
          </div>
        </div>
      </section>
    </main>
  );
}
