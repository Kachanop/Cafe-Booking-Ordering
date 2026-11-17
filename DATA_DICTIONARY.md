# Data Dictionary

The current schema is defined in `MyWebApp/Data/CafeDbContext.cs`. The MVC application reads and writes the local SQLite file at `MyWebApp/Data/cafe.db`; there is no separate API or database service.

| Table | Purpose | Important fields |
|---|---|---|
| `Role` | Application roles | Unique `Name`; Guest, Member, Artist, Waiter, Kitchen, Cashier, Manager, Owner, IT Staff |
| `User` | Customers and staff | Unique `Email`, `PasswordHash`, `RoleId`, `FullName`, `Phone` |
| `StaffProfile` | Artist details | `UserId`, `Bio`, `PortfolioUrl`, `Specialties` |
| `Service` | Studio service catalog | `Name`, `Price` |
| `Booking` | Studio reservations | Customer/artist/service IDs, date, fixed time slot, status, total, note; active artist/date/slot combinations are unique |
| `TableCafe` | Cafe tables and active sessions | Table number, current random session token, open and bill-request times |
| `MenuCategory` | Food and drink categories | `Name` |
| `MenuItem` | Cafe catalog items | Category, name, price and availability |
| `Order` | Per-table bill groups | Table, Dining/Billing/Paid state, total amount and created time |
| `OrderItem` | Individual order lines | Menu item, quantity, unit-price snapshot, note and KDS status |
| `Payment` | Cashier-confirmed receipts | Reference type/id, amount, method, status and paid time |

Passwords are hashed with ASP.NET Core Identity's `PasswordHasher<TUser>`. Login state is kept in the MVC server-side session; no API bearer tokens or API login-session rows are used.
