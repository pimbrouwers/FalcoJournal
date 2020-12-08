# FalcoJournal

A bullet journal built with Falco, .NET 5.x and ASP.NET Core

## Getting Started

```powershell
PS C:\> Get-Content -Raw -Path schema.sql | sqlite3 FalcoJournal.sqlite
```

Creating an `appsettings.Local.json` file and specify the complete filepath to the database within your valid connection string.

```powershell
PS C:\> dotnet restore
PS C:\> dotnet watch run
```

Profit.