# BusTrips MVC (.NET 8, Razor UI + Identity + SQLite)

A runnable MVC web app with cshtml pages **and** the core bus-trips domain.
Includes: Identity (cookie auth), organizations, trips (with buckets), driver registration + admin approvals.

## How to run
1. Install **.NET 8 SDK**.
2. Unzip this folder.
3. Open a terminal:
   ```bash
   dotnet restore BusTrips.sln
   dotnet build BusTrips.sln
   cd src/BusTrips.Web
   dotnet run
   ```
4. Browse to the shown URL (e.g. `https://localhost:5001` or `http://localhost:5000`).  
   The SQLite DB `app.db` is created on first run.

**Seeded admin**
- Email: `admin@demo.local`
- Password: `Pass123$`

## Key pages
- `/Account/Login?role=customer|driver|admin` (wording changes by query string)
- `/Account/RegisterCustomer`, `/Account/RegisterDriver`, `/Account/AcceptTerms`
- `/Organizations` (list/create/invite)
- `/Trips?organizationId=<GUID>` (buckets: Draft/Upcoming/InProgress/Past + create)
- `/Driver/MyTrips`, `/Driver/Unassigned`
- `/Admin/Drivers` (approve/reject)

> Notes:
> - File uploads and rich text editors are not wired yet—add your preferred provider (Azure Blob/S3) & a sanitizer before prod.
> - Switch to SQL Server by changing the provider in `Program.cs` and connection string.
