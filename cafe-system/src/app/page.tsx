import Image from "next/image";
import Link from "next/link";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { prisma } from "@/lib/prisma";
import type { Prisma } from "../../generated/prisma/client";
import { Coffee, Sparkles, CakeSlice, CheckCircle2, ArrowDown } from "lucide-react";

export const revalidate = 0; // ดึงข้อมูลสดใหม่จาก Supabase เสมอ

// ฟังก์ชันแมปรูปภาพความละเอียดสูงสำหรับแต่ละเมนูคาเฟ่
const getMenuItemImage = (name: string) => {
  const n = name.toLowerCase();
  if (n.includes("espresso")) {
    return "https://images.unsplash.com/photo-1510591509098-f4fdc6d0ff04?auto=format&fit=crop&w=800&q=80";
  }
  if (n.includes("americano")) {
    return "https://images.unsplash.com/photo-1517701550927-30cf4ba1dba5?auto=format&fit=crop&w=800&q=80";
  }
  if (n.includes("caramel")) {
    return "https://images.unsplash.com/photo-1485808191679-5f86510681a2?auto=format&fit=crop&w=800&q=80";
  }
  if (n.includes("dirty")) {
    return "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?auto=format&fit=crop&w=800&q=80";
  }
  if (n.includes("latte") && !n.includes("matcha")) {
    return "https://images.unsplash.com/photo-1570968915860-54d5c301fa9f?auto=format&fit=crop&w=800&q=80";
  }
  if (n.includes("matcha")) {
    return "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?auto=format&fit=crop&w=800&q=80";
  }
  if (n.includes("thai")) {
    return "https://images.unsplash.com/photo-1556679343-c7306c1976bc?auto=format&fit=crop&w=800&q=80";
  }
  if (n.includes("yuzu") || n.includes("soda")) {
    return "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=800&q=80";
  }
  if (n.includes("croissant")) {
    return "https://images.unsplash.com/photo-1555507036-ab1f4038808a?auto=format&fit=crop&w=800&q=80";
  }
  if (n.includes("cheesecake") || n.includes("cake")) {
    return "https://images.unsplash.com/photo-1533134242443-d4fd215305ad?auto=format&fit=crop&w=800&q=80";
  }
  return "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?auto=format&fit=crop&w=800&q=80";
};

export default async function Home() {
  // ดึงหมวดหมู่และรายการเมนูทั้งหมดจากฐานข้อมูล Supabase
  let categories: Prisma.MenuCategoryGetPayload<{ include: { menuItems: true } }>[] = [];
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
      {/* 1. Sticky Navigation Menu พร้อมรูป Icons */}
      <Navbar />

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

      {/* 4. Cafe Menu Showcase Section */}
      <section id="cafe-menu" className="max-w-6xl mx-auto px-4 mt-20 mb-24 scroll-mt-20">
        {/* Banner ข้อมูลเมนูคาเฟ่ */}
        <div className="bg-coffee-900 rounded-3xl p-10 md:p-14 text-center shadow-2xl relative overflow-hidden border border-coffee-700 mb-14">
          <div className="absolute -left-12 -bottom-12 w-48 h-48 bg-coffee-700 rounded-full blur-3xl opacity-50"></div>
          <div className="absolute -right-12 -top-12 w-48 h-48 bg-coffee-500 rounded-full blur-3xl opacity-40"></div>

          <div className="relative z-10 max-w-3xl mx-auto">
            <div className="inline-flex items-center gap-2 bg-coffee-700/80 text-coffee-100 text-sm font-medium px-4 py-1.5 rounded-full mb-4 border border-coffee-500/50">
              <Coffee className="w-4 h-4 text-coffee-300" />
              <span>Signature Coffee & Fresh Bakery</span>
            </div>
            <h2 className="text-3xl md:text-5xl font-black text-coffee-50 mb-4 tracking-tight">
              เมนูคาเฟ่ & เครื่องดื่มสูตรพิเศษ
            </h2>
            <p className="text-coffee-100 mb-6 text-lg md:text-xl font-light leading-relaxed">
              คัดสรรเมล็ดกาแฟชั้นเลิศและวัตถุดิบนำเข้าพรีเมียม อบสดใหม่และชงสดใหม่ทุกวัน เพื่อรสชาติที่ดีที่สุดสำหรับคุณ
            </p>
            <div className="flex flex-wrap justify-center gap-4 text-sm text-coffee-100/90 font-medium">
              <span className="flex items-center gap-1.5 bg-black/20 px-4 py-2 rounded-xl">
                <CheckCircle2 className="w-4 h-4 text-green-400" /> สดใหม่ทุกแก้ว
              </span>
              <span className="flex items-center gap-1.5 bg-black/20 px-4 py-2 rounded-xl">
                <CheckCircle2 className="w-4 h-4 text-green-400" /> วัตถุดิบนำเข้าพรีเมียม
              </span>
              <span className="flex items-center gap-1.5 bg-black/20 px-4 py-2 rounded-xl">
                <CheckCircle2 className="w-4 h-4 text-green-400" /> บรรยากาศผ่อนคลาย
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

        {/* Categories & Items Grid: สัดส่วน รูปภาพ 65% : ข้อความ 35% */}
        <div className="space-y-16">
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

              {/* Items Cards Grid */}
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                {category.menuItems.map((item) => (
                  <div
                    key={item.id}
                    className="group bg-white rounded-3xl overflow-hidden border border-coffee-100 shadow-sm hover:shadow-xl hover:border-coffee-300 transition-all duration-300 flex flex-col h-[430px]"
                  >
                    {/* ส่วนรูปภาพ 65% */}
                    <div className="relative h-[65%] w-full overflow-hidden bg-coffee-100/30">
                      <Image
                        src={getMenuItemImage(item.name)}
                        alt={item.name}
                        fill
                        sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
                        className="object-cover group-hover:scale-105 transition-transform duration-500 ease-out"
                      />
                      <div className="absolute top-3 right-3 z-10">
                        <Badge
                          variant="secondary"
                          className={`backdrop-blur-md shadow-sm font-semibold text-xs px-3 py-1 rounded-full ${
                            item.isAvailable
                              ? "bg-white/90 text-emerald-700 border border-emerald-200"
                              : "bg-black/60 text-white border-none"
                          }`}
                        >
                          {item.isAvailable ? "พร้อมเสิร์ฟ" : "สินค้าหมด"}
                        </Badge>
                      </div>
                    </div>

                    {/* ส่วนข้อความและรายละเอียด 35% */}
                    <div className="h-[35%] p-5 flex flex-col justify-between bg-white border-t border-coffee-50">
                      <div>
                        <h5 className="text-lg md:text-xl font-bold text-coffee-900 group-hover:text-coffee-700 transition-colors line-clamp-1">
                          {item.name}
                        </h5>
                        <p className="text-xs text-coffee-500 font-medium mt-1">
                          {category.name}
                        </p>
                      </div>

                      <div className="flex items-center justify-between pt-2">
                        <div className="flex items-baseline gap-1">
                          <span className="text-2xl md:text-3xl font-black text-coffee-900 tracking-tight">
                            {item.price}
                          </span>
                          <span className="text-base font-bold text-coffee-700">
                            บาท
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* 5. Footer (เกี่ยวกับเรา / ข้อมูลร้าน / เมนูลัด) */}
      <Footer />
    </main>
  );
}
