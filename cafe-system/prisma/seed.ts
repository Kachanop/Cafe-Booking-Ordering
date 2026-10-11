import 'dotenv/config'
import { Pool } from 'pg'
import { PrismaPg } from '@prisma/adapter-pg'
import { PrismaClient } from '../generated/prisma/client'

const connectionString = process.env.DATABASE_URL
const pool = new Pool({ connectionString })
const adapter = new PrismaPg(pool)
const prisma = new PrismaClient({ adapter })

async function main() {
  console.log('เริ่มล้างข้อมูลเก่า...')
  // ล้างข้อมูลเพื่อกัน Error ข้อมูลซ้ำเวลา Seed หลายรอบ
  await prisma.payment.deleteMany()
  await prisma.orderItem.deleteMany()
  await prisma.order.deleteMany()
  await prisma.menuItem.deleteMany()
  await prisma.menuCategory.deleteMany()
  await prisma.tableCafe.deleteMany()
  await prisma.booking.deleteMany()
  await prisma.service.deleteMany()
  await prisma.staffProfile.deleteMany()
  await prisma.user.deleteMany()
  await prisma.role.deleteMany()

  console.log('เริ่มสร้าง Mock Data...')

  // 1. สร้าง Role
  const roleMember = await prisma.role.create({ data: { name: 'Member', description: 'ลูกค้าทั่วไป' } })
  const roleArtist = await prisma.role.create({ data: { name: 'Artist', description: 'ช่างภาพ/ช่างแต่งหน้า' } })
  const roleManager = await prisma.role.create({ data: { name: 'Manager', description: 'ผู้จัดการร้าน' } })

  // 2. สร้าง User
  const customer = await prisma.user.create({
    data: {
      roleId: roleMember.id,
      fullName: 'สมชาย ใจดี',
      email: 'somchai@mock.com',
      phone: '0812345678',
    }
  })

  const artist = await prisma.user.create({
    data: {
      roleId: roleArtist.id,
      fullName: 'คุณเอก ช่างภาพ',
      email: 'aek@mock.com',
      phone: '0898765432',
    }
  })

  // 3. สร้าง StaffProfile
  await prisma.staffProfile.create({
    data: {
      userId: artist.id,
      bio: 'ช่างภาพสายคาเฟ่และพอร์ตเทรต ประสบการณ์ 5 ปี',
      portfolioUrl: 'https://instagram.com/aek.photo',
    }
  })

  // 4. สร้าง Service
  const photoService = await prisma.service.create({
    data: { name: 'แพ็กเกจถ่ายภาพคาเฟ่ 2 ชั่วโมง', price: 2500 }
  })

  // 5. สร้าง Booking
  const booking = await prisma.booking.create({
    data: {
      customerId: customer.id,
      staffId: artist.id,
      serviceId: photoService.id,
      bookingDate: new Date('2026-10-15T00:00:00Z'),
      timeSlot: '13:00-15:00',
      totalPrice: 2500,
      status: 'Paid',
    }
  })

  // 6. สร้าง TableCafe
  const table = await prisma.tableCafe.create({
    data: { tableNumber: 'T01', currentSession: 'session_abc123' }
  })

  // 7. สร้าง MenuCategory
  const catDrink = await prisma.menuCategory.create({
    data: { name: 'เครื่องดื่ม (Drinks)' }
  })

  // 8. สร้าง MenuItem
  const menuLatte = await prisma.menuItem.create({
    data: {
      categoryId: catDrink.id,
      name: 'Iced Latte',
      price: 120,
      isAvailable: true,
    }
  })

  // 9. สร้าง Order
  const order = await prisma.order.create({
    data: {
      tableId: table.id,
      status: 'Dining',
      totalAmount: 240, // ลาเต้ 2 แก้ว
    }
  })

  // 10. สร้าง OrderItem
  const orderItem = await prisma.orderItem.create({
    data: {
      orderId: order.id,
      menuItemId: menuLatte.id,
      quantity: 2,
      kdsStatus: 'Pending',
    }
  })

  // 11. สร้าง Payment (จำลองการจ่ายเงินของคาเฟ่)
  await prisma.payment.create({
    data: {
      referenceId: order.id,
      referenceType: 'ORDER',
      amount: 240,
      status: 'Success',
    }
  })

  console.log('✅ จำลองข้อมูลและใส่ลง Supabase สำเร็จเรียบร้อยแล้ว!')
}

main()
  .catch((e) => {
    console.error(e)
    process.exit(1)
  })
  .finally(async () => {
    await prisma.$disconnect()
  })
