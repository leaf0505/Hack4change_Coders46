# DonationApp — Hack4Change 2026

## What it does
DonationApp connects community organisations with donors in real time.
Organisations post what they need. Donors give in under 60 seconds — 
no account required.

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)  
  Everything else installs automatically via NuGet on first run.  
  No database server, no Node.js, no external services required.

---

## How to Run

### 1. Clear the NuGet cache (important — avoids version conflicts)
```powershell
dotnet nuget locals all --clear
```

### 2. Open the solution in Visual Studio

Open `mvc_final_final.sln`.

### 3. Create and seed the database

In the **Package Manager Console** (Tools → NuGet Package Manager → 
Package Manager Console):
```powershell
Add-Migration Init
Update-Database
```

This creates a local `app.db` SQLite file and automatically seeds a 
demo organisation with sample needs. No manual setup required.

### 4. Run the project

Press **F5** or click the green play button.

The app opens at `http://localhost:5000`.

---

## Demo Account

| Role | Email | Password |
|------|-------|----------|
| Organisation | org@demo.com | demo123 |

Donors do not need an account — they can donate directly from 
the public page at `http://localhost:5000`.

---

## Dependencies

All installed automatically via NuGet:

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.0 | User authentication |
| Microsoft.EntityFrameworkCore.Sqlite | 9.0.0 | Local database |
| Microsoft.EntityFrameworkCore.Tools | 9.0.0 | Migrations |
| Microsoft.EntityFrameworkCore.Design | 9.0.0 | Scaffolding |
| EPPlus | 7.3.0 | Excel import (.xlsx) |

---

## Troubleshooting

**"Package X 10.0.5 is not compatible with net9.0"**  
Your local NuGet cache is forcing the wrong version. Run:
```powershell
dotnet nuget locals all --clear
dotnet restore --force
```

**"Failed to bind to address http://127.0.0.1:5000: address already in use"**  
Another instance is already running. Kill it:
```powershell
Get-Process -Name "mvc_final_final" | Stop-Process -Force
```
Then press F5 again.
##### Tech stack
- ASP.NET Core 9 MVC
- Entity Framework Core + SQLite
- EPPlus (Excel import)

###### What AI did:**
- Generated the majority of the view files (.cshtml) including layout, 
  styling, and Razor syntax
- Helped debug build errors
- Wrote the ExcelImportService parsing logic
- Suggested the surplus redistribution architecture in SurplusService
- Generated this README

######## What we did:**
- Defined the full data model and relationships between organisations, 
  needs and donors
- Made all architectural decisions (ASP.NET Core MVC, SQLite, 
  no-account donor flow, priority system)
- Reviewed, tested, and debugged every generated file before keeping it
- Identified when AI output was wrong and directed corrections
- Built and ran the application end to end  
