# Tasks — KnowledgeTrackerDesktop

## Current Direction

The project uses a simple layered architecture:

```text
WinForms
    ↓
Application Services
    ↓
Repositories
    ↓
SQLite
```

Do not introduce MVVM unless a concrete requirement emerges.

---

# Critical

## 1. Fix Step Persistence

### Status: Completed

The repository now synchronizes the persisted Step collection transactionally
with `KnowledgeEntry.Steps`, normalizes order, and has integration coverage for
add, update, removal, reorder and rollback behavior. The original problem and
required behavior below are retained as project history.

### Problem

The UI can remove a Step from:

```text
KnowledgeEntry.Steps
```

but the current persistence flow may only insert/update remaining Steps.

A previously persisted Step can therefore remain in SQLite after being removed from the UI.

### Required Behavior

After saving:

```text
Database Steps == KnowledgeEntry.Steps
```

### Preferred Approach

Implement a transactional save operation.

For small collections, a valid strategy is:

```text
BEGIN TRANSACTION

Save KnowledgeEntry

DELETE FROM Step
WHERE KnowledgeEntryId = ?

INSERT current Steps

COMMIT
```

Normalize `Step.Order`.

Use a more sophisticated synchronization algorithm only if preserving Step IDs is required.

---

# Critical

## 2. Fix KnowledgeEntry Deletion

### Status: Completed

The repository now deletes Step rows, AttachmentInfo rows and the parent entry
in a single transaction. Integration tests verify child cleanup and rollback.
The original problem and required behavior below are retained as project
history.

### Problem

SQLite currently has no foreign-key cascade.

Deleting:

```text
KnowledgeEntry
```

can leave:

```text
Step
AttachmentInfo
```

records behind.

### Required Behavior

Deleting an entry must remove all related child records.

Use a transaction:

```text
BEGIN TRANSACTION

DELETE Steps
DELETE AttachmentInfo
DELETE KnowledgeEntry

COMMIT
```

Rollback on failure.

---

# High Priority

## 3. Centralize Save Logic

### Status: Completed

`KnowledgeEntryService.SaveAsync` delegates persistence of the complete entry
aggregate to the repository, which transactionally synchronizes the parent,
steps, and attachments. The original requirement is retained below as project
history.

Create a clear application operation:

```csharp
SaveAsync(KnowledgeEntry entry)
```

The operation should represent saving the complete logical entry:

```text
KnowledgeEntry
    + Steps
    + Attachments
```

Avoid requiring the UI to know how individual child records are persisted.

---

# High Priority

## 4. Centralize Delete Logic

### Status: Completed

`KnowledgeEntryService.DeleteAsync` delegates to the repository, which removes
the entry and its child rows in one transaction. The original requirement is
retained below as project history.

Create a clear operation:

```csharp
DeleteAsync(int id)
```

The operation must delete:

* KnowledgeEntry
* Steps
* AttachmentInfo

atomically.

---

# High Priority

## 5. Add Persistence Tests

### Status: Completed

Repository and service tests cover create/read/update/delete, step and
attachment synchronization, ordering, and transaction rollback. The scenarios
below are retained as the original test checklist.

Add tests for:

### Create

```text
Create entry
→ save
→ reload
→ verify entry
```

### Update

```text
Update entry
→ save
→ reload
→ verify fields
```

### Step removal

```text
Entry has A, B, C
→ remove B
→ save
→ reload
→ expect A, C
```

### Step reorder

```text
A, B, C
→ reorder
→ save
→ reload
→ verify Order
```

### Attachment synchronization

```text
Existing attachments
→ remove/add
→ save
→ reload
→ verify exact collection
```

### Delete

```text
Entry + Steps + Attachments
→ delete
→ verify all records removed
```

### Transaction failure

Where practical, verify that a failed multi-table operation does not leave partial data.

---

# Medium Priority

## 6. Review ResolutionSteps

The current schema contains:

```text
KnowledgeEntry.ResolutionSteps
```

and:

```text
Step
```

These may represent overlapping concepts.

Determine:

* whether `ResolutionSteps` is still used
* whether existing databases contain meaningful data
* whether it can be deprecated
* whether a migration is required

Do not remove it blindly.

---

# Medium Priority

## 7. Review Repository Schema Initialization

The repository contains special handling for the `Step` table when an existing table lacks the expected primary key.

Review this logic and ensure it:

* does not accidentally destroy valid user data
* behaves predictably on existing databases
* is covered by tests where practical

---

# Medium Priority

## 8. Review Relationship Indexes

Consider adding:

```sql
CREATE INDEX IX_Step_KnowledgeEntryId
ON Step(KnowledgeEntryId);

CREATE INDEX IX_AttachmentInfo_KnowledgeEntryId
ON AttachmentInfo(KnowledgeEntryId);
```

Only add them after confirming that they are appropriate for the expected dataset and query patterns.

---

# Medium Priority

## 9. Improve Async Lifecycle

Review methods such as:

```text
OnAppearing
LoadAsync
```

Avoid unnecessary `async void`.

Use:

```csharp
Task
```

for application-level asynchronous methods.

Only UI event handlers should normally be `async void`.

---

# Medium Priority

## 10. Improve Error Handling

Review:

* database failures
* save failures
* delete failures
* YouTube/network failures
* attachment/file failures

Ensure exceptions are not silently swallowed.

Provide meaningful UI feedback.

---

# UI Improvements

## 11. Main List

Potential improvements:

* clearer empty state
* loading state
* improved card hierarchy
* better favorite affordance
* keyboard navigation
* improved search feedback

---

## 12. Add/Edit

Potential improvements:

* clearer field grouping
* validation
* improved Markdown preview
* better YouTube feedback
* clearer save/cancel state
* attachment management

---

## 13. Manage Steps

Potential improvements:

* drag/reorder support
* clearer order display
* keyboard support
* improved empty state

---

## 14. Statistics

Potential improvements:

* clearer KPI cards
* more useful charts
* date range filtering
* project/tag exploration

Avoid adding statistics that do not provide practical value.

---

# Testing

## 15. Repository Tests

Test:

* create
* read
* update
* delete
* child synchronization
* search
* favorite
* transactions

Use isolated test databases.

---

## 16. Service Tests

Test:

* save orchestration
* delete orchestration
* statistics
* application rules

Mock repositories only where mocking provides useful isolation.

Do not mock SQLite unnecessarily in integration tests.

---

# Technical Debt

## 17. Review Abstractions

Remove abstractions that do not solve a real problem.

In particular, avoid introducing:

* unnecessary ViewModels
* generic repositories
* generic services
* unnecessary mediator/CQRS infrastructure

---

## 18. Review Direct Model Mutation

The current editing approach may mutate the KnowledgeEntry object while the user edits fields.

This is acceptable for the current application.

Do not introduce a copy/undo/change-tracking architecture unless a real requirement appears.

---

## 19. Review Search Strategy

Current datasets can reasonably be searched/filtered in memory.

If the database grows substantially, consider moving search/filtering to SQL.

Do not optimize before there is a measurable need.

---

# Features

## 20. Typed Project Origin

### Status: Completed

`KnowledgeEntry` records where the knowledge came from, per entry, through two
nullable columns:

```text
ProjectOriginKind      1 = LocalFolder, 2 = GitRepository, 3 = WebPage
ProjectOriginLocation  absolute path (local) or http(s) URL (git / web)
```

The rules live in `Services/ProjectOriginRules` and are enforced by
`Services/EntryValidator`: the kind and the location go together, a local
folder needs a rooted path, and the other kinds need an http(s) URL.

The origin is surfaced in the editor (kind + location + browse/open), on the
card, in the preview, in the library filter bar, in the search, in the
statistics breakdown and in the export/import format (version 2).

The location is a record, not a guarantee: opening a missing folder reports it
instead of failing silently, exactly like an attachment.

Deferred on purpose: cloning a repository, `owner/repo` normalisation, bulk
"apply this origin to every entry of the project" and advanced location
filters.

---

# Future Features

Potential future features:

* richer attachment management beyond the current add/open/remove UI
* Markdown export
* JSON export/import
* database backup/restore
* duplicate detection
* richer statistics
* keyboard shortcuts
* advanced tag management
* full-text search

These are not required unless explicitly requested.

---

# Completed / Existing

The following capabilities already exist or are part of the current application:

* KnowledgeEntry CRUD
* SQLite persistence
* Steps
* Attachments model
* favorites
* tags
* search
* sorting
* Markdown
* code snippets
* YouTube links
* typed project origin (local folder / git repository / web page)
* statistics
* navigation
* Syncfusion UI
* dependency injection

Do not rebuild existing functionality unnecessarily.

---

# Definition of Done

A task is complete when:

1. The requested behavior works.
2. Existing behavior is preserved unless intentionally changed.
3. Database consistency is preserved.
4. Related entities are handled correctly.
5. No unnecessary architecture was introduced.
6. The project builds successfully.
7. Relevant tests pass.
8. New persistence behavior has appropriate test coverage.
