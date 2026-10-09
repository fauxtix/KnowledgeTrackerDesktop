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
- View statistics for entries, favorites, projects, tags, and activity over
  time.
- Switch the application language between English and Portuguese (Portugal).

## Entry editor tabs

The editor separates an entry into three tabs to keep related information
organized and easier to work with:

- **General** — the entry's title and metadata (project, date, technologies,
  tags, and favorite status), plus its Markdown description, code, attachments,
  and comments.
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

## Data and privacy

The application is local-first; it does not currently provide cloud
synchronization. During development, its SQLite database is created as
`knowledge.db3` in the solution root. Deployed builds store the database beside
the application. On first startup, an existing database at
`%LOCALAPPDATA%\KnowledgeTrackerApp\knowledge.db3` is copied to the new location
using SQLite's backup API; the original is retained.
`KnowledgeTrackerApp` is the legacy LocalAppData folder name used to preserve
existing installations, not the current product name. The application also
continues to use this folder for its saved language preference.

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
