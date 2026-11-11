# Cafe Frontend

Blazor Server UI. This project only calls the separate Backend API; it has no database or Supabase dependency.

Run the Backend first, then start the Frontend:

```powershell
dotnet run --project .\Frontend\CafeFrontend.csproj
```

Default local address: http://localhost:5002. Set `BackendUrl` in `appsettings.json` or `BackendUrl` environment variable if the API is hosted elsewhere.
