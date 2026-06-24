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

Build configuration: **Release | x86** (the installer scripts expect the `x86\Release` output path)

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

## Building the Installer

### 1. Install Inno Setup

Download and install Inno Setup 6:

**Download:** https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe

### 2. Compile the installer script

Two installer scripts are provided in `Source\`:

| Script | Output file | Purpose |
|---|---|---|
| `Apskaita5.iss` | `InnoOutput\Apskaita5_setup_full.exe` | Full installer (first-time install) |
| `Apskaita5_update.iss` | `InnoOutput\Apskaita5_setup.exe` | Update/patch installer |

Open the desired `.iss` file in the Inno Setup IDE and press **Build → Compile** (or run from the command line):

```
iscc Source\Apskaita5.iss
```

The compiled installer will appear in the `InnoOutput\` folder at the repository root.

> The installer scripts expect the `Release|x86` build output from step 3 above, so make sure `Apskaita5.sln` was built with that configuration before compiling the installer.

## External Libraries

Pre-built third-party DLLs are located in `Source\ExternalReferencedLibraries\` and are referenced directly — no NuGet restore is needed for them.

## Documentation

User documentation (Lithuanian): [Documentation/Documentation.md](Documentation/Documentation.md)

## License

Microsoft Public License (Ms-PL) — see [license.md](license.md)
