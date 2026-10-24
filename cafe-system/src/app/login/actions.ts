'use server'

import { revalidatePath } from 'next/cache'
import { redirect } from 'next/navigation'
import { createClient } from '@/lib/supabase/server'
import { ensureMemberProfile, normalizeRole } from '@/lib/auth'
import { prisma } from '@/lib/prisma'

export async function login(formData: FormData) {
  const supabase = await createClient()

  const email = formData.get('email') as string
  const password = formData.get('password') as string

  const { data, error } = await supabase.auth.signInWithPassword({
    email,
    password,
  })

  if (error) {
    redirect('/login?error=' + encodeURIComponent(error.message))
  }

  // ดึง Role จาก metadata ของ Supabase ที่บันทึกไว้ตอนสมัคร
  if (data.user) await ensureMemberProfile(data.user)
  const role = data.user
    ? normalizeRole((await prisma.user.findUnique({
        where: { authUserId: data.user.id },
        include: { role: true },
      }))?.role.name ?? 'Member')
    : 'member'

  revalidatePath('/', 'layout')

  // แยกระบบหลังล็อกอินตาม Role
  switch (role) {
    case 'owner':
    case 'manager':
      redirect('/admin') // ไปหน้า Dashboard ผู้จัดการ
    case 'kitchen':
      redirect('/kds') // ไปหน้าจอห้องครัว
    case 'cashier':
      redirect('/cashier') // ไปหน้าจอแคชเชียร์
    case 'artist':
      redirect('/artist-dashboard') // ไปหน้าดูคิวงานช่าง
    case 'waiter':
      redirect('/cashier')
    case 'it staff':
      redirect('/it')
    default:
      redirect('/') // ลูกค้าทั่วไป (Member) ไปหน้าแรก
  }
}

export async function signup(formData: FormData) {
  const { registerMember } = await import('@/app/actions')
  return registerMember(formData)
}
