# Copilot instructions for KnowledgeTrackerDesktop

This repository is a Windows desktop knowledge-base app built with .NET 10, WinForms, Syncfusion, SQLite, and sqlite-net. The project-specific guidance in `KnowledgeTrackerDesktop/AGENTS.md` is the source of truth for repository conventions, and this file adds the repo-specific operational context future Copilot sessions need.

## Build, test, and validation

- Restore/build the solution:
  - `dotnet restore`
  - `dotnet build KnowledgeTrackerDesktop.slnx`
- Run the test project:
  - `dotnet test KnowledgeTrackerDesktop.Tests/KnowledgeTrackerDesktop.Tests.csproj`
- Run one test by name or class:
  - `dotnet test KnowledgeTrackerDesktop.Tests/KnowledgeTrackerDesktop.Tests.csproj --filter "Name~<TestMethodName>"`
  - `dotnet test KnowledgeTrackerDesktop.Tests/KnowledgeTrackerDesktop.Tests.csproj --filter "FullyQualifiedName~<TestClassName>"`
- There is no separate lint script in this repo. Prefer targeted `dotnet build` and `dotnet test` validation for meaningful code changes.
- To run the app locally, set `SYNCFUSION_LICENSE_KEY` in the environment before startup; `Program.Main()` checks for it and exits with an error dialog if it is missing.

## High-level architecture

The codebase intentionally follows a small layered architecture rather than a heavy framework pattern:

```text
WinForms UI
    ↓
Application Services
    ↓
Repositories
    ↓
SQLite
```

- `KnowledgeTrackerDesktop/Program.cs` is the entry point and composition root.
- `KnowledgeTrackerDesktop/Bootstrap/AppComposition.cs` registers the DI container for the app and wires the repository, services, navigation shell, and main UI.
- `KnowledgeTrackerDesktop/UI/Shell/frmMain.cs` acts as the shell and owns the main navigation host.
- `KnowledgeTrackerDesktop/Services/*` contains application-level orchestration (for example `KnowledgeEntryService` and `StatisticsService`).
- `KnowledgeTrackerDesktop/Data/*` contains SQLite persistence and schema migration logic.
- The app stores its SQLite database at the repo root as `knowledge.db3` during local development; schema initialization runs before the shell is shown.

Important domain shape:

- `KnowledgeEntry` is the aggregate root.
- `Step` and `AttachmentInfo` are child records owned by a `KnowledgeEntry`.
- `KnowledgeEntry.Steps` and `KnowledgeEntry.Attachments` are `[Ignore]` properties and are not SQLite columns.
- The application is responsible for referential integrity because SQLite does not enforce the relationship with foreign keys here.

## Key conventions that matter here

- Keep the layering strict: forms and controls own UI concerns, services own business logic, repositories own SQL and persistence details.
- Do not introduce MVVM, generic repositories, CQRS, or extra abstraction layers unless there is a concrete, project-specific reason.
- Prefer constructor injection and central dependency registration in `AppComposition`; avoid ad hoc object creation in forms.
- Preserve aggregate consistency when saving or deleting a `KnowledgeEntry`: updates must keep `KnowledgeEntry`, `Step`, and `AttachmentInfo` in sync in a transaction.
- Do not silently swallow exceptions. Surface them at an appropriate app/UI boundary, and avoid raw stack traces for normal users.
- Use async APIs for database and I/O work; avoid `.Result`, `.Wait()`, and `async void` except for actual UI event handlers.
- Respect the current SQLite/sqlite-net compatibility constraints: there is existing migration logic that checks table schema and rebuilds `Step` if needed. Do not remove or rewrite that behavior without understanding compatibility implications.
- `KnowledgeEntry.ResolutionSteps` exists alongside the `Step` table; avoid repurposing or deleting it without checking whether existing data or application code still depends on it.
- The app is intentionally simple and maintainable; prefer small, focused changes over broad refactors.

## Testing and change expectations

- Tests live under `KnowledgeTrackerDesktop.Tests` and use xUnit.
- If behavior changes touch persistence or schema, validate with the most targeted test or a small temporary database scenario where practical.
- Keep edits aligned with the existing layered architecture and the repository’s data-integrity rules.

## Source-of-truth project guidance

For architectural and database-specific constraints beyond this file, read:

- `KnowledgeTrackerDesktop/AGENTS.md`
- `KnowledgeTrackerDesktop/ARCHITECTURE.md`

Those files capture the repo’s explicit rules for architecture, persistence, UI boundaries, and migration safety.
