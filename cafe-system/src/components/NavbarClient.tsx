"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { Home, UtensilsCrossed, Camera, Users, UserRound } from "lucide-react";

export default function NavbarClient({ isAuthenticated }: { isAuthenticated: boolean }) {
  const pathname = usePathname();
  const router = useRouter();

  // ฟังก์ชันเลื่อนจอแบบ Smooth Animation เมื่ออยู่หน้าแรก
  const handleScrollToSection = (
    e: React.MouseEvent<HTMLAnchorElement>,
    sectionId: string
  ) => {
    if (pathname === "/") {
      e.preventDefault();
      const element = document.getElementById(sectionId);
      if (element) {
        element.scrollIntoView({ behavior: "smooth", block: "start" });
      }
    } else {
      // หากอยู่หน้าอื่น (เช่น /booking) ให้พาไปหน้าแรกแล้วเลื่อนไปจุดนั้น
      e.preventDefault();
      router.push(`/#${sectionId}`);
    }
  };

  // เลื่อนกลับขึ้นบนสุดเมื่อกดหน้าแรก
  const handleScrollToTop = (e: React.MouseEvent<HTMLAnchorElement>) => {
    if (pathname === "/") {
      e.preventDefault();
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  };

  return (
    <nav className="sticky top-0 z-50 w-full bg-white/95 backdrop-blur-sm border-b border-coffee-100 shadow-sm transition-all duration-300">
      <div className="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between">
        {/* Logo */}
        <div className="flex items-center gap-2">
          <Link href="/" onClick={handleScrollToTop}>
            <span className="text-2xl font-extrabold text-coffee-900 tracking-tight cursor-pointer hover:opacity-90 transition-opacity">
              Cafe<span className="text-coffee-300">&</span>Studio
            </span>
          </Link>
        </div>

        {/* Navigation Items with Icons & Smooth Scroll Animation */}
        <div className="flex items-center gap-1 sm:gap-2 md:gap-3 text-sm font-semibold text-coffee-700">
          {/* 1. หน้าแรก */}
          <Link
            href="/"
            onClick={handleScrollToTop}
            title="หน้าแรก"
            className="flex items-center gap-2 px-3.5 py-2 rounded-full hover:bg-coffee-100/60 hover:text-coffee-900 transition-all group cursor-pointer"
          >
            <div className="p-1 rounded-full group-hover:scale-110 transition-transform">
              <Home className="w-5 h-5 text-coffee-700 group-hover:text-coffee-900" />
            </div>
            <span className="hidden sm:inline font-bold">หน้าแรก</span>
          </Link>

          {/* 2. เมนู (คลิกแล้วมีอนิเมชั่นเลื่อนลงมาด้านล่าง) */}
          <Link
            href="/#cafe-menu"
            onClick={(e) => handleScrollToSection(e, "cafe-menu")}
            title="เมนูอาหาร & กาแฟ"
            className="flex items-center gap-2 px-3.5 py-2 rounded-full hover:bg-coffee-100/60 hover:text-coffee-900 transition-all group cursor-pointer"
          >
            <div className="p-1 rounded-full group-hover:scale-110 transition-transform">
              <UtensilsCrossed className="w-5 h-5 text-coffee-700 group-hover:text-coffee-900" />
            </div>
            <span className="hidden sm:inline font-bold">เมนู</span>
          </Link>

          {/* 3. ช่างภาพ */}
          <Link
            href="/booking"
            title="ช่างภาพ & สตูดิโอ"
            className="flex items-center gap-2 px-3.5 py-2 rounded-full hover:bg-coffee-100/60 hover:text-coffee-900 transition-all group cursor-pointer"
          >
            <div className="p-1 rounded-full group-hover:scale-110 transition-transform">
              <Camera className="w-5 h-5 text-coffee-700 group-hover:text-coffee-900" />
            </div>
            <span className="hidden sm:inline font-bold">ช่างภาพ</span>
          </Link>

          {/* 4. เกี่ยวกับเรา (คลิกแล้วเลื่อนลงมาที่ฟุตเตอร์) */}
          <Link
            href="/#about"
            onClick={(e) => handleScrollToSection(e, "about")}
            title="เกี่ยวกับเรา"
            className="flex items-center gap-2 px-3.5 py-2 rounded-full hover:bg-coffee-100/60 hover:text-coffee-900 transition-all group cursor-pointer"
          >
            <div className="p-1 rounded-full group-hover:scale-110 transition-transform">
              <Users className="w-5 h-5 text-coffee-700 group-hover:text-coffee-900" />
            </div>
            <span className="hidden sm:inline font-bold">เกี่ยวกับเรา</span>
          </Link>
        </div>

        <div className="flex items-center gap-2">
          {isAuthenticated ? (
            <>
              <Link href="/my-bookings" className="hidden text-sm font-semibold text-coffee-700 sm:inline">การจองของฉัน</Link>
              <Link href="/account" aria-label="ตั้งค่าบัญชี" title="ตั้งค่าบัญชี" className="flex items-center gap-2 rounded-full border border-coffee-200 px-3 py-2 font-bold text-coffee-800 hover:bg-coffee-100">
                <UserRound className="h-5 w-5" aria-hidden="true" />
                <span className="hidden sm:inline">บัญชีของฉัน</span>
              </Link>
            </>
          ) : (
            <>
              <Link href="/login" className="rounded-full px-4 py-2 text-sm font-bold text-coffee-700 hover:bg-coffee-100">เข้าสู่ระบบ</Link>
              <Link href="/signup" className="rounded-full bg-coffee-700 px-4 py-2 text-sm font-bold text-white hover:bg-coffee-900">สมัครสมาชิก</Link>
            </>
          )}
        </div>
      </div>
    </nav>
  );
}



