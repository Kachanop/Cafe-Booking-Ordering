-- ลบตารางเก่าทิ้งทั้งหมดเพื่อป้องกัน Error (กรณีรันซ้ำ)
DROP TABLE IF EXISTS Payment CASCADE;
DROP TABLE IF EXISTS OrderItem CASCADE;
DROP TABLE IF EXISTS "Order" CASCADE;
DROP TABLE IF EXISTS MenuItem CASCADE;
DROP TABLE IF EXISTS MenuCategory CASCADE;
DROP TABLE IF EXISTS TableCafe CASCADE;
DROP TABLE IF EXISTS Booking CASCADE;
DROP TABLE IF EXISTS Service CASCADE;
DROP TABLE IF EXISTS StaffProfile CASCADE;
DROP TABLE IF EXISTS "User" CASCADE;
DROP TABLE IF EXISTS Role CASCADE;

-- หมายเหตุ: ไม่ใช้คำสั่ง CREATE DATABASE เนื่องจาก Supabase สร้างฐานข้อมูล postgres ไว้ให้แล้ว

-- 1. สร้างตาราง Role
CREATE TABLE Role (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) UNIQUE NOT NULL,
    description TEXT
);

-- 2. สร้างตาราง User
CREATE TABLE "User" (
    id SERIAL PRIMARY KEY,
    "roleId" INT NOT NULL,
    phone VARCHAR(20) UNIQUE,
    email VARCHAR(100) UNIQUE,
    "passwordHash" VARCHAR(255),
    "fullName" VARCHAR(100) NOT NULL,
    "createdAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("roleId") REFERENCES Role(id)
);

-- 3. สร้างตาราง StaffProfile
CREATE TABLE StaffProfile (
    id SERIAL PRIMARY KEY,
    "userId" INT UNIQUE NOT NULL,
    bio TEXT,
    "portfolioUrl" VARCHAR(255),
    FOREIGN KEY ("userId") REFERENCES "User"(id) ON DELETE CASCADE
);

-- 4. สร้างตาราง Service
CREATE TABLE Service (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    price DECIMAL(10, 2) NOT NULL
);

-- 5. สร้างตาราง Booking
CREATE TABLE Booking (
    id SERIAL PRIMARY KEY,
    "customerId" INT NOT NULL,
    "staffId" INT NOT NULL,
    "serviceId" INT NOT NULL,
    "bookingDate" DATE NOT NULL,
    "timeSlot" VARCHAR(50) NOT NULL,
    status VARCHAR(50) DEFAULT 'Pending',
    "totalPrice" DECIMAL(10, 2) NOT NULL,
    FOREIGN KEY ("customerId") REFERENCES "User"(id),
    FOREIGN KEY ("staffId") REFERENCES "User"(id),
    FOREIGN KEY ("serviceId") REFERENCES Service(id)
);

-- 6. สร้างตาราง TableCafe
CREATE TABLE TableCafe (
    id SERIAL PRIMARY KEY,
    "tableNumber" VARCHAR(20) UNIQUE NOT NULL,
    "currentSession" VARCHAR(255)
);

-- 7. สร้างตาราง MenuCategory
CREATE TABLE MenuCategory (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL
);

-- 8. สร้างตาราง MenuItem
CREATE TABLE MenuItem (
    id SERIAL PRIMARY KEY,
    "categoryId" INT NOT NULL,
    name VARCHAR(100) NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    "isAvailable" BOOLEAN DEFAULT TRUE,
    FOREIGN KEY ("categoryId") REFERENCES MenuCategory(id)
);

-- 9. สร้างตาราง Order (ใช้ "" ครอบเพราะเป็นคำสงวนใน SQL)
CREATE TABLE "Order" (
    id SERIAL PRIMARY KEY,
    "tableId" INT NOT NULL,
    status VARCHAR(50) DEFAULT 'Dining',
    "totalAmount" DECIMAL(10, 2) DEFAULT 0,
    FOREIGN KEY ("tableId") REFERENCES TableCafe(id)
);

-- 10. สร้างตาราง OrderItem
CREATE TABLE OrderItem (
    id SERIAL PRIMARY KEY,
    "orderId" INT NOT NULL,
    "menuItemId" INT NOT NULL,
    quantity INT DEFAULT 1,
    "kdsStatus" VARCHAR(50) DEFAULT 'Pending',
    FOREIGN KEY ("orderId") REFERENCES "Order"(id) ON DELETE CASCADE,
    FOREIGN KEY ("menuItemId") REFERENCES MenuItem(id)
);

-- 11. สร้างตาราง Payment
CREATE TABLE Payment (
    id SERIAL PRIMARY KEY,
    "referenceId" INT NOT NULL,
    "referenceType" VARCHAR(50) NOT NULL,
    amount DECIMAL(10, 2) NOT NULL,
    status VARCHAR(50) DEFAULT 'Success',
    "paidAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
