# Coding Standards — KnowledgeTrackerDesktop

## 1. General

Use modern C# appropriate for .NET 10.

Prefer code that is:

* readable
* explicit
* maintainable
* testable
* unsurprising

Avoid clever abstractions.

---

## 2. Naming

Use standard C# naming conventions.

```text
PascalCase
```

for:

* classes
* methods
* properties
* public fields

```text
camelCase
```

for:

* parameters
* local variables

Private fields should follow the existing project convention, such as:

```csharp
private readonly IService _service;
```

Preserve established naming conventions when modifying existing code.

---

## 3. Classes

Classes should have a clear responsibility.

Prefer:

```text
KnowledgeEntryService
StatisticsService
KnowledgeRepositorySQLite
NavigationServiceWinForms
```

over many tiny classes such as:

```text
SaveEntryService
DeleteEntryService
UpdateFavoriteService
```

unless there is a concrete reason for separation.

---

## 4. WinForms Code

Forms and controls may contain:

* event handlers
* UI state
* presentation transformations
* filtering
* sorting
* control configuration
* validation presentation
* navigation calls

They should not contain:

* SQL
* SQLite transactions
* database connections
* persistence algorithms
* multi-entity application rules

---

## 5. Services

Services coordinate application operations.

A service should not become a generic utility bag.

Prefer cohesive services such as:

```csharp
KnowledgeEntryService
StatisticsService
```

A service may coordinate multiple repository operations when required to maintain consistency.

---

## 6. Repositories

Repositories contain database-specific code.

Repository responsibilities include:

* SQL
* SQLite access
* mapping
* transactions
* loading related entities
* insert/update/delete

Do not expose SQLite implementation details to the WinForms layer.

---

## 7. Transactions

Use transactions when an operation modifies multiple related records.

For example:

```text
Save KnowledgeEntry
    + Steps
    + Attachments
```

must be atomic.

Delete operations involving parent and children must also be atomic.

---

## 8. Entity Aggregates

Treat:

```text
KnowledgeEntry
    ├── Steps
    └── Attachments
```

as one logical unit when saving/deleting.

The application should avoid leaving partial child state in the database.

---

## 9. Step Synchronization

When saving Steps, the persisted collection must match the current collection.

This is incorrect:

```csharp
foreach (var step in entry.Steps)
{
    await AddOrUpdateStepAsync(step);
}
```

if deleted steps are never removed.

The persistence algorithm must also account for records that existed previously but no longer exist in the current collection.

For small collections, deleting existing child rows and recreating the current collection inside a transaction is an acceptable strategy when child IDs do not need to be preserved.

---

## 10. Delete Operations

Deleting a KnowledgeEntry must also remove:

```text
Step
AttachmentInfo
```

records belonging to it.

Do not rely on database cascade behavior because foreign keys are not currently defined.

---

## 11. Ordering Steps

`Step.Order` should represent the user-visible order.

Before persistence, normalize ordering when necessary:

```text
1
2
3
...
```

Avoid gaps unless there is a concrete reason to preserve them.

---

## 12. Async

Use asynchronous APIs for I/O.

Prefer:

```csharp
public async Task SaveAsync(...)
```

Avoid:

```csharp
.Result
.Wait()
```

Avoid `async void` except for event handlers.

---

## 13. Exceptions

Do not use:

```csharp
catch
{
}
```

or otherwise silently ignore failures.

Catch exceptions where they can be meaningfully handled.

Repositories may allow exceptions to propagate to the application layer.

The UI should present an appropriate user-facing message.

---

## 14. Nullability

Respect nullable reference type analysis.

Do not suppress warnings using `!` without understanding why the value is guaranteed to be non-null.

Prefer explicit validation where appropriate.

---

## 15. Collections

Use appropriate collection types.

For UI-bound or observable state, use the collection type required by the existing UI.

For simple service/repository operations, prefer interfaces such as:

```csharp
IReadOnlyList<T>
IEnumerable<T>
ICollection<T>
```

when appropriate.

---

## 16. SQLite

SQLite-specific code belongs in the repository layer.

Do not spread SQLite calls throughout the application.

Use parameterized queries or sqlite-net APIs rather than concatenating user input into SQL.

---

## 17. Data Integrity

Because the current SQLite schema has no foreign-key constraints:

* repositories must explicitly maintain relationships
* deletes must clean up child records
* multi-table writes must use transactions
* tests should cover orphan prevention

Data integrity has higher priority than convenience.

---

## 18. Schema Changes

Do not modify the database schema casually.

Before changing a model/table:

1. inspect the existing schema
2. consider existing database files
3. consider existing data
4. determine migration requirements
5. test both new and existing databases

Changing a C# model is not automatically a database migration.

---

## 19. Indexes

Do not add indexes automatically.

Add them when:

* a query is frequently executed
* data volume justifies them
* measurements indicate a performance problem

Potential relationship indexes include:

```text
Step.KnowledgeEntryId
AttachmentInfo.KnowledgeEntryId
```

---

## 20. UI Event Handlers

Event handlers should remain reasonably small.

For example:

```csharp
private async void btnSave_Click(...)
{
    await SaveAsync();
}
```

Move substantial application operations into services.

---

## 21. Dependency Injection

Use constructor injection.

Avoid service location throughout the application.

Do not instantiate infrastructure services directly inside controls when DI can provide them.

---

## 22. IDisposable

Dispose resources that implement `IDisposable`.

Be particularly careful with:

* database connections
* streams
* images
* controls
* disposable dialogs

Do not dispose objects that are owned by another component.

---

## 23. Logging

Use the application's existing logging approach if one exists.

Do not introduce a logging framework solely for a small feature unless there is a concrete requirement.

---

## 24. Comments

Prefer self-explanatory code.

Use comments to explain:

* why something is unusual
* compatibility behavior
* database migration behavior
* non-obvious constraints

Do not comment obvious code.

---

## 25. Testing

Prioritize tests for:

* KnowledgeEntry persistence
* Step synchronization
* Attachment synchronization
* deletion
* transactions
* search
* statistics
* important application rules

Especially test the cases that can create orphaned database records.

Example:

```text
Given:
Entry has Steps A, B, C

When:
User removes B and saves

Then:
Database contains A, C
Database does not contain B
```

---

## 26. Refactoring

Refactor when it improves a concrete problem.

Do not refactor the entire project simply to make it conform to a theoretical architecture.

Preserve behavior unless behavior change is explicitly required.

---

## 27. Simplicity Rule

When two solutions are functionally equivalent, prefer the one with:

* fewer abstractions
* fewer classes
* fewer dependencies
* clearer ownership
* easier debugging

The architecture should serve the application, not the other way around.
