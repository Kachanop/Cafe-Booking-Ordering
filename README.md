# Cafe & Studio — ระบบจองสตูดิโอและสั่งอาหาร

เว็บสำหรับธุรกิจคาเฟ่และสตูดิโอถ่ายภาพ/แต่งหน้า รวมการแสดงเมนู การจองคิวสตูดิโอ การสั่งอาหารที่โต๊ะผ่าน QR หน้าจอครัว การรับชำระเงิน และรายงานรายได้ไว้ในระบบเดียว

## สถานะและเทคโนโลยี

- เว็บ: ASP.NET Core 8 MVC จากโครงสร้าง `dotnet new mvc -o MyWebApp`, Razor Views, C#
- API: ASP.NET Core 8 Minimal API, C#
- ORM และฐานข้อมูล: Entity Framework Core 8 กับ SQLite
- QR: QRCoder
- ทดสอบ API: Postman collection และ Mocha (Node.js ใช้เฉพาะตอนรัน Mocha; ตัวเว็บ/API ไม่ต้องใช้ Node.js หรือ `node_modules`)
- ไม่ใช้ Supabase, Prisma หรือ PostgreSQL

เว็บ MVC ติดต่อ API ผ่าน HTTP; มีเพียง Backend ที่เข้าถึง SQLite โดยตรง

## ความสามารถของระบบ

### ลูกค้าและบัญชี

- ผู้เข้าชมดูหน้าแรก เมนู บริการ และข้อมูลช่างได้โดยไม่เข้าสู่ระบบ
- สมัคร/เข้าสู่ระบบด้วยอีเมลและรหัสผ่าน
- หน้าบัญชีแก้ชื่อและเบอร์โทรได้ และออกจากระบบได้
- หลังเข้าสู่ระบบ ระบบนำผู้ใช้ไปหน้าตามบทบาท

### จองคิวสตูดิโอ

- แสดงบริการและช่าง พร้อมประวัติย่อ ความถนัด และลิงก์ Portfolio เมื่อมีข้อมูล
- รอบจองคงที่ 10:00–12:00, 13:00–15:00, 15:00–17:00 และ 17:00–19:00
- ตรวจคิวว่างตามช่างและวันที่
- สมาชิกส่งคำขอจองและดู/ยกเลิกรายการที่ยังรอชำระได้
- ฐานข้อมูลมี unique index ป้องกันการจองซ้ำของช่าง/วัน/รอบเดียวกัน โดยยกเว้นรายการที่ยกเลิกแล้ว
- การจองเริ่มสถานะ `Pending`; แคชเชียร์บันทึกการรับชำระเต็มจำนวนเป็น `Paid`; ช่างหรือผู้จัดการปิดงานเป็น `Completed`

### สั่งอาหารที่โต๊ะ

- พนักงานเปิดโต๊ะเพื่อออก session token แบบสุ่มสำหรับรอบลูกค้าปัจจุบัน
- QR Code เปิดหน้าสั่งอาหารของโต๊ะและใช้ token ตรวจสอบทุกครั้ง
- หน้าโต๊ะแสดงเมนูที่พร้อมขาย แบ่งหมวด พร้อมราคา จำนวน และหมายเหตุ เช่น ระดับความหวาน
- ระบบบันทึกราคา ณ เวลาสั่งไว้ในรายการออเดอร์
- ลูกค้ากดเรียกเก็บเงินเพื่อส่งบิลไปยังหน้าจอแคชเชียร์
- เมื่อแคชเชียร์ปิดบิล ระบบปิดออเดอร์ ล้าง token และทำให้ QR เดิมใช้ไม่ได้

### หน้าจอครัวและแคชเชียร์

- KDS แสดงออเดอร์ตามลำดับเวลา หรือสรุปจำนวนเมนู
- สถานะอาหารเปลี่ยนตามลำดับ `Pending` → `Cooking` → `Served`
- แคชเชียร์ดูยอดโต๊ะที่เรียกเก็บเงิน รับเงินสด หรือบันทึกการรับ PromptPay ที่หน้าร้าน
- แคชเชียร์รับชำระคิวสตูดิโอและปิดสถานะรายการ

### จัดการร้านและรายงาน

- Manager/Owner จัดการรายการเมนู ราคา ความพร้อมขาย และบริการสตูดิโอ
- Owner เปลี่ยนบทบาทพนักงานและดูรายชื่อบัญชี
- Dashboard แสดงยอดวันนี้ จำนวนการจอง โต๊ะที่เปิด และแยกรายได้ Cafe/Studio
- IT Staff ดูจำนวนข้อมูลระบบ โดยไม่มี endpoint รายงานรายได้สำหรับบทบาท IT Staff

## บทบาทและสิทธิ์

มีบทบาท 9 ระดับ ชื่อบทบาทที่เก็บในฐานข้อมูลคือ `Guest`, `Member`, `Artist`, `Waiter`, `Kitchen`, `Cashier`, `Manager`, `Owner` และ `IT Staff`

| บทบาท | งานหลัก |
|---|---|
| Guest | ดูหน้าเว็บ เมนู บริการ และข้อมูลช่าง |
| Member | จองคิว ดูประวัติ และยกเลิกการจองที่ยังไม่ชำระ |
| Artist | ดูตารางงานของตนและปิดงานที่ชำระแล้ว |
| Waiter | ดูโต๊ะและเปิดโต๊ะเพื่อสร้าง QR session |
| Kitchen | ใช้ KDS และอัปเดตสถานะอาหาร |
| Cashier | ดูบิล รับชำระโต๊ะ และบันทึกการชำระคิวสตูดิโอ |
| Manager | จัดการเมนู/บริการ ดูรายงาน และเข้าถึงงานหน้าร้าน/ครัว |
| Owner | สิทธิ์จัดการทั้งหมด รวมการเงินและบทบาทพนักงาน |
| IT Staff | ดูภาพรวมจำนวนข้อมูลทางเทคนิค |

การตรวจ role ทำที่ Backend API ด้วย bearer token และบางหน้าตรวจซ้ำที่ MVC ก่อนเรียก API

## การยืนยันตัวตนและความปลอดภัย

- รหัสผ่าน hash ด้วย `PasswordHasher<TUser>` ของ ASP.NET Core Identity
- ตอน login/register API คืน bearer token แบบสุ่ม; ฐานข้อมูลเก็บ SHA-256 hash ของ token แทน token จริง และ token หมดอายุใน 14 วัน
- MVC เก็บ token ไว้ใน server-side session; browser ได้ session cookie แบบ HttpOnly
- POST forms ของเว็บ MVC ใช้ Anti-forgery token
- API ตรวจ role สำหรับข้อมูลการเงิน การจัดการร้าน งานครัว และรายการจอง; Member/Artist ถูกกรองรายการตามบัญชีของตน
- QR ของโต๊ะเป็น session token แบบสุ่ม เปลี่ยนทุกครั้งที่เปิดโต๊ะ และถูกล้างเมื่อปิดบิล
- ห้ามนำ SQLite database, owner password หรือ bearer token ขึ้น Git

## โครงสร้างโฟลเดอร์

```text
Backend/                 C# API, EF Core, SQLite, auth และกฎธุรกิจ
  Data/                  CafeDbContext และการสร้าง/ปรับ schema
  Models/                Entity ของฐานข้อมูล
  Security/              Bearer token authentication
  Program.cs             API routes และการเริ่มฐานข้อมูล
MyWebApp/                เว็บหลักที่สร้างจาก dotnet MVC template
  Controllers/           Account, Booking, Home, Operations, Table
  Models/                View models และ DTO สำหรับเว็บ
  Services/              HTTP client สำหรับเรียก Backend
  Views/                 หน้า Razor MVC
  wwwroot/               CSS และรูปที่เว็บใช้
postman/                 Collection และ environment สำหรับ Postman
test/                    Mocha smoke checks และ isolated workflow check
CafeBookingOrdering.sln  Solution รวม Backend และ MyWebApp
DATA_DICTIONARY.md       ตารางและฟิลด์ฐานข้อมูล
REQUIREMENTS.md          เอกสารความต้องการเดิมของระบบ
```

## ความต้องการก่อนเริ่ม

- .NET 8 SDK
- Windows PowerShell ตามตัวอย่างด้านล่าง (ระบบปฏิบัติการอื่นใช้ shell ที่ตั้งค่า environment variable ได้)
- Node.js และ npm เฉพาะเมื่อต้องการรัน Mocha
- Postman เฉพาะเมื่อต้องการ import collection แล้วทดสอบจาก Postman

ไม่ต้องติดตั้งฐานข้อมูลแยก เพราะระบบใช้ SQLite และสร้างไฟล์ฐานข้อมูลเมื่อ Backend เริ่มครั้งแรก

## เริ่มระบบในเครื่อง

เปิด PowerShell 2 หน้าต่างจากโฟลเดอร์ root ของ repository

### หน้าต่างที่ 1: Backend API

```powershell
dotnet run --project .\Backend\Backend.csproj
```

API ใช้ `http://localhost:5080` โดยค่าเริ่มต้น และ endpoint ตรวจสถานะคือ `GET http://localhost:5080/health`

### หน้าต่างที่ 2: MVC website

```powershell
dotnet run --project .\MyWebApp\MyWebApp.csproj
```

เปิดเว็บที่ [http://localhost:5002](http://localhost:5002)

Build ทั้ง solution ได้จาก root:

```powershell
dotnet build .\CafeBookingOrdering.sln
```

ข้อมูลเริ่มต้น (เมนู บริการ โต๊ะ และตัวอย่างช่าง) ถูกเพิ่มเมื่อฐานข้อมูลยังไม่มีข้อมูลประเภทนั้น ระบบไม่ลบฐานข้อมูลเดิมเมื่อเริ่มใหม่

## สร้างหรือกำหนดบัญชี Owner

กำหนดรหัสผ่านด้วย environment variable ก่อนเริ่ม Backend; ต้องยาวอย่างน้อย 12 ตัวอักษร:

```powershell
$env:BootstrapOwner__Password = "ใส่รหัสผ่านที่คาดเดายากอย่างน้อย 12 ตัวอักษร"
dotnet run --project .\Backend\Backend.csproj
```

อีเมลและชื่อเริ่มต้นอยู่ใน `Backend/appsettings.json` ภายใต้ `BootstrapOwner` (ค่าเริ่มต้น `kachanop.peace@gmail.com`) เมื่อเริ่มระบบ Backend จะสร้างบัญชีนี้ถ้ายังไม่มี หรือกำหนด role `Owner` ให้บัญชีอีเมลนี้ถ้ามีอยู่แล้ว หากบัญชีมีรหัสผ่านเดิม ต้องเข้าสู่ระบบด้วยรหัสผ่านเดิม; การกำหนด Owner ไม่ได้เปลี่ยนรหัสผ่านของบัญชีที่มีอยู่

อย่าใส่รหัสผ่านจริงใน `appsettings.json` หรือ commit ลง Git

## หน้าของเว็บไซต์

| URL | เนื้อหา | สิทธิ์ |
|---|---|---|
| `/` | หน้าแรก เมนู บริการ และช่าง | สาธารณะ |
| `/booking` | เลือกช่าง วันที่ และรอบจอง | ดูได้สาธารณะ; สร้างรายการต้องเข้าสู่ระบบ |
| `/my-bookings` | ประวัติและยกเลิกการจอง | Member ที่เข้าสู่ระบบ |
| `/Account/Login` | เข้าสู่ระบบ | สาธารณะ |
| `/Account/Signup` | สมัครสมาชิก | สาธารณะ |
| `/Account/Profile` | แก้ชื่อ/เบอร์และออกจากระบบ | ผู้เข้าสู่ระบบ |
| `/table/{id}?session={token}` | เมนูและบิลของโต๊ะรอบปัจจุบัน | ผู้ถือ session token ที่ยังใช้งาน |
| `/tables` | เปิดโต๊ะและ QR | Waiter, Cashier, Manager, Owner |
| `/kds` | หน้าจอครัวแบบ FIFO หรือสรุปเมนู | Kitchen, Manager, Owner |
| `/cashier` | บิลโต๊ะและคิวสตูดิโอรอชำระ | Cashier, Manager, Owner |
| `/artist-dashboard` | ตารางงานและปิดงาน | Artist, Manager, Owner |
| `/admin` | รายงานและจัดการร้าน | Manager, Owner |
| `/it` | จำนวนข้อมูลและสถานะระบบ | IT Staff, Manager, Owner |

URL ที่ต้องใช้ session/token สำหรับโต๊ะจะถูกสร้างตอนเปิดโต๊ะ ห้ามใช้ token เก่าเป็น QR ถาวร

## API สำหรับ Postman และ client อื่น

Base URL เริ่มต้นคือ `http://localhost:5080` ถ้า endpoint ต้องยืนยันตัวตน ให้ส่ง header `Authorization: Bearer <accessToken>` ซึ่งได้จาก login หรือ register

### Public endpoints

| Method | Path | จุดประสงค์ |
|---|---|---|
| GET | `/health` | ตรวจ API และ SQLite |
| GET | `/api/menu` | เมนูที่พร้อมขาย |
| GET | `/api/services` | บริการสตูดิโอ |
| GET | `/api/artists` | ข้อมูลช่าง |
| GET | `/api/availability?staffId={id}&date=YYYY-MM-DD` | รอบว่างของช่างในวันนั้น |
| GET | `/api/tables/{id}/session?session={token}` | ข้อมูลโต๊ะ เมนู และออเดอร์ของ session |
| POST | `/api/tables/{id}/orders` | ส่งรายการอาหารของโต๊ะ |
| POST | `/api/tables/{id}/bill` | เรียกเก็บเงินโดยใช้ session token |

ตัวอย่าง body สำหรับสั่งอาหาร:

```json
{
  "session": "token-from-open-table",
  "items": [
    { "menuItemId": 1, "quantity": 2, "note": "หวานน้อย" }
  ]
}
```

ตัวอย่างสมัครสมาชิกและเข้าสู่ระบบ:

```http
POST /api/auth/register
Content-Type: application/json

{"email":"member@example.com","password":"YourStrongPassword!","fullName":"ชื่อสมาชิก"}
```

```http
POST /api/auth/login
Content-Type: application/json

{"email":"member@example.com","password":"YourStrongPassword!"}
```

ใช้ `accessToken` จากคำตอบใน header `Authorization: Bearer <accessToken>` สำหรับ API ที่ต้องเข้าสู่ระบบ

ตัวอย่างสร้าง booking (วันที่ต้องไม่ใช่วันย้อนหลัง และ `timeSlot` ต้องตรงกับรอบที่ระบบกำหนด):

```json
{
  "staffId": 1,
  "serviceId": 1,
  "bookingDate": "2030-12-01",
  "timeSlot": "10:00-12:00",
  "note": "รายละเอียดเพิ่มเติม"
}
```

ตัวอย่างใช้วันที่ในอนาคตเพื่อให้รูปแบบ request ใช้ได้; เปลี่ยนเป็นวันที่ต้องการจองจริง

### Authentication และบัญชี

| Method | Path | สิทธิ์ |
|---|---|---|
| POST | `/api/auth/register` | สาธารณะ; สมัครเป็น Member |
| POST | `/api/auth/login` | สาธารณะ; รับ bearer token |
| GET | `/api/me` | เข้าสู่ระบบ |
| POST | `/api/auth/logout` | เข้าสู่ระบบ; เพิกถอน session token |
| GET | `/api/profile` | เข้าสู่ระบบ |
| PUT | `/api/profile` | เข้าสู่ระบบ; เปลี่ยนชื่อ/เบอร์โทร |

### Booking, tables, KDS, cashier และ admin

| Method | Path | สิทธิ์ |
|---|---|---|
| GET | `/api/bookings` | Member เห็นของตน; Artist เห็นงานของตน; Waiter, Cashier, Manager, Owner เห็นรายการสำหรับงานที่รับผิดชอบ |
| POST | `/api/bookings` | Member, Artist, Waiter, Manager, Owner |
| POST | `/api/bookings/{id}/cancel` | Member ยกเลิกของตน; Artist ยกเลิกงานของตน; Waiter, Manager, Owner ช่วยยกเลิกรายการ Pending ได้ |
| GET | `/api/tables` | เข้าสู่ระบบ |
| POST | `/api/tables/{id}/open` | Waiter, Cashier, Manager, Owner |
| GET | `/api/kds` | Kitchen, Manager, Owner |
| GET | `/api/kds/summary` | Kitchen, Manager, Owner |
| POST | `/api/kds/{lineId}/status` | Kitchen, Manager, Owner |
| GET | `/api/cashier` | Cashier, Manager, Owner |
| POST | `/api/tables/{id}/close` | Cashier, Manager, Owner |
| POST | `/api/bookings/{id}/paid` | Cashier, Manager, Owner |
| POST | `/api/bookings/{id}/complete` | Artist, Manager, Owner |
| GET | `/api/admin/overview` | Manager, Owner |
| GET | `/api/admin/users` | Owner |
| PUT | `/api/admin/users/{id}/role` | Owner |
| GET/POST | `/api/admin/menu` | Manager, Owner |
| PUT | `/api/admin/menu/{id}` | Manager, Owner |
| GET/POST | `/api/admin/services` | Manager, Owner |
| PUT | `/api/admin/services/{id}` | Manager, Owner |
| GET | `/api/it/overview` | IT Staff, Manager, Owner |

ช่องทางรับชำระที่ API รองรับคือ `CASH` และ `PROMPTPAY` (การยืนยันโดยพนักงานหน้าร้าน) สถานะที่ไม่ถูกต้องหรือ session QR หมดอายุจะตอบกลับเป็น HTTP error เช่น `400`, `401`, `403`, `404` หรือ `409` ตามกรณี

## ฐานข้อมูลและการเปลี่ยน schema

- ค่าเริ่มต้น: `Backend/Data/cafe.db`
- กำหนด path ใหม่ด้วย `Database__Path`; path แบบ relative จะอิงจากโฟลเดอร์ Backend
- ตารางหลัก: `Role`, `User`, `StaffProfile`, `Service`, `Booking`, `TableCafe`, `MenuCategory`, `MenuItem`, `Order`, `OrderItem`, `Payment`, `LoginSession`
- รายละเอียดคอลัมน์และความสัมพันธ์อยู่ใน [DATA_DICTIONARY.md](DATA_DICTIONARY.md)
- Backend ใช้ `EnsureCreated` และเพิ่มคอลัมน์ที่ต้องรองรับฐานข้อมูลเดิมตอนเริ่มระบบ; โปรเจกต์ยังไม่มีชุด EF Core migrations สำหรับ migration ซับซ้อน
- อย่าลบหรือแทนที่ไฟล์ฐานข้อมูลที่มีข้อมูลร้านเพื่อแก้ปัญหาโดยไม่สำรองข้อมูลก่อน

## การตั้งค่า

| Setting | ค่าเริ่มต้น | ใช้กับ |
|---|---|---|
| `Database:Path` / `Database__Path` | `Data/cafe.db` (relative to Backend) | ที่อยู่ไฟล์ SQLite |
| `BootstrapOwner:Email` | `kachanop.peace@gmail.com` | อีเมลบัญชี Owner |
| `BootstrapOwner:FullName` | `Kachanop Sawasdee` | ชื่อ Owner ตอนสร้างบัญชี |
| `BootstrapOwner:Password` / `BootstrapOwner__Password` | ไม่มีค่า | รหัสผ่าน Owner; ต้องตั้งผ่าน secret/env |
| `FrontendUrl` | `http://localhost:5002` | CORS origin ที่ API อนุญาต |
| `BackendUrl` | `http://localhost:5080/` | URL ที่ MVC ใช้เรียก API |
| `PublicBaseUrl` | `http://localhost:5002` | URL ที่ใช้ทำ QR ของโต๊ะ |

รูปแบบ `__` ใช้แทน `:` เมื่อตั้งค่า .NET ผ่าน environment variable เช่น `Database__Path`

## ทดสอบ API

### Mocha แบบอ่านข้อมูล

ต้องเปิด Backend และ MVC ไว้ก่อน จาก root:

```powershell
npm install
npm run test:api
```

ชุดนี้ตรวจ health, เมนู, บริการ, ช่าง, หน้า MVC และการป้องกันหน้า Admin โดยไม่สร้างข้อมูลธุรกิจ การติดตั้ง npm dependencies ใช้เฉพาะ test tooling; ไม่มีผลต่อการรันเว็บ C#

### Mocha workflow บนฐานข้อมูลแยก

Workflow test จะสมัครผู้ใช้ สร้าง booking/order เปลี่ยนสถานะ KDS และบันทึกการชำระเงิน **ห้ามรันกับฐานข้อมูลจริงหรือฐานข้อมูลที่มีข้อมูลร้าน** ใช้ไฟล์ทดสอบใหม่เท่านั้น

หน้าต่าง PowerShell แรก:

```powershell
$testDb = Join-Path $env:TEMP 'cafe-workflow-test.db'
if (Test-Path $testDb) { throw "ไฟล์ทดสอบมีอยู่แล้ว: $testDb" }
$env:Database__Path = $testDb
$env:BootstrapOwner__Password = "SmokeOwnerPass-2026!"
dotnet run --project .\Backend\Backend.csproj
```

หน้าต่างที่สอง:

```powershell
$env:API_BASE_URL = "http://localhost:5080"
$env:SMOKE_OWNER_PASSWORD = "SmokeOwnerPass-2026!"
$env:SMOKE_ISOLATED_DB = "true"
npm install
npm run test:api:workflow
```

หลังทดสอบ ให้หยุด Backend ก่อน แล้วค่อยลบไฟล์ `cafe-workflow-test.db` พร้อมไฟล์ `-wal`/`-shm` ที่อาจเกิดขึ้นในโฟลเดอร์ temp

### Postman

1. เปิด Postman แล้ว Import `postman/cafe-system-local.collection.json`
2. Import `postman/cafe-system-local.environment.json`
3. เลือก environment `Cafe & Studio - Local C#`
4. เริ่ม Backend และ MVC แล้ว Run Collection

Collection ปัจจุบันใช้ GET requests สำหรับ health/catalog/pages และตรวจว่าหน้า Admin ปฏิเสธผู้ใช้ที่ยังไม่ได้ login

## ข้อจำกัดที่ยังต้องพัฒนาต่อ

- PromptPay เป็นการบันทึกผลรับชำระโดยพนักงาน ไม่มี payment gateway, QR payment provider หรือ webhook
- KDS และแคชเชียร์รีเฟรชเป็นช่วง (KDS/แคชเชียร์ประมาณ 10 วินาที; หน้าโต๊ะประมาณ 15 วินาที) ยังไม่มี push/WebSocket แบบ real-time ต่ำกว่า 1 วินาที
- SQLite อยู่ในเครื่อง/เซิร์ฟเวอร์ที่รัน Backend; ยังไม่มีระบบสำรองข้อมูลอัตโนมัติหรือการสำรองขึ้น cloud
- การยืนยันอีเมล การรีเซ็ตรหัสผ่าน และการส่งข้อความ/อีเมลแจ้งเตือนยังไม่มีบริการเชื่อมต่อ
- Booking เริ่มเป็น Pending และชำระผ่านแคชเชียร์; ยังไม่มีการหักเงินออนไลน์ทันทีตอนลูกค้ากดจอง

## เอกสารเพิ่มเติม

- [DATA_DICTIONARY.md](DATA_DICTIONARY.md) — schema และความหมายของข้อมูล
- [REQUIREMENTS.md](REQUIREMENTS.md) — ความต้องการเดิมของระบบ
- [Backend/README.md](Backend/README.md) — รายละเอียด API แบบย่อ
- [Backend/Backend.http](Backend/Backend.http) — ตัวอย่าง GET requests สำหรับ REST Client ที่รองรับไฟล์ `.http`
