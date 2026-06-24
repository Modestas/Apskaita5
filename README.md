# Apskaita5

Lithuanian small business accounting software built with VB.NET and Windows Forms.

## Prerequisites

- Visual Studio 2015 or later (VB.NET support required)
- .NET Framework 4.x
- SQLite3 command-line tools

## Build Instructions

### 1. Download SQLite3 tools

Download the SQLite command-line tools and extract `sqlite3.exe` into a location on your `PATH` (e.g. `C:\Windows\System32` or any folder in your `PATH`):

**Download:** https://www.sqlite.org/2026/sqlite-tools-win-x64-3530200.zip

### 2. Build InvoiceInfo

Open and build `Source\InvoiceInfo\InvoiceInfo.sln` first. This project produces a dependency required by the main solution.

```
Source\InvoiceInfo\InvoiceInfo.sln
```

Build configuration: **Release | Any CPU**

### 3. Build Apskaita5

After InvoiceInfo builds successfully, open and build the main solution:

```
Source\Apskaita5\Apskaita5.sln
```

Build configuration: **Release | Any CPU** (or `Release | x86` for 32-bit)

The solution includes the following projects:

| Project | Description |
|---|---|
| AccDataAccessLayer | Database access layer (SQLite / MySQL / SQL Server) |
| ApskaitaObjects | Core business logic and domain objects |
| AccCommon | Shared utilities |
| AccMigration | Database migration helpers |
| AccControlsWinForms | Custom UI controls |
| AccDataBindingsWinForms | WinForms data bindings |
| AccPluginManager | Plugin loader |
| AccIPlugin | Plugin interface |
| AccDataProvider | Data provider abstraction |
| ApskaitaRemotingServer | Remoting server |
| AccWebService | Web service layer |
| Apskaita | Main WinForms application |

## External Libraries

Pre-built third-party DLLs are located in `Source\ExternalReferencedLibraries\` and are referenced directly — no NuGet restore is needed for them.

## Documentation

User documentation (Lithuanian): [Documentation/Documentation.md](Documentation/Documentation.md)

## License

Microsoft Public License (Ms-PL) — see [license.md](license.md)
