import Link from "next/link";
import { Coffee, Camera, MapPin, Clock, Phone, Mail, Globe, Share2 } from "lucide-react";

export default function Footer() {
  return (
    <footer id="about" className="bg-coffee-900 text-coffee-100 border-t border-coffee-700 scroll-mt-20">
      <div className="max-w-7xl mx-auto px-6 py-16">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-10">
          {/* Col 1: เกี่ยวกับเรา & เรื่องราวของร้าน */}
          <div className="space-y-4">
            <Link href="/">
              <span className="text-2xl font-extrabold text-white tracking-tight cursor-pointer">
                Cafe<span className="text-coffee-300">&</span>Studio
              </span>
            </Link>
            <p className="text-sm text-coffee-100/80 leading-relaxed font-light">
              พื้นที่สร้างสรรค์ที่ผสานรสชาติกาแฟพรีเมียม เข้ากับสตูดิโอถ่ายภาพและช่างแต่งหน้ามืออาชีพ ให้ทุกช่วงเวลาของคุณถูกบันทึกไว้อย่างงดงามและประทับใจ
            </p>
            <div className="flex items-center gap-3 pt-2">
              <a
                href="#"
                className="w-9 h-9 rounded-full bg-coffee-800 hover:bg-coffee-700 flex items-center justify-center text-coffee-100 transition-colors"
                title="Website"
              >
                <Globe className="w-4 h-4" />
              </a>
              <a
                href="#"
                className="w-9 h-9 rounded-full bg-coffee-800 hover:bg-coffee-700 flex items-center justify-center text-coffee-100 transition-colors"
                title="Share"
              >
                <Share2 className="w-4 h-4" />
              </a>
              <a
                href="#"
                className="w-9 h-9 rounded-full bg-coffee-800 hover:bg-coffee-700 flex items-center justify-center text-coffee-100 transition-colors"
                title="Cafe Specials"
              >
                <Coffee className="w-4 h-4" />
              </a>
            </div>
          </div>

          {/* Col 2: เมนูลัด */}
          <div className="space-y-4">
            <h4 className="text-base font-bold text-white tracking-wider uppercase border-b border-coffee-800 pb-2">
              เมนูลัด
            </h4>
            <ul className="space-y-2.5 text-sm text-coffee-100/80 font-medium">
              <li>
                <Link href="/" className="hover:text-white transition-colors">
                  หน้าแรก (Home)
                </Link>
              </li>
              <li>
                <Link href="/#cafe-menu" className="hover:text-white transition-colors">
                  เมนูอาหาร & เครื่องดื่ม
                </Link>
              </li>
              <li>
                <Link href="/booking" className="hover:text-white transition-colors">
                  จองคิวช่างภาพ & ช่างแต่งหน้า
                </Link>
              </li>
              <li>
                <Link href="/login" className="hover:text-white transition-colors">
                  เข้าสู่ระบบพนักงาน & สมาชิก
                </Link>
              </li>
            </ul>
          </div>

          {/* Col 3: เวลาทำการ */}
          <div className="space-y-4">
            <h4 className="text-base font-bold text-white tracking-wider uppercase border-b border-coffee-800 pb-2">
              เวลาทำการ
            </h4>
            <div className="space-y-3 text-sm text-coffee-100/80">
              <div className="flex items-start gap-2.5">
                <Clock className="w-4 h-4 text-coffee-300 mt-0.5 shrink-0" />
                <div>
                  <p className="font-semibold text-white">โซนคาเฟ่</p>
                  <p className="text-xs">เปิดทุกวัน: 08:30 - 19:00 น.</p>
                </div>
              </div>
              <div className="flex items-start gap-2.5">
                <Camera className="w-4 h-4 text-coffee-300 mt-0.5 shrink-0" />
                <div>
                  <p className="font-semibold text-white">โซนสตูดิโอถ่ายภาพ</p>
                  <p className="text-xs">เปิดตามรอบจอง: 10:00 - 19:00 น.</p>
                </div>
              </div>
            </div>
          </div>

          {/* Col 4: ข้อมูลติดต่อ */}
          <div className="space-y-4">
            <h4 className="text-base font-bold text-white tracking-wider uppercase border-b border-coffee-800 pb-2">
              ติดต่อเรา
            </h4>
            <ul className="space-y-2.5 text-sm text-coffee-100/80">
              <li className="flex items-start gap-2.5">
                <MapPin className="w-4 h-4 text-coffee-300 mt-0.5 shrink-0" />
                <span>123 ถนนสุขุมวิท ซอยคาเฟ่ กรุงเทพฯ 10110</span>
              </li>
              <li className="flex items-center gap-2.5">
                <Phone className="w-4 h-4 text-coffee-300 shrink-0" />
                <span>02-123-4567, 089-123-4567</span>
              </li>
              <li className="flex items-center gap-2.5">
                <Mail className="w-4 h-4 text-coffee-300 shrink-0" />
                <span>contact@cafeandstudio.com</span>
              </li>
            </ul>
          </div>
        </div>

        {/* ส่วนลิขสิทธิ์ด้านล่าง */}
        <div className="border-t border-coffee-800 mt-12 pt-8 text-center text-xs text-coffee-100/60 flex flex-col sm:flex-row items-center justify-between gap-4">
          <p>© 2026 Cafe & Studio Booking & Ordering System. All rights reserved.</p>
          <p>CSI402 Project • Powered by Next.js & Supabase</p>
        </div>
      </div>
    </footer>
  );
}
