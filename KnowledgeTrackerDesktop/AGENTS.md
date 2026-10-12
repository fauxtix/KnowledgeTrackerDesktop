## Project

KnowledgeTrackerDesktop is a Windows desktop application for maintaining a personal knowledge base of 
solved technical problems and troubleshooting solutions.

The application is built with:

* .NET 10
* C#
* Windows Forms
* Syncfusion WinForms controls
* SQLite
* sqlite-net
* Dependency Injection
* xUnit/NUnit/MSTest as already configured by the repository

The application is a personal desktop application. Prefer simple, maintainable solutions over architectural complexity.

---

## Architecture

KnowledgeTracker uses a simple layered architecture.

```text
WinForms UI
    ↓
Application Services
    ↓
Repositories
    ↓
SQLite
```

Models are shared between the relevant layers.

The main responsibility of each layer is:

### WinForms

Responsible for:

* controls
* forms
* user interaction
* event handling
* presentation state
* navigation
* filtering and sorting presentation
* displaying validation/errors
* loading indicators
* UI-specific transformations

### Application Services

Responsible for:

* application operations
* coordinating repositories
* business rules involving multiple entities
* transactions
* consistency between KnowledgeEntry and child entities
* statistics aggregation
* operations that should not belong to a Form

### Repositories

Responsible for:

* SQLite access
* SQL queries
* persistence
* transactions
* loading related entities
* database-specific implementation details

### Models

Responsible for:

* representing domain/application data
* simple intrinsic rules
* entity state

Do not put SQL, SQLite transactions or persistence logic inside Forms or controls.

---

## Do Not Introduce MVVM Unnecessarily

Do not introduce ViewModels solely to follow an architectural pattern.

Do not introduce CommunityToolkit.Mvvm, ViewModels, mediators, CQRS, generic repositories or additional abstraction layers unless they solve a concrete problem in this project.

Forms may contain presentation logic.

The important boundary is:

```text
Presentation logic → WinForms
Application/business logic → Services
Persistence → Repositories
```

---

## Dependency Injection

Dependencies should be registered centrally during application startup.

Prefer constructor injection.

Forms and services should depend on abstractions where an abstraction provides a meaningful benefit.

Avoid creating unnecessary interfaces for classes that have no realistic alternative implementation.

---

## Database

Debug builds store the SQLite database in the solution root:

```text
<solution root>\knowledge.db3
```

Non-Debug builds store it under the per-user LocalAppData folder:

```text
%LOCALAPPDATA%\KnowledgeTrackerApp\knowledge.db3
```

This per-user, writable location is required for MSIX-packaged builds, whose
install directory is read-only (packaged apps also redirect LocalAppData to the
package's private folder). On first startup in Debug, existing data at
`%LOCALAPPDATA%\KnowledgeTrackerApp\knowledge.db3` is copied into the solution
root using SQLite's backup API; the original file is retained. The
`KnowledgeTrackerApp` folder name is a storage identifier retained for
compatibility, not the current product name. `AppLocalization` also stores
language preferences in this folder, so do not rename it without migrating
existing user data.

The database contains:

* `KnowledgeEntry`
* `Step`
* `AttachmentInfo`

Current relationships:

```text
KnowledgeEntry
    ├── 1:N Step
    └── 1:N AttachmentInfo
```

`Step.KnowledgeEntryId` and `AttachmentInfo.KnowledgeEntryId` represent these relationships.

SQLite currently does not enforce these relationships through foreign-key constraints.

Therefore:

> Referential integrity is an application/repository responsibility.

Do not assume SQLite will automatically remove child records.

---

## Persistence Rules

Operations involving a KnowledgeEntry and its child collections must maintain consistency.

Saving a KnowledgeEntry must synchronize:

* KnowledgeEntry
* Steps
* Attachments

Deleting a KnowledgeEntry must also remove:

* its Steps
* its Attachments

Multi-table operations must use a transaction.

Never leave partially persisted changes when an operation fails.

---

## Current Schema

### KnowledgeEntry

Columns:

* `Id`
* `Title`
* `Description`
* `DateResolved`
* `ProjectName`
* `ResolutionSteps`
* `Technologies`
* `Tags`
* `Comments`
* `CodeSnippet`
* `IsFavorite`
* `YouTubeUrl`

`Attachments` and `Steps` are `[Ignore]` properties and are not SQLite columns.

### Step

Columns:

* `Id`
* `Description`
* `Order`
* `KnowledgeEntryId`

### AttachmentInfo

Columns:

* `Id`
* `KnowledgeEntryId`
* `FileName`
* `FilePath`
* `DateAdded`

There are currently no explicit foreign-key constraints and no explicit non-primary-key indexes.

---

## Important Schema Considerations

`KnowledgeEntry.ResolutionSteps` exists alongside the `Step` table.

Do not remove or repurpose `ResolutionSteps` without first checking whether existing data or application code depends on it.

The `Step` table is the structured representation of individual resolution steps.

If a future migration removes `ResolutionSteps`, it must preserve existing data where necessary.

---

## SQLite / sqlite-net

The project uses sqlite-net model attributes.

Do not add database-specific assumptions that are not supported by the existing sqlite-net configuration.

There is existing repository logic that checks the `Step` table and recreates it if an existing table does not have the expected primary key.

Do not remove or rewrite this behavior without understanding its compatibility implications.

---

## Existing UI

The main UI consists of:

* `frmMain`
* `ctlMain`
* `ctlAddEdit`
* `ctlManageSteps`
* statistics UI/control (`ctlStatistics`)
* `ImageGalleryForm` (image attachment carousel)
* `CodePreviewForm` (read-only code snippet preview)
* `CarbonTheme` / `CarbonPalette` / `ButtonStyler` (theming and semantic button roles)
* navigation service
* supporting dialogs/services

The main screen displays KnowledgeEntry records as cards.

Users can:

* search
* filter by tags
* filter favorites
* sort entries
* open an entry
* create an entry
* edit an entry
* delete an entry
* toggle favorites
* view statistics

---

## Navigation

`frmMain` acts as the shell.

It contains the content area and navigation state.

Navigation is a UI concern.

Do not move navigation responsibilities into application services.

A navigation service may:

* resolve views from DI
* display a view
* manage navigation history
* go back

It should not perform database operations.

---

## Editing

The Add/Edit UI works with a KnowledgeEntry instance.

The UI may update the object's properties while editing.

Saving is the point where persistence is committed.

Do not introduce a complex change-tracking system unless the project actually requires one.

---

## Search and Filtering

Search/filter/sort operations that only affect the already loaded UI collection may remain in the UI.

Database-backed search belongs in the repository/service when the dataset becomes large enough to require it.

Do not prematurely move simple presentation filtering into SQL.

---

## Async

Use asynchronous APIs for database and I/O operations.

Avoid blocking calls such as:

```csharp
.Result
.Wait()
```

Do not use `async void` except for actual UI event handlers.

Prefer:

```csharp
private async Task LoadEntriesAsync()
```

over fire-and-forget application methods.

Exceptions from application/database operations must not be silently discarded.

---

## Error Handling

Do not silently swallow exceptions.

Database errors should be handled at an appropriate application/UI boundary and presented meaningfully to the user.

Do not show raw stack traces to normal users.

---

## Scope

Before changing architecture, ask:

1. Is the change solving an actual problem?
2. Does it reduce complexity?
3. Does it preserve existing behavior?
4. Does it preserve existing data?
5. Does it fit the current layered architecture?

Prefer small, focused changes.

Do not perform unrelated refactoring while implementing a feature.

---

## Validation

After meaningful code changes:

```bash
dotnet build
dotnet test
```

If tests are not present for the affected behavior, consider adding focused tests.

Database changes should be tested against a temporary/test database where practical.

---

## Packaging

The app is published as a self-contained `win-x64` MSIX package through the
Windows Application Packaging Project in `KnowledgeTrackerDesktop.Package`.
Packaging requires MSBuild from Visual Studio with the Windows application
packaging workload; the plain `dotnet` CLI cannot build `.wapproj` projects.
Use `scripts/build-msix.ps1` and set `SYNCFUSION_LICENSE_KEY` so the license key
is embedded at build time. The key must never be committed. Keep the MSIX
`Publisher` in `Package.appxmanifest` in sync with the signing certificate
subject.

---

## Priority

When requirements conflict, prioritize:

1. Data integrity
2. Correct functionality
3. Existing user-visible behavior
4. Simplicity
5. Maintainability
6. Performance optimization

Do not sacrifice data integrity for architectural elegance.
