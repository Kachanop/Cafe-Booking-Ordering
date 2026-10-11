# Data Dictionary: ระบบ Cafe Booking & Ordering

เอกสารฉบับนี้อธิบายรายละเอียดของโครงสร้างตาราง (Tables) และฟิลด์ (Fields) ต่างๆ ในฐานข้อมูล PostgreSQL ของระบบ Cafe Booking & Ordering ตามที่ได้ออกแบบไว้

---

## 1. ตาราง Role (จัดการระดับสิทธิ์ผู้ใช้งาน)
เก็บข้อมูลระดับสิทธิ์ของผู้ใช้งานในระบบ เช่น Member, Manager, Artist

| ชื่อฟิลด์ (Field Name) | ชนิดข้อมูล (Data Type) | PK/FK | ความหมาย (Description) | ข้อกำหนด (Constraints) |
| --- | --- | --- | --- | --- |
| `id` | Int | **PK** | รหัสระดับผู้ใช้ | Auto Increment |
| `name` | String | - | ชื่อระดับผู้ใช้ | Unique, ไม่เป็นค่าว่าง |
| `description` | String | - | คำอธิบายสิทธิ์ | Nullable |

---

## 2. ตาราง User (จัดการข้อมูลผู้ใช้งานทั้งหมด)
เก็บข้อมูลลูกค้า พนักงาน และช่างภาพ/ช่างแต่งหน้า

| ชื่อฟิลด์ (Field Name) | ชนิดข้อมูล (Data Type) | PK/FK | ความหมาย (Description) | ข้อกำหนด (Constraints) |
| --- | --- | --- | --- | --- |
| `id` | Int | **PK** | รหัสผู้ใช้งาน | Auto Increment |
| `roleId` | Int | **FK** | อ้างอิงตาราง `Role(id)` | ไม่เป็นค่าว่าง |
| `phone` | String | - | เบอร์โทรศัพท์ | Unique, Nullable |
| `email` | String | - | อีเมล | Unique, Nullable |
| `passwordHash` | String | - | รหัสผ่านที่เข้ารหัสแล้ว | Nullable |
| `fullName` | String | - | ชื่อ-นามสกุล | ไม่เป็นค่าว่าง |
| `createdAt` | DateTime | - | เวลาที่สร้างบัญชี | Default: now() |

---

## 3. ตาราง StaffProfile (ข้อมูลเฉพาะของช่าง)
เก็บประวัติและผลงานเฉพาะของผู้ใช้งานที่เป็นช่างภาพหรือช่างแต่งหน้า

| ชื่อฟิลด์ (Field Name) | ชนิดข้อมูล (Data Type) | PK/FK | ความหมาย (Description) | ข้อกำหนด (Constraints) |
| --- | --- | --- | --- | --- |
| `id` | Int | **PK** | รหัสโปรไฟล์ช่าง | Auto Increment |
| `userId` | Int | **FK** | อ้างอิงตาราง `User(id)` | Unique, Cascade Delete |
| `bio` | String | - | ประวัติย่อ/คำแนะนำตัว | Nullable |
| `portfolioUrl` | String | - | ลิงก์เว็บไซต์รวมผลงาน | Nullable |

---

## 4. ตาราง Service (บริการของสตูดิโอ)
เก็บข้อมูลบริการที่เปิดให้จอง เช่น ถ่ายภาพ, แต่งหน้า

| ชื่อฟิลด์ (Field Name) | ชนิดข้อมูล (Data Type) | PK/FK | ความหมาย (Description) | ข้อกำหนด (Constraints) |
| --- | --- | --- | --- | --- |
| `id` | Int | **PK** | รหัสบริการ | Auto Increment |
| `name` | String | - | ชื่อบริการ | ไม่เป็นค่าว่าง |
| `price` | Float | - | ราคาค่าบริการ | ไม่เป็นค่าว่าง |

---

## 5. ตาราง Booking (ข้อมูลการจองคิวสตูดิโอ)
เก็บข้อมูลคำสั่งจองคิวช่างของลูกค้า

| ชื่อฟิลด์ (Field Name) | ชนิดข้อมูล (Data Type) | PK/FK | ความหมาย (Description) | ข้อกำหนด (Constraints) |
| --- | --- | --- | --- | --- |
| `id` | Int | **PK** | รหัสการจอง | Auto Increment |
| `customerId` | Int | **FK** | อ้างอิง `User(id)` (ลูกค้า) | ไม่เป็นค่าว่าง |
| `staffId` | Int | **FK** | อ้างอิง `User(id)` (ช่าง) | ไม่เป็นค่าว่าง |
| `serviceId` | Int | **FK** | อ้างอิง `Service(id)` | ไม่เป็นค่าว่าง |
| `bookingDate` | DateTime | - | วันที่จอง | ไม่เป็นค่าว่าง |
| `timeSlot` | String | - | รอบเวลา (เช่น 10:00-12:00) | ไม่เป็นค่าว่าง |
| `status` | String | - | สถานะการจอง | Default: 'Pending' (Pending, Paid, Completed, Cancelled) |
| `totalPrice` | Float | - | ราคารวม | ไม่เป็นค่าว่าง |

---

## 6. ตาราง TableCafe (ข้อมูลโต๊ะในคาเฟ่)
จัดการโต๊ะและเซสชันการสั่งอาหารผ่าน QR Code

| ชื่อฟิลด์ (Field Name) | ชนิดข้อมูล (Data Type) | PK/FK | ความหมาย (Description) | ข้อกำหนด (Constraints) |
| --- | --- | --- | --- | --- |
| `id` | Int | **PK** | รหัสโต๊ะ | Auto Increment |
| `tableNumber` | String | - | หมายเลขโต๊ะ | Unique, ไม่เป็นค่าว่าง |
| `currentSession`| String | - | Token สำหรับ QR Order | Nullable (ว่างเมื่อโต๊ะว่าง) |

---

## 7. ตาราง MenuCategory (หมวดหมู่เมนูอาหาร)
| ชื่อฟิลด์ (Field Name) | ชนิดข้อมูล (Data Type) | PK/FK | ความหมาย (Description) | ข้อกำหนด (Constraints) |
| --- | --- | --- | --- | --- |
| `id` | Int | **PK** | รหัสหมวดหมู่ | Auto Increment |
| `name` | String | - | ชื่อหมวดหมู่ (อาหาร, น้ำ) | ไม่เป็นค่าว่าง |

---

## 8. ตาราง MenuItem (เมนูอาหารคาเฟ่)
| ชื่อฟิลด์ (Field Name) | ชนิดข้อมูล (Data Type) | PK/FK | ความหมาย (Description) | ข้อกำหนด (Constraints) |
| --- | --- | --- | --- | --- |
| `id` | Int | **PK** | รหัสรายการอาหาร | Auto Increment |
| `categoryId` | Int | **FK** | อ้างอิง `MenuCategory(id)`| ไม่เป็นค่าว่าง |
| `name` | String | - | ชื่อเมนูอาหาร | ไม่เป็นค่าว่าง |
| `price` | Float | - | ราคา | ไม่เป็นค่าว่าง |
| `isAvailable` | Boolean| - | สถานะพร้อมขาย | Default: true |

---

## 9. ตาราง Order (บิลหลักของคาเฟ่)
เก็บข้อมูลบิลรวมของแต่ละโต๊ะ

| ชื่อฟิลด์ (Field Name) | ชนิดข้อมูล (Data Type) | PK/FK | ความหมาย (Description) | ข้อกำหนด (Constraints) |
| --- | --- | --- | --- | --- |
| `id` | Int | **PK** | รหัสบิลคาเฟ่ | Auto Increment |
| `tableId` | Int | **FK** | อ้างอิง `TableCafe(id)` | ไม่เป็นค่าว่าง |
| `status` | String | - | สถานะบิล | Default: 'Dining' (Dining, Billing, Paid) |
| `totalAmount` | Float | - | ยอดรวมของบิล | Default: 0 |

---

## 10. ตาราง OrderItem (รายการอาหารที่สั่งในบิล)
เก็บข้อมูลออเดอร์แต่ละจาน (เชื่อมกับระบบ KDS ของห้องครัว)

| ชื่อฟิลด์ (Field Name) | ชนิดข้อมูล (Data Type) | PK/FK | ความหมาย (Description) | ข้อกำหนด (Constraints) |
| --- | --- | --- | --- | --- |
| `id` | Int | **PK** | รหัสรายการที่สั่ง | Auto Increment |
| `orderId` | Int | **FK** | อ้างอิง `Order(id)` | Cascade Delete |
| `menuItemId` | Int | **FK** | อ้างอิง `MenuItem(id)` | ไม่เป็นค่าว่าง |
| `quantity` | Int | - | จำนวนที่สั่ง | Default: 1 |
| `kdsStatus` | String | - | สถานะจอครัว (KDS) | Default: 'Pending' (Pending, Cooking, Served) |

---

## 11. ตาราง Payment (ระบบการชำระเงิน)
จุดศูนย์กลางรับชำระเงินที่แยกได้ว่ามาจากฝั่งจองคิว หรือฝั่งคาเฟ่

| ชื่อฟิลด์ (Field Name) | ชนิดข้อมูล (Data Type) | PK/FK | ความหมาย (Description) | ข้อกำหนด (Constraints) |
| --- | --- | --- | --- | --- |
| `id` | Int | **PK** | รหัสการชำระเงิน | Auto Increment |
| `referenceId` | Int | - | รหัสอ้างอิงบิล | ID ของ Booking หรือ Order |
| `referenceType`| String | - | ประเภทการอ้างอิง | enum: 'BOOKING' หรือ 'ORDER' |
| `amount` | Float | - | จำนวนเงินที่ชำระ | ไม่เป็นค่าว่าง |
| `status` | String | - | สถานะการชำระเงิน | Default: 'Success' |
| `paidAt` | DateTime | - | เวลาที่ชำระเงินสำเร็จ | Default: now() |
