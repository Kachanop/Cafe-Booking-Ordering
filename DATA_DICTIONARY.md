# Data Dictionary

The authoritative schema is implemented by `Backend/Data/CafeDbContext.cs` and persisted in the local SQLite database `Backend/Data/cafe.db`. EF Core creates the schema and performs the small additive upgrades required by existing local database files.

| Table | Purpose | Important fields |
|---|---|---|
| `Role` | Access roles | `Name` is unique; roles include Member, Artist, Waiter, Kitchen, Cashier, Manager, Owner and IT Staff |
| `User` | Customers and staff | Unique `Email`, `PasswordHash`, `RoleId`, `FullName`, `Phone` |
| `StaffProfile` | Artist details | `UserId`, `Bio`, `PortfolioUrl`, `Specialties` |
| `Service` | Studio services | `Name`, `Price` |
| `Booking` | Studio reservations | Customer/artist/service IDs, date, fixed time slot, status, total and note; active artist/date/slot combinations are unique |
| `TableCafe` | Cafe tables and active sessions | Table number, current random session token, open and bill-request times |
| `MenuCategory` | Cafe catalog categories | Unique category name is managed by the API |
| `MenuItem` | Food and drink catalog | Category, name, price and availability |
| `Order` | Per-table bill groups | Table, Dining/Billing/Paid state and total amount |
| `OrderItem` | Individual cafe items | Menu item, quantity, price snapshot, note and KDS status |
| `Payment` | Cashier-confirmed receipts | Reference type/id, amount, method, status and paid time |
| `LoginSession` | Revocable API bearer sessions | SHA-256 token hash, user and expiry; raw bearer tokens are not stored in the database |

Database and authentication access are implemented in the C# Backend. The MVC application sends business requests to that API and does not connect directly to SQLite.
