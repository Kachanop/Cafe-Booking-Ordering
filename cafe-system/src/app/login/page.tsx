import Link from 'next/link'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { login, signup } from './actions'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ message: string; error: string }>
}) {
  const { message, error } = await searchParams

  return (
    <div className="min-h-screen bg-coffee-50 flex flex-col items-center justify-center p-4 font-sans relative">
      <Link href="/" className="absolute top-8 left-8 text-coffee-700 hover:text-coffee-900 font-bold transition-colors">
        &larr; กลับหน้าหลัก
      </Link>
      
      <Card className="w-full max-w-md shadow-2xl border-coffee-100 bg-white/80 backdrop-blur-md rounded-3xl overflow-hidden">
        <div className="h-2 w-full bg-coffee-500"></div>
        <CardHeader className="text-center space-y-2 pt-8">
          <CardTitle className="text-4xl font-extrabold text-coffee-900 tracking-tight">เข้าสู่ระบบ</CardTitle>
          <CardDescription className="text-coffee-500 font-medium text-md">
            ระบบจองคิวและจัดการคาเฟ่แบบครบวงจร
          </CardDescription>
        </CardHeader>
        
        <CardContent className="pb-10 px-8">
          <form className="space-y-6">
            <div className="space-y-3">
              <Label htmlFor="email" className="text-coffee-900 font-bold text-md">อีเมล (Email)</Label>
              <Input 
                id="email" 
                name="email" 
                type="email" 
                placeholder="you@example.com" 
                required 
                className="border-coffee-300 focus-visible:ring-coffee-500 text-lg py-6 bg-white"
              />
            </div>
            
            <div className="space-y-3">
              <Label htmlFor="password" className="text-coffee-900 font-bold text-md">รหัสผ่าน (Password)</Label>
              <Input 
                id="password" 
                name="password" 
                type="password" 
                placeholder="••••••••" 
                required 
                className="border-coffee-300 focus-visible:ring-coffee-500 text-lg py-6 bg-white"
              />
            </div>

            {/* Role Selector (สำหรับทดสอบสมมติบทบาท) */}
            <div className="space-y-3 pt-2">
              <Label htmlFor="role" className="text-coffee-900 font-bold text-md">เลือก Role (สำหรับตอนกดสมัครสมาชิก)</Label>
              <select 
                id="role" 
                name="role"
                className="w-full border border-coffee-300 rounded-xl py-4 px-4 text-coffee-900 bg-white focus:outline-none focus:ring-2 focus:ring-coffee-500 appearance-none font-medium"
              >
                <option value="member">ลูกค้าระบบสมาชิก (Member)</option>
                <option value="artist">ช่างภาพ/ช่างแต่งหน้า (Artist)</option>
                <option value="cashier">แคชเชียร์หน้าร้าน (Cashier)</option>
                <option value="kitchen">คนครัว (Kitchen)</option>
                <option value="manager">ผู้จัดการ (Manager)</option>
                <option value="owner">เจ้าของร้าน (Owner)</option>
              </select>
              <p className="text-xs text-coffee-500 font-medium">*ใช้เลือกระดับสิทธิ์ ตอนกดปุ่มสมัครสมาชิกใหม่เท่านั้น</p>
            </div>

            {error && (
              <p className="text-sm font-bold text-red-600 bg-red-50 p-4 rounded-xl text-center border border-red-200">
                {error}
              </p>
            )}
            
            {message && (
              <p className="text-sm font-bold text-green-700 bg-green-50 p-4 rounded-xl text-center border border-green-200">
                {message}
              </p>
            )}

            <div className="flex flex-col gap-4 pt-4">
              <Button formAction={login} className="w-full bg-coffee-700 hover:bg-coffee-900 text-white font-bold text-xl py-7 rounded-2xl shadow-lg transition-transform hover:scale-[1.02]">
                เข้าสู่ระบบ
              </Button>
              
              <Button formAction={signup} variant="outline" className="w-full border-2 border-coffee-300 text-coffee-700 hover:bg-coffee-50 font-bold text-lg py-7 rounded-2xl transition-transform hover:scale-[1.02]">
                สมัครสมาชิก (ตาม Role ที่เลือก)
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
