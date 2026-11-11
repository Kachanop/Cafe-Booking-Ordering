# Cafe & Studio (C#)

ASP.NET Core Blazor Server application targeting .NET 8, with Entity Framework Core and Npgsql for PostgreSQL. The existing Supabase/PostgreSQL schema is retained.

## Run locally

Set the environment variables used by the existing project, then run:

```powershell
$env:DATABASE_URL = "postgresql://postgres:<password>@<project-ref>.supabase.co:5432/postgres"
$env:NEXT_PUBLIC_SUPABASE_URL = "https://<project-ref>.supabase.co"
$env:NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY = "sb_publishable_<key>"
dotnet run --project .\cafe-system-csharp.csproj
```

The home page reads available menu items from PostgreSQL. Supabase Auth login and member registration endpoints are included. Do not expose the PostgreSQL password in a `NEXT_PUBLIC_` variable.

This is the C# conversion workspace. The legacy Next.js and Vite applications remain alongside it while the remaining booking, table ordering, KDS, cashier and administration screens are ported.
