# Cafe & Studio

ระบบคาเฟ่และสตูดิโอถ่ายภาพ/แต่งหน้า สำหรับดูเมนู จองคิว สั่งอาหารด้วย QR จัดการงานครัว รับชำระเงิน และดูรายงานร้าน

## รูปแบบการทำงาน

- เป็นแอปเดียวด้วย ASP.NET Core 8 MVC, Razor Views และ C# ตามโครงสร้าง `dotnet new mvc -o MyWebApp`
- MVC อ่านและบันทึกข้อมูลลง SQLite โดยตรงผ่าน Entity Framework Core
- ไม่มี Web API, backend แยก, HTTP client ระหว่างโปรเจกต์, Supabase, Prisma หรือ PostgreSQL
- ไม่เรียก API หรือบริการภายนอกเพื่อให้ระบบหลักทำงาน
- QR Code สร้างภายในเครื่องด้วย QRCoder
- ตัวเว็บไม่ต้องใช้ Node.js, npm หรือ `node_modules`

## ความสามารถ

### ลูกค้าและบัญชี

- ผู้เข้าชมดูหน้าแรก เมนู บริการ และข้อมูลช่างได้
- สมัครและเข้าสู่ระบบด้วยอีเมลกับรหัสผ่าน
- แก้ชื่อ/เบอร์โทร ดูประวัติการจอง และออกจากระบบได้
- หลัง login ระบบส่งผู้ใช้ไปหน้าที่ตรงกับบทบาท

### จองคิวสตูดิโอ

- แสดงรายชื่อช่าง ความถนัด และ Portfolio เมื่อมีข้อมูล
- รอบเวลา 10:00–12:00, 13:00–15:00, 15:00–17:00 และ 17:00–19:00
- ตรวจคิวว่างตามช่างและวันที่
- ป้องกันการจองซ้ำด้วย unique index ใน SQLite; รายการที่ยกเลิกแล้วไม่กันคิว
- รายการเริ่ม `Pending`; แคชเชียร์บันทึกการรับชำระเป็น `Paid`; ช่างหรือผู้จัดการปิดงานเป็น `Completed`

### สั่งอาหารผ่าน QR

- พนักงานเปิดโต๊ะเพื่อสร้าง session token แบบสุ่มของรอบนั้น
- ลูกค้าสแกน QR เพื่อดูเมนูที่พร้อมขาย เพิ่มจำนวนและหมายเหตุ แล้วส่งรายการเข้าครัว
- ตรวจ token ทุกครั้งที่ดูหรือส่งรายการ และทำให้ QR เดิมหมดอายุหลังปิดบิล
- บันทึกราคาสินค้าตอนสั่งไว้ในรายการเพื่อไม่ให้ราคาใหม่เปลี่ยนยอดเก่า
- ลูกค้ากดเรียกเก็บเงินจากหน้าโต๊ะได้

### งานครัว แคชเชียร์ และจัดการร้าน

- KDS แสดงคิวตามเวลา หรือสรุปจำนวนเมนู
- สถานะรายการอาหารเปลี่ยนตามลำดับ `Pending` → `Cooking` → `Served`
- แคชเชียร์ดูบิลโต๊ะ รับเงินสดหรือบันทึกการรับ PromptPay ที่หน้าร้าน และปิดโต๊ะ
- แคชเชียร์บันทึกการชำระคิวสตูดิโอ
- Manager/Owner จัดการเมนู ราคา ความพร้อมขาย และบริการสตูดิโอ
- Owner เปลี่ยน role พนักงานและดูรายชื่อบัญชี
- Dashboard แสดงยอดวันนี้ จำนวนจอง โต๊ะที่เปิด และแยกรายได้ Cafe/Studio
- IT Staff ดูจำนวนข้อมูลระบบ โดยไม่มีสิทธิ์ดูรายงานการเงิน

## บทบาทผู้ใช้

ระบบมี 9 role: `Guest`, `Member`, `Artist`, `Waiter`, `Kitchen`, `Cashier`, `Manager`, `Owner` และ `IT Staff`

| บทบาท | สิทธิ์หลัก |
|---|---|
| Guest | ดูหน้าเว็บ เมนู บริการ และข้อมูลช่าง |
| Member | จองคิว ดูประวัติ และยกเลิกรายการ Pending ของตน |
| Artist | ดูตารางงานของตนและปิดงานที่ชำระแล้ว |
| Waiter | ดูและเปิดโต๊ะเพื่อสร้าง QR session |
| Kitchen | ดู KDS และเปลี่ยนสถานะอาหาร |
| Cashier | ดูบิล รับชำระโต๊ะและคิวสตูดิโอ |
| Manager | จัดการเมนู/บริการ ดูรายงาน และเข้าถึงงานครัว/หน้าร้าน |
| Owner | จัดการร้าน รายงานการเงิน และ role พนักงาน |
| IT Staff | ดูสถานะและจำนวนข้อมูลระบบ |

## หน้าเว็บไซต์

| URL | หน้า | สิทธิ์ |
|---|---|---|
| `/` | หน้าแรก เมนู บริการ และช่าง | ทุกคน |
| `/booking` | เลือกช่าง วันที่ และรอบ | ดูได้ทุกคน; ยืนยันจองต้อง login |
| `/my-bookings` | ประวัติการจอง | ผู้ใช้ที่ login |
| `/Account/Login` | เข้าสู่ระบบ | ทุกคน |
| `/Account/Signup` | สมัครสมาชิก | ทุกคน |
| `/Account/Profile` | ข้อมูลบัญชีและออกจากระบบ | ผู้ใช้ที่ login |
| `/table/{id}?session={token}` | เมนูและรายการของโต๊ะ | ผู้ถือ session token ที่ยังใช้งาน |
| `/tables` | เปิดโต๊ะและสร้าง QR | Waiter, Cashier, Manager, Owner |
| `/kds` | หน้าจอครัว | Kitchen, Manager, Owner |
| `/cashier` | หน้าจอแคชเชียร์ | Cashier, Manager, Owner |
| `/artist-dashboard` | ตารางงานช่าง | Artist, Manager, Owner |
| `/admin` | Dashboard และจัดการร้าน | Manager, Owner |
| `/it` | ข้อมูลสถานะระบบ | IT Staff, Manager, Owner |

## โครงสร้างโปรเจกต์

```text
MyWebApp/
  Controllers/       MVC controllers สำหรับหน้าเว็บและการส่งแบบฟอร์ม
  Data/              Entity, DbContext และ SQLite access
  Models/            View models และข้อมูลที่ใช้แสดงผล
  Services/          กฎธุรกิจและการอ่าน/เขียนฐานข้อมูลใน process เดียวกัน
  Views/             Razor pages
  wwwroot/           CSS, รูป และ static files
  Program.cs         ตั้งค่า MVC, session, SQLite และ seed data
CafeBookingOrdering.sln
DATA_DICTIONARY.md
REQUIREMENTS.md      ข้อกำหนดตั้งต้นของโครงงาน
```

## ติดตั้งและเริ่มใช้งาน

ต้องมี .NET 8 SDK ไม่ต้องติดตั้ง Node.js หรือ database server เพิ่ม

เปิด PowerShell ที่ root ของ repository แล้วรัน:

```powershell
dotnet run --project .\MyWebApp\MyWebApp.csproj
```

เว็บเปิดที่ [http://localhost:5002](http://localhost:5002)

Build solution:

```powershell
dotnet build .\CafeBookingOrdering.sln
```

เมื่อเริ่มครั้งแรก ระบบสร้าง SQLite database และข้อมูลตั้งต้น ได้แก่ role, เมนูตัวอย่าง, บริการ, โต๊ะ 8 โต๊ะ และข้อมูลช่างตัวอย่าง หากฐานข้อมูลมีข้อมูลอยู่แล้ว ระบบจะคงข้อมูลเดิมไว้และเพิ่มคอลัมน์ที่จำเป็นแบบ additive

## ฐานข้อมูล

- ไฟล์หลัก: `MyWebApp/Data/cafe.db`
- กำหนดตำแหน่งอื่นได้ด้วย environment variable `Database__Path`; path แบบ relative อิงจากโฟลเดอร์ `MyWebApp`
- MVC ใช้ `MyWebApp/Data/CafeDbContext.cs` เข้าถึงข้อมูลโดยตรง
- ตารางหลัก: `Role`, `User`, `StaffProfile`, `Service`, `Booking`, `TableCafe`, `MenuCategory`, `MenuItem`, `Order`, `OrderItem` และ `Payment`
- รายละเอียดฟิลด์อยู่ใน [DATA_DICTIONARY.md](DATA_DICTIONARY.md)
- ยังไม่มีระบบสำรองข้อมูลอัตโนมัติ; สำรองไฟล์ SQLite เมื่อหยุดแอปแล้ว

## ตั้งค่าบัญชี Owner

ก่อนเริ่มเว็บ ให้ตั้งรหัสผ่าน Owner ใน PowerShell (อย่างน้อย 12 ตัวอักษร):

```powershell
$env:BootstrapOwner__Password = "ใส่รหัสผ่านที่คาดเดายากอย่างน้อย 12 ตัวอักษร"
dotnet run --project .\MyWebApp\MyWebApp.csproj
```

อีเมลและชื่อเริ่มต้นกำหนดใน `MyWebApp/appsettings.json` ภายใต้ `BootstrapOwner` โดยค่าเริ่มต้นคือ `kachanop.peace@gmail.com` และ `Kachanop Sawasdee` ถ้าพบบัญชีนี้ในฐานข้อมูล ระบบกำหนด role เป็น Owner โดยไม่เปลี่ยนรหัสผ่านเดิม

อย่าบันทึกรหัสผ่านจริงใน `appsettings.json` หรือ commit ลง Git

## ความปลอดภัยในแอป local

- Password hash ด้วย `PasswordHasher<TUser>` ของ ASP.NET Core Identity
- login เก็บ user id ใน ASP.NET server-side session ผ่าน cookie แบบ HttpOnly; ไม่มี bearer token หรือ API session
- POST forms ใช้ Anti-forgery token
- role ตรวจจากข้อมูลผู้ใช้ใน SQLite และปรับ session role ให้ตรงกับฐานข้อมูลในแต่ละ request
- QR token สุ่มใหม่ทุกครั้งที่เปิดโต๊ะ และถูกล้างเมื่อปิดบิล
- ไม่ควรเปิดเว็บให้เข้าจากอินเทอร์เน็ตโดยไม่เพิ่ม HTTPS, การจัดการ secret และการสำรองข้อมูลให้เหมาะสม

## ข้อจำกัดปัจจุบัน

- ระบบไม่เรียก API ภายนอก; การชำระเงินเป็นการบันทึกผลโดยแคชเชียร์ ไม่มี payment gateway หรือ webhook
- KDS/แคชเชียร์รีเฟรชหน้าเป็นช่วงประมาณ 10 วินาที และหน้าโต๊ะประมาณ 15 วินาที ยังไม่มี push real-time
- ยังไม่มีการยืนยันอีเมล รีเซ็ตรหัสผ่าน หรือส่งข้อความแจ้งเตือน
- ใช้ SQLite บนเครื่องที่รันเว็บ จึงเหมาะกับการใช้งานในเครื่องหรือร้านขนาดเล็กที่ต้องการฐานข้อมูลไฟล์
