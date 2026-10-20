# KnowledgeTrackerDesktop

![Platform: Windows](https://img.shields.io/badge/platform-Windows-0078D4?style=flat-square&logo=windows)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/10.0)
![UI: Windows Forms](https://img.shields.io/badge/UI-Windows%20Forms-512BD4?style=flat-square)
![Storage: SQLite](https://img.shields.io/badge/storage-SQLite-003B57?style=flat-square&logo=sqlite)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

**[Ler em português](README.pt-PT.md)**

KnowledgeTrackerDesktop is a Windows desktop application for recording and
finding technical problems and solutions. Keep troubleshooting notes, ordered
resolution steps, code snippets, tags, project details, and file references in
a personal SQLite knowledge base.



This project is licensed under the [MIT License](LICENSE).

## Features

- Search entries and combine the search with project, tag, and favorite filters.
- Sort by resolution date, title, or project.
- Create, edit, and delete entries with descriptions, Markdown, comments, code,
  technologies, and tags. Add a related YouTube tutorial or demonstration to
  an entry so the video supporting a solution is easy to find again alongside
  your notes.
- Manage ordered resolution steps and attach related files, such as logs or
  reference documents, so supporting material stays easy to find with the
  solution. Attachments store file paths; the files themselves are not copied
  into the database.
- Open an image gallery from the attachments section to browse supporting
  screenshots and pictures in a carousel and open the selected file directly.
- Preview a code snippet in a dedicated read-only window before copying it,
  alongside the Copy action in the editor.
- View statistics for entries, favorites, projects, tags, and activity over
  time.
- Switch the application language between English and Portuguese (Portugal).

## Entry editor tabs

The editor separates an entry into four tabs to keep related information
organized and easier to work with:

- **General** — the entry's title and metadata (project, date, technologies,
  tags, and favorite status), plus its Markdown description, code, and
  comments.
- **Attachments** — the files and images linked to the entry, with actions to
  open or remove them and to browse images in a gallery.
- **YouTube** — a video link that supports or demonstrates the solution, kept
  with the entry for later reference.
- **Resolutions** — the ordered troubleshooting steps that describe how the
  problem was solved.

These tabs are sections of the same entry, not separate records. Use **Save**
to persist the entry and its resolution steps together.

## Requirements

- Windows.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- A Syncfusion WinForms license key configured as the `SYNCFUSION_LICENSE_KEY`
  environment variable. The application checks for this key at startup.

Obtain and use a Syncfusion license according to Syncfusion's licensing terms.
Do not commit the key to the repository.

The key belongs to whoever holds the Syncfusion license, so a clean clone has no
key and the application will not start: `Program.Main()` looks for the key in the
environment and then in the assembly metadata, and if neither is present it shows
a "Syncfusion license key was not found" dialog and exits. Set the variable (or
produce a packaged build, which embeds the key) before running the app.

A free [Community License](https://www.syncfusion.com/products/communitylicense)
is sufficient for this application when you qualify for it (currently a company
or individual with less than $1M USD in gross revenue and no more than five
developers). Generate the license key from your Syncfusion account and use it as
the `SYNCFUSION_LICENSE_KEY` value. No Syncfusion control used here is restricted
by edition. If you stop qualifying, obtain a paid license instead.

## Build and run

Clone the repository and build the solution:

```powershell
git clone https://github.com/fauxtix/KnowledgeTrackerDesktop.git
cd KnowledgeTrackerDesktop
dotnet restore
dotnet build KnowledgeTrackerDesktop.slnx
```

Set `SYNCFUSION_LICENSE_KEY` in your user or machine environment, then start
the application:

```powershell
dotnet run --project KnowledgeTrackerDesktop\KnowledgeTrackerDesktop.csproj
```

For a temporary setting in the current PowerShell session:

```powershell
$env:SYNCFUSION_LICENSE_KEY = "your-license-key"
dotnet run --project KnowledgeTrackerDesktop\KnowledgeTrackerDesktop.csproj
```

## Tests

Run the test suite:

```powershell
dotnet test KnowledgeTrackerDesktop.Tests\KnowledgeTrackerDesktop.Tests.csproj
```

Run a single test by method name or test class:

```powershell
dotnet test KnowledgeTrackerDesktop.Tests\KnowledgeTrackerDesktop.Tests.csproj --filter "Name~TestMethodName"
dotnet test KnowledgeTrackerDesktop.Tests\KnowledgeTrackerDesktop.Tests.csproj --filter "FullyQualifiedName~TestClassName"
```

The tests use xUnit and temporary SQLite databases for persistence and
migration scenarios.

## Publish (MSIX)

The application is distributed as an MSIX package (self-contained `win-x64`, so
no .NET runtime is required on the target machine).

Requirements:

- Visual Studio with the **Windows application packaging project** workload and
  a matching Windows SDK (the plain `dotnet` CLI cannot build `.wapproj`
  packaging projects).
- A Syncfusion license key. It is embedded into the build at compile time, so
  published packages start without an environment variable on the target
  machine. The key is never committed to the repository.

Build and sign the package:

```powershell
$env:SYNCFUSION_LICENSE_KEY = "your-license-key"
.\scripts\build-msix.ps1 -CreateSelfSignedCertificate
```

`-CreateSelfSignedCertificate` creates a self-signed code-signing certificate
for local testing, exports it to `scripts\KnowledgeTrackerDev.pfx` (git-ignored)
and prints the command to trust it. The package is written to `AppPackages\`.

For a production release, replace the self-signed certificate with a trusted
code-signing certificate:

```powershell
.\scripts\build-msix.ps1 -PackageCertificateKeyFile C:\certs\my.pfx -PackageCertificatePassword "password"
```

The manifest `Publisher` in
`KnowledgeTrackerDesktop.Package\Package.appxmanifest` must match the
certificate subject.

Install the package locally (trust the certificate once, as administrator):

```powershell
Import-Certificate -FilePath .\scripts\KnowledgeTrackerDev.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
Add-AppxPackage .\AppPackages\KnowledgeTrackerDesktop.Package_1.0.2.0_x64_Test\KnowledgeTrackerDesktop.Package_1.0.2.0_x64.msix
```

(The `1.0.2.0` path above matches the current `Identity/@Version` in the
manifest; replace it with whatever version you built.)

The `AppPackages\..._Test\` folder also contains `Install.ps1` (and
`Add-AppDevPackage.ps1`) that install the package and its certificate in one
step.

## Data and privacy

The application is local-first; it does not currently provide cloud
synchronization. Debug builds create its SQLite database as `knowledge.db3` in
the solution root. Release and packaged builds store it under the per-user
LocalAppData folder at `%LOCALAPPDATA%\KnowledgeTrackerApp\knowledge.db3`
(MSIX-packaged builds get a package-private copy of that folder). This folder
is also used for the saved language preference. In Debug builds, an existing
database in this folder is copied into the solution root on first startup using
SQLite's backup API; the original is retained. `KnowledgeTrackerApp` is a
storage folder name retained for compatibility with existing installations,
not the current product name.

The local database and generated build files are excluded from Git. Back up
your database separately if you need to preserve your knowledge base.
Removing an entry or attachment reference does not delete the physical file
referenced by an attachment path.

## Project documentation

- [Architecture](KnowledgeTrackerDesktop/ARCHITECTURE.md)
- [Coding standards](KnowledgeTrackerDesktop/CODING_STANDARDS.md)
- [Product requirements](KnowledgeTrackerDesktop/PRD.md)
- [UI guidelines](KnowledgeTrackerDesktop/UI_GUIDELINES.md)
- [UI design specification](KnowledgeTrackerDesktop/UI_DESIGN_SPEC.md)
