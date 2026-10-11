import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Home, UtensilsCrossed, Camera, Users } from "lucide-react";

export default function Navbar() {
  return (
    <nav className="sticky top-0 z-50 w-full bg-white/95 backdrop-blur-sm border-b border-coffee-100 shadow-sm transition-all duration-300">
      <div className="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between">
        {/* Logo */}
        <div className="flex items-center gap-2">
          <Link href="/">
            <span className="text-2xl font-extrabold text-coffee-900 tracking-tight cursor-pointer hover:opacity-90 transition-opacity">
              Cafe<span className="text-coffee-300">&</span>Studio
            </span>
          </Link>
        </div>

        {/* Navigation Items with Icons */}
        <div className="flex items-center gap-1 sm:gap-2 md:gap-3 text-sm font-semibold text-coffee-700">
          {/* 1. หน้าแรก */}
          <Link
            href="/"
            title="หน้าแรก"
            className="flex items-center gap-2 px-3.5 py-2 rounded-full hover:bg-coffee-100/60 hover:text-coffee-900 transition-all group"
          >
            <div className="p-1 rounded-full group-hover:scale-110 transition-transform">
              <Home className="w-5 h-5 text-coffee-700 group-hover:text-coffee-900" />
            </div>
            <span className="hidden sm:inline font-bold">หน้าแรก</span>
          </Link>

          {/* 2. เมนู */}
          <Link
            href="/#cafe-menu"
            title="เมนู"
            className="flex items-center gap-2 px-3.5 py-2 rounded-full hover:bg-coffee-100/60 hover:text-coffee-900 transition-all group"
          >
            <div className="p-1 rounded-full group-hover:scale-110 transition-transform">
              <UtensilsCrossed className="w-5 h-5 text-coffee-700 group-hover:text-coffee-900" />
            </div>
            <span className="hidden sm:inline font-bold">เมนู</span>
          </Link>

          {/* 3. ช่างภาพ */}
          <Link
            href="/booking"
            title="ช่างภาพ"
            className="flex items-center gap-2 px-3.5 py-2 rounded-full hover:bg-coffee-100/60 hover:text-coffee-900 transition-all group"
          >
            <div className="p-1 rounded-full group-hover:scale-110 transition-transform">
              <Camera className="w-5 h-5 text-coffee-700 group-hover:text-coffee-900" />
            </div>
            <span className="hidden sm:inline font-bold">ช่างภาพ</span>
          </Link>

          {/* 4. เกี่ยวกับเรา */}
          <a
            href="/#about"
            title="เกี่ยวกับเรา"
            className="flex items-center gap-2 px-3.5 py-2 rounded-full hover:bg-coffee-100/60 hover:text-coffee-900 transition-all group"
          >
            <div className="p-1 rounded-full group-hover:scale-110 transition-transform">
              <Users className="w-5 h-5 text-coffee-700 group-hover:text-coffee-900" />
            </div>
            <span className="hidden sm:inline font-bold">เกี่ยวกับเรา</span>
          </a>
        </div>

        {/* เข้าสู่ระบบ */}
        <Link href="/login">
          <Button
            variant="outline"
            className="border-coffee-500 text-coffee-900 hover:bg-coffee-100 rounded-full px-5 sm:px-6 font-bold shadow-sm"
          >
            เข้าสู่ระบบ
          </Button>
        </Link>
      </div>
    </nav>
  );
}
