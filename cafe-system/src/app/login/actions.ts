'use server'

import { revalidatePath } from 'next/cache'
import { redirect } from 'next/navigation'
import { createClient } from '@/lib/supabase/server'

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
  const role = data?.user?.user_metadata?.role || 'member'

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
    default:
      redirect('/') // ลูกค้าทั่วไป (Member) ไปหน้าแรก
  }
}

export async function signup(formData: FormData) {
  const supabase = await createClient()

  const email = formData.get('email') as string
  const password = formData.get('password') as string
  const role = formData.get('role') as string || 'member'

  // บันทึก Role ลงใน Supabase Auth โดยใช้ user_metadata
  const { error } = await supabase.auth.signUp({
    email,
    password,
    options: {
      data: {
        role: role,
      }
    }
  })

  if (error) {
    redirect('/login?error=' + encodeURIComponent(error.message))
  }

  revalidatePath('/', 'layout')
  redirect('/login?message=' + encodeURIComponent('ลงทะเบียนสำเร็จ! กรุณาล็อกอินด้วยบัญชีที่คุณเพิ่งสร้าง'))
}
