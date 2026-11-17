# Cafe Backend

ASP.NET Core 8 Web API. It owns the application database, SQLite file, authentication sessions, business validation and role checks. Start it with `dotnet run --project .\Backend\Backend.csproj`; default address is `http://localhost:5080`.

Public catalog endpoints are `/health`, `/api/menu`, `/api/services`, `/api/artists` and `/api/availability`. Member and staff actions are under `/api`; protected calls use `Authorization: Bearer <accessToken>`. Guest ordering proves the current table session token on each request.

The schema is initialized and safely extended at startup. The default local database is `Backend/Data/cafe.db`. Configure `BootstrapOwner:Password` using a local secret or `BootstrapOwner__Password` environment variable before the first start.
