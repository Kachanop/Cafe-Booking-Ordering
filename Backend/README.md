# Cafe Backend

ASP.NET Core 8 Web API with an application-owned SQLite database. The API creates and seeds `Backend/Data/cafe.db` on first start; no Supabase project or external database is needed.

Run locally:

```powershell
dotnet run --project .\Backend\Backend.csproj
```

Default API address: http://localhost:5080. Useful public endpoints are `/health`, `/api/menu`, `/api/services`, and `/api/artists`. Registration and login use `/api/auth/register` and `/api/auth/login`; protected endpoints accept the returned bearer token.
