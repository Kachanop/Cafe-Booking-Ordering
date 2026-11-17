# Cafe & Studio

ระบบคาเฟ่และสตูดิโอ ใช้ ASP.NET Core 8 MVC เป็นเว็บหลัก, ASP.NET Core Web API เป็น backend, Entity Framework Core และ SQLite เป็นฐานข้อมูลในเครื่อง ไม่มีการเรียกใช้ Supabase

## เริ่มระบบในเครื่อง

ติดตั้ง .NET 8 SDK ก่อน แล้วเปิด PowerShell 2 หน้าต่างจากโฟลเดอร์นี้

หน้าต่างที่ 1 — API และฐานข้อมูล:

```powershell
dotnet run --project .\Backend\Backend.csproj
```

Backend เปิดที่ `http://localhost:5080` และสร้าง/เติมข้อมูลเริ่มต้นไว้ที่ `Backend/Data/cafe.db` เมื่อเริ่มครั้งแรก

หน้าต่างที่ 2 — เว็บ MVC:

```powershell
dotnet run --project .\MyWebApp\MyWebApp.csproj
```

เปิดเว็บที่ [http://localhost:5002](http://localhost:5002)

## ตั้งบัญชีเจ้าของร้าน

กำหนดรหัสผ่านเจ้าของก่อนเริ่ม Backend โดยตั้งค่า environment variable ที่ไม่ commit ลง Git:

```powershell
$env:BootstrapOwner__Password = "รหัสผ่านที่คาดเดายากอย่างน้อย 12 ตัวอักษร"
dotnet run --project .\Backend\Backend.csproj
```

อีเมลเริ่มต้นคือ `kachanop.peace@gmail.com` ใน `Backend/appsettings.json` หากต้องการเปลี่ยนอีเมลหรือชื่อเจ้าของ แก้ค่าที่ `BootstrapOwner` ได้ เมื่อผู้ใช้สมัครด้วยอีเมลนี้ Backend จะกำหนดบทบาท Owner ให้อัตโนมัติขณะเริ่มระบบ

## ฟังก์ชันหลัก

- สมัคร/เข้าสู่ระบบและตั้งค่าบัญชี พร้อม role 9 ระดับ; รหัสผ่าน hash ด้วย ASP.NET Identity และ API ใช้ bearer session token
- ดูเมนู บริการ และช่าง, ตรวจรอบคิวว่าง, จองและยกเลิกคิว
- เปิดโต๊ะพร้อม QR session แบบใช้เฉพาะรอบ, สั่งอาหารพร้อมหมายเหตุ และเรียกเก็บเงิน
- KDS แบบคิว FIFO หรือสรุปเมนู พร้อมอัปเดต Pending → Cooking → Served
- แคชเชียร์ปิดบิลเงินสดหรือบันทึกการรับ PromptPay ที่หน้าร้าน และรับชำระคิวสตูดิโอ
- จัดการเมนู/ราคา/บริการ/สิทธิ์พนักงาน และดูรายได้ Cafe เทียบ Studio
- IT ดูจำนวนข้อมูลโดยไม่เห็นตัวเลขการเงิน

การยืนยัน PromptPay ยังเป็นการตรวจรับโดยแคชเชียร์ ไม่มี payment gateway หรือ webhook ต่ออยู่ในโปรเจกต์

## ตรวจ API

```powershell
npm install
npm run test:api
```

เปิด Postman แล้ว Import ไฟล์ `postman/cafe-system-local.collection.json` และ `postman/cafe-system-local.environment.json` จากนั้นเลือก environment `Cafe & Studio - Local C#` แล้วกด Run Collection ชุดนี้ส่งเฉพาะ GET requests และไม่สร้างข้อมูลธุรกิจ

Mocha workflow test ที่สร้างการจองและออเดอร์แยกไว้ในคำสั่ง `npm run test:api:workflow` ต้องรันกับฐานข้อมูลทดสอบที่แยกจากข้อมูลร้านเท่านั้น ตั้ง `Database__Path` ให้ชี้ไปไฟล์ชั่วคราวและตั้ง `SMOKE_ISOLATED_DB=true`, `API_BASE_URL`, `SMOKE_OWNER_PASSWORD` ก่อนรัน

## การตั้งค่า

- เว็บเปลี่ยน Backend URL และ Public URL ได้ใน `MyWebApp/appsettings.json`
- ฐานข้อมูลเป็นไฟล์ SQLite ที่ `Backend/Data/cafe.db`; กำหนด path อื่นได้ด้วย `Database__Path`
- ตั้งรหัสผ่าน Owner ด้วย secret manager หรือ environment variable เท่านั้น ห้ามเก็บ secret ใน `appsettings.json` หรือ commit ลง Git
