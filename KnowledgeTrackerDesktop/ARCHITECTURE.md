# Architecture — KnowledgeTrackerDesktop

## 1. Architectural Style

KnowledgeTrackerDesktop uses a simple layered architecture designed for a small Windows Forms desktop application.

```text
┌──────────────────────────────┐
│          WinForms            │
│                              │
│ Forms / Controls / UI        │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│     Application Services     │
│                              │
│ KnowledgeEntryService        │
│ StatisticsService            │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│         Repositories         │
│                              │
│ KnowledgeRepositorySQLite    │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│           SQLite             │
│                              │
│ KnowledgeEntry               │
│ Step                         │
│ AttachmentInfo               │
└──────────────────────────────┘
```

This project intentionally does not require MVVM.

---

## 2. Responsibilities

### WinForms Layer

The WinForms layer owns:

* controls
* forms
* user interaction
* event handlers
* visual state
* navigation
* dialogs
* presentation filtering
* presentation sorting
* rendering
* loading/error feedback

Forms may contain non-trivial presentation logic.

Forms must not contain:

* SQL
* SQLite connection management
* database transactions
* persistence rules
* complex multi-entity business rules

---

## 3. Application Services

Application services coordinate application-level operations.

Examples:

```text
IKnowledgeEntryService
KnowledgeEntryService

IStatisticsService
StatisticsService
```

A service should represent a meaningful application operation rather than merely wrapping one repository method.

Examples of valid service operations:

```csharp
SaveAsync(KnowledgeEntry entry)
DeleteAsync(int id)
GetAllAsync()
SearchAsync(string text)
ToggleFavoriteAsync(KnowledgeEntry entry)
```

---

## 4. Repository

The repository is responsible for SQLite persistence.

Example:

```text
IKnowledgeRepository
KnowledgeRepositorySQLite
```

Responsibilities include:

* opening/using the database
* SQL queries
* inserting records
* updating records
* deleting records
* loading related records
* transactions
* schema-specific handling

The repository should not contain UI logic.

---

## 5. Data Model

The current database contains three tables.

```text
KnowledgeEntry
---------------
Id PK
Title
Description
DateResolved
ProjectName
ResolutionSteps
Technologies
Tags
Comments
CodeSnippet
IsFavorite
YouTubeUrl

Step
----
Id PK
Description
Order
KnowledgeEntryId

AttachmentInfo
--------------
Id PK
KnowledgeEntryId
FileName
FilePath
DateAdded
```

The object model additionally contains:

```text
KnowledgeEntry.Steps
KnowledgeEntry.Attachments
```

These are `[Ignore]` properties and are not SQLite columns.

---

## 6. Relationships

Logical relationships:

```text
KnowledgeEntry 1 ──── * Step

KnowledgeEntry 1 ──── * AttachmentInfo
```

The relationship is represented by:

```text
Step.KnowledgeEntryId
AttachmentInfo.KnowledgeEntryId
```

There are currently no SQLite foreign-key constraints.

The repository/application layer must therefore enforce referential integrity.

---

## 7. Save Operation

Saving a KnowledgeEntry should represent saving the complete aggregate:

```text
KnowledgeEntry
    ├── Steps
    └── Attachments
```

Preferred conceptual operation:

```text
KnowledgeEntryService.SaveAsync(entry)
                ↓
KnowledgeRepositorySQLite.SaveAsync(entry)
                ↓
             Transaction
                ├── Save KnowledgeEntry
                ├── Synchronize Steps
                └── Synchronize Attachments
```

The transaction must either complete entirely or roll back.

The repository may use a delete-and-reinsert strategy for child collections where appropriate, provided that child IDs do not have external meaning.

If preserving child IDs is required, explicit synchronization should be used instead.

---

## 8. Delete Operation

Deleting a KnowledgeEntry must delete its related children.

Conceptually:

```text
Delete KnowledgeEntry
        ↓
BEGIN TRANSACTION
        ↓
DELETE Steps
        ↓
DELETE AttachmentInfo
        ↓
DELETE KnowledgeEntry
        ↓
COMMIT
```

If any operation fails:

```text
ROLLBACK
```

Do not rely on SQLite cascade deletion because foreign keys are not currently defined.

---

## 9. Step Persistence

The final state of `KnowledgeEntry.Steps` is authoritative when the entry is saved.

Example:

```text
Database:
1. Step A
2. Step B
3. Step C

User edits:
1. Step A
2. Step C
3. Step D

Save:
→ database must become exactly:
1. Step A
2. Step C
3. Step D
```

A deleted Step must not remain orphaned in SQLite.

`Order` should be normalized when saving.

---

## 10. Attachment Persistence

Attachments follow the same aggregate-consistency principle.

The database should contain exactly the persisted AttachmentInfo records associated with the KnowledgeEntry.

Physical files referenced by `FilePath` are separate from the database record.

Deleting an AttachmentInfo row must not automatically be assumed to mean deleting the physical file unless that behavior is explicitly required.

---

## 11. ResolutionSteps

`KnowledgeEntry` currently contains:

```text
ResolutionSteps
```

and there is also a structured:

```text
Step
```

table.

These represent potentially overlapping concepts.

Do not remove `ResolutionSteps` without checking existing code/data.

The structured `Step` entity should be treated as the authoritative representation for individually ordered resolution steps if that is the current application behavior.

Any future migration should explicitly address legacy `ResolutionSteps` data.

---

## 12. Navigation

Navigation is a WinForms concern.

A navigation service may manage:

* current view
* navigation history
* Back
* view creation through DI
* view disposal

It must not manage:

* SQLite
* repositories
* persistence
* business rules

---

## 13. Main Screen

`ctlMain` is responsible for presenting the KnowledgeEntry collection.

Typical flow:

```text
OnAppearing
    ↓
KnowledgeEntryService.GetAllAsync()
    ↓
local UI collection
    ↓
search/filter/sort
    ↓
render cards
```

The UI may keep presentation state such as:

* `_allEntries`
* search text
* favorite filter
* selected tags
* selected sort mode

This is acceptable without MVVM.

---

## 14. Add/Edit Screen

`ctlAddEdit` is responsible for:

* editing KnowledgeEntry fields
* Markdown preview
* YouTube presentation
* validation feedback
* invoking save/cancel
* opening Step management
* copying code snippets

The control should not directly access SQLite.

Flow:

```text
ctlAddEdit
    ↓
KnowledgeEntryService.SaveAsync(entry)
    ↓
KnowledgeRepositorySQLite
    ↓
SQLite
```

---

## 15. Manage Steps

`ctlManageSteps` is a UI editor for:

```text
KnowledgeEntry.Steps
```

It should manipulate the in-memory collection.

Persistence should happen through the KnowledgeEntry save operation rather than through independent UI-level database calls for every Step change.

---

## 16. Statistics

Statistics are application-level operations.

For the current dataset, statistics may be calculated in memory after loading entries.

Examples:

* total entries
* favorites
* entries by month
* top projects
* top tags

If the database becomes significantly larger, expensive aggregations may be moved to repository-level SQL queries.

Do not optimize prematurely.

---

## 17. Search

Search may be implemented at repository level when database querying is appropriate.

The current application can load entries and apply presentation filtering in memory.

Repository search should be used when:

* datasets become large
* performance becomes an issue
* SQL-side filtering is clearly beneficial

Do not create separate repositories or services for every search/filter operation.

---

## 18. Transactions

Transactions are required for operations that modify multiple related tables.

At minimum:

* saving an entry and its children
* deleting an entry and its children

A transaction must not span UI operations.

---

## 19. Schema Changes

Schema changes must consider existing databases.

Do not assume that changing a C# model automatically upgrades an existing SQLite database safely.

Any schema change should define:

1. how existing databases are detected
2. how they are migrated
3. how existing data is preserved
4. how failure is handled

---

## 20. Indexes

The current schema has primary-key indexes only.

Potential future indexes include:

```sql
CREATE INDEX IX_Step_KnowledgeEntryId
ON Step(KnowledgeEntryId);

CREATE INDEX IX_AttachmentInfo_KnowledgeEntryId
ON AttachmentInfo(KnowledgeEntryId);
```

Indexes should be introduced based on actual query/performance requirements.

Do not add indexes indiscriminately.

---

## 21. Architectural Rule

The central architectural rule is:

> Forms own presentation. Services own application operations. Repositories own persistence.

Do not violate this boundary merely to reduce a few lines of code.

At the same time, do not introduce abstractions that provide no practical value.
