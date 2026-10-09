# UI Guidelines — KnowledgeTrackerApp

# UI Design Specification

## Product Character

KnowledgeTracker is a personal technical knowledge-management tool.

The UI must feel like a polished developer productivity application.

It must NOT resemble:
- an enterprise CRUD application;
- a generic database editor;
- a data-entry form;
- a legacy WinForms utility.

The visual design should prioritize:
- information density;
- clarity;
- hierarchy;
- speed;
- discoverability;
- efficient use of screen space.

## 1. General Principles

KnowledgeTrackerApp is a desktop application intended for fast technical note capture and retrieval.

The UI should be:

* professional
* fast
* compact
* readable
* desktop-native
* consistent
* keyboard/mouse friendly

Avoid unnecessary navigation and dialogs.

---

## 2. Technology

The UI uses:

* Syncfusion Windows Forms
* Syncfusion WinForms controls
* Markdown rendering/preview where applicable

Use Syncfusion controls.

Do not replace Syncfusion components with custom implementations without a reason.

---

## 3. Main Shell

`MainForm` acts as the application shell.

It is responsible for:

* hosting the current content view
* navigation
* Back
* current page title
* navigation history
* application-level visual state

The shell should not contain database logic.

---

## 4. Main List

`EntryCard` presents KnowledgeEntry records as cards.

Cards should expose important information at a glance, such as:

* title
* project
* technologies/tags
* resolved date
* favorite state
* relevant metadata

The card should remain visually clean with a polished and professional appearance.
Use syncfusion controls to design the card.
Do not display every field.

---

## 5. Card Interaction

Current interaction model:

* left click on the main card area → open entry
* click in the favorite area → toggle favorite
* right click → context menu

The interaction zones should remain visually understandable.

Avoid accidental activation when interacting with the favorite control.

---

## 6. Search

Search should be easy to discover.

The search experience should:

* respond quickly
* preserve the current UI context
* combine naturally with filters
* clearly show when no entries match

Search should not unexpectedly modify persisted data.

---

## 7. Tags

Tags may be displayed as selectable chips/filters.

Tag filtering should:

* be visually obvious
* support multiple selected tags where intended
* combine with search
* combine with favorite filtering
* not modify stored tags

---

## 8. Sorting

Sorting is exposed through a compact selector.

Sort options should have clear labels.

Changing sort order should not modify the database.

---

## 9. Favorites

Favorite state should have clear visual feedback.

Toggling a favorite should:

1. update the visual state
2. persist the change
3. avoid unnecessary navigation

Failure to persist should be communicated to the user.

---

## 10. Add/Edit

The 'AddEdit' view should clearly separate:

* core information
* description
* technologies/tags
* code
* YouTube
* steps
* attachments
* comments

The form should not feel overloaded.

Use sensible grouping and spacing.

---

## 11. Markdown

Markdown editing and preview should make it easy to understand the final rendered result.

The source Markdown must remain editable.

Preview should not silently modify source content.

---

## 12. YouTube

When a valid YouTube URL is detected:

* show a thumbnail when possible
* provide a clear action to open the video
* avoid blocking the rest of the form if the thumbnail cannot be loaded

Network failure should not make the entry unusable.

---

## 13. Code Snippet

The code editor should use a readable monospace presentation where appropriate.

Provide an easy Copy action.

Copying should provide normal desktop feedback where useful.

---

## 14. Steps

The 'Manage Steps' UI should allow:

* adding
* editing
* deleting
* reordering

The list should make the order obvious.

The UI edits the in-memory KnowledgeEntry.Steps collection.

Persistence occurs through the normal KnowledgeEntry save operation.

If a Step is removed, it must not remain persisted after save.

---

## 15. Attachments

Attachment UI should clearly show:

* file name
* relevant path/status
* date where useful
* available actions

Do not expose raw internal database IDs unless required.

File-system failures should be handled gracefully.

---

## 16. Validation

Validation should occur as close as possible to the user action.

Examples:

* required title
* malformed YouTube URL
* invalid attachment path
* invalid data

Validation messages should explain how to correct the problem.

---

## 17. Save / Cancel

Save should:

* validate
* persist the complete entry
* include Steps and Attachments
* report failures
* return to the appropriate view on success

Cancel should:

* avoid unintended persistence
* return to the previous view

---

## 18. Delete

Deleting an entry is destructive.

Always request confirmation.

The confirmation should identify what will be deleted.

The database operation must delete the entry and its related Step/AttachmentInfo records atomically.

---

## 19. Statistics

Statistics should provide useful high-level information without overwhelming the user.

Potential metrics:

* total entries
* favorites
* monthly activity
* projects
* tags

Charts should remain readable with realistic personal datasets. Use syncfusion charting controls.

---

## 20. Loading States

Long-running operations should provide appropriate visual feedback.

Avoid freezing the UI during:

* database operations
* file operations
* network operations
* expensive calculations

Prefer asynchronous operations.

---

## 21. Error Feedback

Errors should be:

* understandable
* actionable where possible
* non-technical for normal users

Do not display raw exceptions or stack traces as the primary user message.

---

## 22. Theme

The UI should respect the application's existing theme.

Do not introduce hard-coded colors that conflict with the selected theme.

Reuse existing theme resources and Syncfusion styling.

---

## 23. Consistency

Use consistent:

* margins
* spacing
* button sizes
* typography
* icons
* control behavior
* terminology

Existing UI conventions should be preserved unless there is a clear reason to change them.

---

## 24. Desktop Interaction

Support normal desktop behavior:

* mouse
* right-click
* keyboard navigation where practical
* keyboard shortcuts where already established

Do not design the application as if it were a mobile UI.

---

## 25. Principle

The UI should optimize for:

> Find a previous solution quickly, or record a new solution with minimal friction.
