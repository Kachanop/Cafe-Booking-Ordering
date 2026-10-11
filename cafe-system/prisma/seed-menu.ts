import 'dotenv/config'
import { Pool } from 'pg'
import { PrismaPg } from '@prisma/adapter-pg'
import { PrismaClient } from '../generated/prisma/client'

const connectionString = process.env.DATABASE_URL
const pool = new Pool({ connectionString })
const adapter = new PrismaPg(pool)
const prisma = new PrismaClient({ adapter })

async function main() {
  console.log('--- เริ่มต้นเพิ่ม 10 เมนูคาเฟ่ลงใน Supabase ---')

  // 1. หมวดหมู่เมนู (Categories)
  const categoriesData = [
    { name: 'Coffee (กาแฟ)' },
    { name: 'Non-Coffee (เครื่องดื่มอื่นๆ)' },
    { name: 'Bakery & Dessert (เบเกอรี่และของหวาน)' },
  ]

  const categoriesMap: Record<string, number> = {}

  for (const cat of categoriesData) {
    let existing = await prisma.menuCategory.findFirst({
      where: { name: cat.name },
    })

    if (!existing) {
      existing = await prisma.menuCategory.create({
        data: { name: cat.name },
      })
      console.log(`+ สร้างหมวดหมู่: ${existing.name}`)
    }
    categoriesMap[cat.name] = existing.id
  }

  // 2. ข้อมูล 10 เมนูคาเฟ่ยอดนิยม
  const menuList = [
    // Coffee
    {
      category: 'Coffee (กาแฟ)',
      name: 'Espresso (เอสเพรสโซ่ร้อน)',
      price: 65,
      isAvailable: true,
    },
    {
      category: 'Coffee (กาแฟ)',
      name: 'Iced Americano (อเมริกาโน่เย็น)',
      price: 80,
      isAvailable: true,
    },
    {
      category: 'Coffee (กาแฟ)',
      name: 'Iced Latte (ลาเต้เย็น)',
      price: 95,
      isAvailable: true,
    },
    {
      category: 'Coffee (กาแฟ)',
      name: 'Iced Caramel Macchiato (คาราเมลมัคคิอาโต้เย็น)',
      price: 110,
      isAvailable: true,
    },
    {
      category: 'Coffee (กาแฟ)',
      name: 'Dirty Coffee (เดอร์ตี้)',
      price: 115,
      isAvailable: true,
    },
    // Non-Coffee
    {
      category: 'Non-Coffee (เครื่องดื่มอื่นๆ)',
      name: 'Iced Matcha Latte (มัทฉะลาเต้พรีเมียม)',
      price: 105,
      isAvailable: true,
    },
    {
      category: 'Non-Coffee (เครื่องดื่มอื่นๆ)',
      name: 'Iced Thai Tea (ชาไทยต้นตำรับ)',
      price: 75,
      isAvailable: true,
    },
    {
      category: 'Non-Coffee (เครื่องดื่มอื่นๆ)',
      name: 'Yuzu Sparkling Soda (ยูสุสปาร์กลิงโซดา)',
      price: 90,
      isAvailable: true,
    },
    // Bakery & Dessert
    {
      category: 'Bakery & Dessert (เบเกอรี่และของหวาน)',
      name: 'French Butter Croissant (ครัวซองต์เนยสดฝรั่งเศส)',
      price: 85,
      isAvailable: true,
    },
    {
      category: 'Bakery & Dessert (เบเกอรี่และของหวาน)',
      name: 'Basque Burnt Cheesecake (ชีสเค้กหน้าไหม้)',
      price: 135,
      isAvailable: true,
    },
  ]

  for (const item of menuList) {
    const categoryId = categoriesMap[item.category]
    const existingItem = await prisma.menuItem.findFirst({
      where: { name: item.name },
    })

    if (!existingItem) {
      const created = await prisma.menuItem.create({
        data: {
          categoryId: categoryId,
          name: item.name,
          price: item.price,
          isAvailable: item.isAvailable,
        },
      })
      console.log(`✔ เพิ่มเมนู: ${created.name} (${created.price} บาท)`)
    } else {
      console.log(`ℹ เมนูนี้มีอยู่แล้ว: ${existingItem.name}`)
    }
  }

  console.log('🎉 เพิ่มเมนูลง Supabase ครบถ้วนเรียบร้อยแล้ว!')
}

main()
  .catch((e) => {
    console.error('Error seeding menu:', e)
    process.exit(1)
  })
  .finally(async () => {
    await prisma.$disconnect()
  })
