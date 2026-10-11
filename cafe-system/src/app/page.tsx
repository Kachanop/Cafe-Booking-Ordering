import Image from "next/image";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { prisma } from "@/lib/prisma";
import { Coffee, Sparkles, CakeSlice, QrCode, CheckCircle2, ArrowDown } from "lucide-react";

export const revalidate = 0; // ดึงข้อมูลสดใหม่จาก Supabase เสมอ

export default async function Home() {
  // ดึงหมวดหมู่และรายการเมนูทั้งหมดจากฐานข้อมูล Supabase
  let categories: any[] = [];
  try {
    categories = await prisma.menuCategory.findMany({
      include: {
        menuItems: {
          orderBy: {
            id: "asc",
          },
        },
      },
      orderBy: {
        id: "asc",
      },
    });
  } catch (error) {
    console.error("Failed to load menus from database:", error);
  }

  // กำหนดไอคอนตามหมวดหมู่
  const getCategoryIcon = (categoryName: string) => {
    const name = categoryName.toLowerCase();
    if (name.includes("coffee") && !name.includes("non")) {
      return <Coffee className="w-5 h-5 text-coffee-700" />;
    } else if (name.includes("bakery") || name.includes("dessert")) {
      return <CakeSlice className="w-5 h-5 text-coffee-700" />;
    } else {
      return <Sparkles className="w-5 h-5 text-coffee-700" />;
    }
  };

  return (
    <main className="min-h-screen bg-coffee-50 font-sans scroll-smooth">
      {/* 1. Sticky Navigation Menu */}
      <nav className="sticky top-0 z-50 w-full bg-white/95 backdrop-blur-sm border-b border-coffee-100 shadow-sm transition-all duration-300">
        <div className="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Link href="/">
              <span className="text-2xl font-extrabold text-coffee-900 tracking-tight cursor-pointer">
                Cafe<span className="text-coffee-300">&</span>Studio
              </span>
            </Link>
          </div>
          <div className="hidden md:flex gap-8 text-sm font-semibold text-coffee-700">
            <Link href="/" className="hover:text-coffee-900 transition-colors">
              หน้าแรก
            </Link>
            <Link href="/booking" className="hover:text-coffee-900 transition-colors">
              ทีมช่างภาพ
            </Link>
            <a href="#cafe-menu" className="hover:text-coffee-900 transition-colors">
              เมนูคาเฟ่
            </a>
            <a href="#cafe-menu" className="hover:text-coffee-900 transition-colors">
              สั่งอาหารผ่าน QR
            </a>
          </div>
          <Link href="/login">
            <Button
              variant="outline"
              className="border-coffee-500 text-coffee-900 hover:bg-coffee-100 rounded-full px-6"
            >
              เข้าสู่ระบบ
            </Button>
          </Link>
        </div>
      </nav>

      {/* 2. Hero Section */}
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
            <a href="#cafe-menu">
              <Button
                variant="outline"
                className="bg-white hover:bg-gray-50 text-coffee-900 text-xl px-12 py-8 rounded-full shadow-xl transition-all hover:scale-105 border-2 border-white cursor-pointer font-bold"
              >
                เมนูคาเฟ่ <ArrowDown className="w-5 h-5 ml-1" />
              </Button>
            </a>
          </div>
        </div>
      </section>

      {/* 4. Cafe Menu Showcase Section (โชว์เมนูที่ดึงตรงจาก Supabase) */}
      <section id="cafe-menu" className="max-w-6xl mx-auto px-4 mt-20 mb-24 scroll-mt-20">
        {/* Banner กล่องสีน้ำตาลเดิม - ปรับให้พรีเมียมและเกริ่นนำเมนู */}
        <div className="bg-coffee-900 rounded-3xl p-10 md:p-14 text-center shadow-2xl relative overflow-hidden border border-coffee-700 mb-14">
          <div className="absolute -left-12 -bottom-12 w-48 h-48 bg-coffee-700 rounded-full blur-3xl opacity-50"></div>
          <div className="absolute -right-12 -top-12 w-48 h-48 bg-coffee-500 rounded-full blur-3xl opacity-40"></div>

          <div className="relative z-10 max-w-3xl mx-auto">
            <div className="inline-flex items-center gap-2 bg-coffee-700/80 text-coffee-100 text-sm font-medium px-4 py-1.5 rounded-full mb-4 border border-coffee-500/50">
              <QrCode className="w-4 h-4 text-coffee-300" />
              <span>In-Store Table Ordering System</span>
            </div>
            <h2 className="text-3xl md:text-5xl font-black text-coffee-50 mb-4 tracking-tight">
              หิวไหม? สั่งอาหารจากโต๊ะของคุณได้เลย
            </h2>
            <p className="text-coffee-100 mb-6 text-lg md:text-xl font-light leading-relaxed">
              สำหรับลูกค้าที่นั่งอยู่ในคาเฟ่ สามารถสแกน <span className="font-semibold text-white">QR Code ที่โต๊ะ</span> เพื่อสั่งอาหารได้ทันที รายการจะถูกส่งตรงไปยังห้องครัวแบบเรียลไทม์
            </p>
            <div className="flex flex-wrap justify-center gap-4 text-sm text-coffee-100/90 font-medium">
              <span className="flex items-center gap-1.5 bg-black/20 px-4 py-2 rounded-xl">
                <CheckCircle2 className="w-4 h-4 text-green-400" /> สดใหม่ทุกออเดอร์
              </span>
              <span className="flex items-center gap-1.5 bg-black/20 px-4 py-2 rounded-xl">
                <CheckCircle2 className="w-4 h-4 text-green-400" /> ไม่ต้องรอเรียกพนักงาน
              </span>
              <span className="flex items-center gap-1.5 bg-black/20 px-4 py-2 rounded-xl">
                <CheckCircle2 className="w-4 h-4 text-green-400" /> ชำระเงินสะดวกที่โต๊ะ
              </span>
            </div>
          </div>
        </div>

        {/* Section Header สำหรับรายการเมนู */}
        <div className="text-center mb-12">
          <div className="inline-block bg-coffee-100/60 text-coffee-900 font-bold text-sm px-4 py-1.5 rounded-full mb-3">
            MENU CATALOG (SUPABASE DATABASE)
          </div>
          <h3 className="text-3xl md:text-4xl font-extrabold text-coffee-900">
            รายการอาหาร & เครื่องดื่มแนะนำ
          </h3>
          <p className="text-coffee-700 text-base md:text-lg mt-2 font-medium">
            ดึงข้อมูลสดจากฐานข้อมูล Supabase ทั้งหมด 10 รายการ
          </p>
        </div>

        {/* Categories & Items Grid */}
        <div className="space-y-14">
          {categories.map((category) => (
            <div key={category.id} className="space-y-6">
              {/* Category Title */}
              <div className="flex items-center gap-3 border-b-2 border-coffee-200 pb-3">
                <div className="p-2.5 bg-coffee-100 rounded-xl">
                  {getCategoryIcon(category.name)}
                </div>
                <div>
                  <h4 className="text-2xl font-bold text-coffee-900">
                    {category.name}
                  </h4>
                  <p className="text-xs text-coffee-700 font-medium">
                    {category.menuItems?.length || 0} รายการ
                  </p>
                </div>
              </div>

              {/* Items Cards */}
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {category.menuItems?.map((item: any) => (
                  <Card
                    key={item.id}
                    className="group border border-coffee-100 bg-white hover:border-coffee-300 hover:shadow-xl transition-all duration-300 rounded-2xl overflow-hidden flex flex-col justify-between"
                  >
                    <CardContent className="p-6">
                      <div className="flex items-start justify-between gap-4 mb-3">
                        <div className="flex-1">
                          <h5 className="text-lg font-bold text-coffee-900 group-hover:text-coffee-700 transition-colors">
                            {item.name}
                          </h5>
                        </div>
                        <Badge
                          variant="secondary"
                          className={`shrink-0 text-xs font-semibold px-2.5 py-1 ${
                            item.isAvailable
                              ? "bg-emerald-50 text-emerald-700 border border-emerald-200"
                              : "bg-red-50 text-red-600 border border-red-200"
                          }`}
                        >
                          {item.isAvailable ? "พร้อมเสิร์ฟ" : "สินค้าหมด"}
                        </Badge>
                      </div>

                      <div className="flex items-baseline justify-between mt-6 pt-4 border-t border-coffee-50">
                        <div>
                          <span className="text-xs text-coffee-500 block font-medium">ราคา</span>
                          <span className="text-2xl font-extrabold text-coffee-900">
                            ฿{item.price.toLocaleString()}
                          </span>
                        </div>
                        <Button
                          size="sm"
                          className="bg-coffee-700 hover:bg-coffee-900 text-white rounded-xl px-4 py-2 text-xs font-semibold shadow-sm transition-transform active:scale-95 cursor-pointer"
                        >
                          + สั่งเมนูนี้
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>
          ))}
        </div>
      </section>
    </main>
  );
}
