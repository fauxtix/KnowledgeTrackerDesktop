## 1. Product Overview

KnowledgeTrackerDesktop is a personal Windows desktop application for recording technical problems, troubleshooting processes and solutions that have already been discovered.

The goal is to create a searchable personal knowledge base that makes previously solved technical problems easy to find and reuse.

The application is intended primarily for personal use and offline operation.

---

## 2. Target User

The primary user is a technically experienced person who regularly:

* develops software
* troubleshoots technical problems
* works with multiple projects and technologies
* encounters problems that may recur later
* wants to document solutions for future reference

The application should optimize for fast capture and fast retrieval.

---

## 3. KnowledgeEntry

The central entity is `KnowledgeEntry`.

A KnowledgeEntry contains:

* Title
* Description
* DateResolved
* ProjectName
* ResolutionSteps
* Technologies
* Tags
* Comments
* CodeSnippet
* IsFavorite
* YouTubeUrl
* ProjectOriginKind
* ProjectOriginLocation

It also has related collections:

* Steps
* Attachments

The SQLite representation stores the main fields in `KnowledgeEntry`.

`Steps` and `Attachments` are related entities and are not columns of the KnowledgeEntry table.

---

## 3.1 Project Origin

`ProjectName` names the project an entry belongs to. It says nothing about
where the knowledge itself came from, so every entry can also record a **typed
origin**:

* `ProjectOriginKind` — `LocalFolder`, `GitRepository` or `WebPage`. It is
  empty when the origin was not recorded.
* `ProjectOriginLocation` — an absolute folder path for a local folder, an
  http(s) URL for a repository or a web page.

The two go together: a kind without a location points nowhere, and a location
without a kind cannot be interpreted.

The origin is a record, not a guarantee. The folder may have been moved and the
site may be gone, so the application reports a missing target when the user
opens it instead of pretending the origin is still there.

The origin is per entry, not per project: the "found this on a web page" case
belongs to the entry, not to the application it documents.

---

## 4. Resolution Steps

Structured troubleshooting steps are represented by the `Step` entity.

Each Step contains:

* Id
* Description
* Order
* KnowledgeEntryId

Steps belong to a KnowledgeEntry.

The UI should allow the user to:

* add steps
* edit steps
* remove steps
* reorder steps
* save the resulting collection

The persisted Step collection must reflect the final state shown by the user.

Removing a Step in the UI must remove it from the database when the entry is saved.

---

## 5. Attachments

Attachments are represented by `AttachmentInfo`.

Each attachment contains:

* Id
* KnowledgeEntryId
* FileName
* FilePath
* DateAdded

Attachments belong to a KnowledgeEntry.

The application should preserve the relationship between an attachment and its KnowledgeEntry.

When an entry is deleted, its AttachmentInfo records must also be deleted.

Deleting a database record does not necessarily imply deleting the physical file referenced by `FilePath`; file-system deletion must be treated as a separate explicit decision.

---

## 6. Markdown

The Description field supports Markdown.

The application should provide a convenient way to:

* edit Markdown
* preview rendered Markdown
* preserve the original Markdown content

Markdown rendering is a presentation concern and must not alter the persisted source text unexpectedly.

---

## 7. Code Snippets

KnowledgeEntry may contain a code snippet.

The UI should make code snippets easy to:

* edit
* read
* copy

Copying a snippet must not modify the stored KnowledgeEntry.

---

## 8. YouTube

KnowledgeEntry may contain a YouTube URL.

The UI should:

* accept a YouTube URL
* identify the video when possible
* display a thumbnail when possible
* allow the user to open the video

Invalid or unsupported URLs should fail gracefully.

---

## 9. Favorites

A KnowledgeEntry can be marked as favorite using `IsFavorite`.

The user can:

* mark an entry as favorite
* remove it from favorites
* filter the main list to favorites

Favorite changes should be persisted immediately or as part of the application's normal save behavior, according to the existing UI behavior.

---

## 10. Search

Users should be able to search the knowledge base.

Search should cover relevant KnowledgeEntry fields, including:

* Title
* Description
* ProjectName
* ProjectOriginLocation
* Technologies
* Tags
* Comments
* CodeSnippet

Search should be case-insensitive from the user's perspective.

The UI should provide immediate and understandable feedback when there are no results.

---

## 11. Filtering

The main screen supports:

* favorite filtering
* tag filtering
* project filtering
* origin filtering (the kind of source the knowledge came from, or the entries
  whose origin was never recorded)

Filtering should be combinable with search and sorting.

Filtering should not modify persisted data.

---

## 12. Sorting

Users should be able to sort the main list.

The available sorting options should remain centralized and easy to extend.

Sorting is a presentation concern unless database-side sorting becomes necessary for performance.

---

## 13. CRUD

The application supports:

### Create

Create a new KnowledgeEntry, edit its fields, add Steps/Attachments and save it.

### Read

Load KnowledgeEntry records and their related Steps and Attachments.

### Update

Modify an existing KnowledgeEntry and persist the complete updated state.

### Delete

Delete a KnowledgeEntry and its related Step and AttachmentInfo records.

Deletion should be atomic.

---

## 14. Statistics

The application provides statistics such as:

* total entries
* favorite entries
* entries over time
* entries by project
* entries by tag
* entries by origin kind

Statistics should be useful for understanding the knowledge base without becoming a primary feature.

---

## 15. Navigation

The application uses a shell-based navigation model.

The main shell hosts different views/controls.

Users should be able to:

* navigate from the main list to Add/Edit
* navigate from Add/Edit to Manage Steps
* return to previous views
* return to the main list after save/cancel

Navigation should not depend on database implementation details.

---

## 16. Persistence

The application uses a local SQLite database:

```text
<solution root>\knowledge.db3 (development)
%LOCALAPPDATA%\KnowledgeTrackerApp\knowledge.db3 (deployed / MSIX)
```

The deployed location is per-user and writable, which is required for
MSIX-packaged builds whose install directory is read-only. `KnowledgeTrackerApp`
is a storage folder name retained for compatibility with existing user data; it
is not the current product name. In development, an existing database from this
folder is copied into the solution root on first startup with SQLite's backup
API and the original is kept.

The application is primarily offline.

Database operations must preserve consistency between:

```text
KnowledgeEntry
    ├── Steps
    └── Attachments
```

Because SQLite currently does not enforce foreign keys, the application/repository layer is responsible for maintaining these relationships.

---

## 17. UX Goals

The application should feel:

* fast
* simple
* practical
* desktop-native
* keyboard/mouse friendly
* visually organized

Avoid unnecessary dialogs and navigation steps.

The user should be able to capture a solved problem quickly.

---

## 18. Non-Goals

The following are not current goals:

* cloud synchronization
* multi-user collaboration
* web application
* mobile application
* authentication
* complex role/permission systems
* enterprise-scale architecture
* CQRS
* event sourcing
* unnecessary abstraction layers

The architecture should not be designed around these hypothetical future requirements.
