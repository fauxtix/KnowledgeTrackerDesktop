# UI Design Specification

## 1. Purpose

This document defines the concrete visual and interaction design for the KnowledgeTracker desktop application.

It complements:

- PRD.md
- ARCHITECTURE.md
- UI_GUIDELINES.md
- CODING_STANDARDS.md
- TASKS.md

The current UI implementation is considered a functional prototype only.

It must NOT be treated as the desired final visual design.

The goal is to create a polished desktop developer productivity application for maintaining a personal technical knowledge base.

The application should feel closer to a developer productivity tool than to a generic enterprise CRUD application.

---

# 2. Design Principles

## 2.1 Content first

The knowledge itself is the primary content.

The UI must give the most visual space to:

- Title
- Description
- Resolution
- Steps
- Code

Secondary information such as:

- Date
- Project
- Technologies
- Tags
- Favorite
- YouTube
- Attachments
- Comments

must not visually dominate the main knowledge content.

## 2.2 High information density

Avoid large empty areas.

Desktop screen space should be used efficiently.

Controls should be compact and grouped by purpose.

Do not create large vertical stacks of independent GroupBoxes.

## 2.3 Clear hierarchy

Every screen must have:

1. clear page identity
2. primary content
3. primary action
4. secondary actions

The user should understand what the screen is for within a few seconds.

## 2.4 Developer-oriented visual language

The application should feel appropriate for a developer.

Prefer:

- compact metadata
- code-oriented typography where appropriate
- chips/tags
- ordered step indicators
- clear technical labels
- contextual actions
- keyboard-friendly interactions

Avoid:

- generic business-form layouts
- excessive borders
- excessive GroupBoxes
- decorative UI without purpose
- oversized controls

---

# 3. Global Layout

## 3.1 Main window

The application uses a desktop shell with:

- navigation area
- page header
- content area

Conceptually:

┌──────────────────────────────────────────────────────────┐
│ KnowledgeTracker                              window    │
├───────────────┬──────────────────────────────────────────┤
│               │ ← Page title                     Action │
│ Navigation    ├──────────────────────────────────────────┤
│               │                                          │
│ Library       │                                          │
│ Statistics    │              Page content                │
│               │                                          │
│ + New Entry   │                                          │
│               │                                          │
└───────────────┴──────────────────────────────────────────┘

The shell must remain visually quiet.

The content page is the focus.

## 3.2 Navigation

Primary destinations:

- Library
- Statistics
- New Entry

Navigation must not be duplicated unnecessarily inside every page.

Back navigation is contextual and should primarily be used when the user entered a detail/editor workflow.

---

# 4. Library Screen

## 4.1 Purpose

The Library is the primary screen of the application.

It must allow the user to:

- find knowledge
- scan knowledge quickly
- identify favorites
- inspect an entry
- open an entry for editing
- create a new entry

The Library must be the most polished and information-dense screen.

## 4.2 Layout

Use a two-pane layout.

Conceptually:

┌──────────────────────────────────────────────────────────────┐
│ Library                                      + New Entry     │
│                                                              │
│ [ Search............................................... ]    │
│ [Favorites] [Tags] [Project] [Sort ▼]                       │
├──────────────────────────────┬───────────────────────────────┤
│ Knowledge entries            │ Selected entry                │
│                              │                               │
│ ┌──────────────────────────┐ │ Title                         │
│ │ Fix database connection  │ │ Project · Technologies         │
│ │ MyProject                │ │ Tags                           │
│ │ .NET · SQLite            │ │                               │
│ │ #database #sqlite    ★   │ │ Description                    │
│ │ 4 steps · 2 files       │ │                               │
│ └──────────────────────────┘ │                               │
│                              │ Resolution                     │
│ ┌──────────────────────────┐ │                               │
│ │ Configure API timeout    │ │ 4 steps · 2 attachments        │
│ │ ...                      │ │                               │
│ └──────────────────────────┘ │                               │
│                              │                               │
└──────────────────────────────┴───────────────────────────────┘

The list pane should normally occupy approximately 40% of the content width.

The preview pane should occupy approximately 60%.

The exact ratio may adapt to the minimum supported window size.

## 4.3 Entry cards

Do not use a single DisplayText string for the entire row.

Each entry must have visually distinct elements:

- title
- project
- technologies
- tags
- date
- favorite
- step count
- attachment count
- short description/excerpt

Example:

┌────────────────────────────────────────────┐
│ Fix SQLite migration failure            ★  │
│ KnowledgeTracker · .NET 10 · SQLite       │
│ #sqlite  #migration  #database             │
│ Fixes startup failure after schema change   │
│ 4 steps · 2 attachments · 08 Oct 2026      │
└────────────────────────────────────────────┘

The title is the strongest element.

Metadata is visually secondary.

Tags should be compact chips.

Counts should be compact metadata rather than large controls.

## 4.4 Selection

Single click selects an entry.

The selected entry must have a clearly visible selection state.

Selection must update the preview pane.

Selection must not require double-click.

## 4.5 Opening an entry

Double-clicking an entry opens Add/Edit.

The context menu must provide actions such as:

- Open/Edit
- Toggle favorite
- Delete

The context menu should operate on the entry under the pointer.

## 4.6 Favorite

Favorite state must be visible directly in the card.

The user must be able to toggle favorite without opening the editor.

The favorite action must have a clear hover/pressed/selected state.

## 4.7 Preview

The preview pane is for quick inspection.

It should show:

- title
- project
- technologies
- tags
- date
- favorite
- description
- resolution/steps summary
- code availability
- attachment availability
- YouTube availability

The preview must not become a second full editor.

Primary action:

- Edit

Secondary actions:

- Toggle favorite
- Delete

## 4.8 Search and filters

Search must remain prominent.

Search, favorites, tag filtering and sorting must be combinable.

The active filters should be visually obvious.

Provide a clear way to remove filters.

## 4.9 Empty library state

When no entries exist, show a useful empty state.

Example:

No knowledge entries yet

Start documenting the technical problems you solve.

[ + Create your first entry ]

Do not display an empty ListView without explanation.

## 4.10 No search results

Example:

No entries match your search.

Try a different search term or clear the filters.

[ Clear filters ]

## 4.11 Loading

Show a compact loading state in the content area.

Do not block the entire application with an unnecessary modal dialog.

## 4.12 Error

Show a human-readable error.

Do not expose raw exception messages as the primary user-facing message.

Example:

Unable to load the knowledge library.

[ Retry ]

Technical details may be available separately if appropriate.

---

# 5. Add/Edit Entry

## 5.1 Purpose

Add/Edit is an editor, not a generic data-entry form.

The user should feel that they are writing a technical knowledge article.

## 5.2 Overall layout

Use a single primary content flow rather than two independently scrolling columns.


## 5.3 Header

The page header must contain:

- Back
- page title
- Cancel
- Save

Save must be visually identifiable as the primary action.

The user should not have to scroll to find Save.

## 5.4 Title

Title is the most visually prominent editable field.

It should not be hidden inside a GroupBox.

## 5.5 Metadata

Project, Technologies and Date should be arranged horizontally when window size allows.

Tags and Favorite should appear in a compact metadata row.

## 5.6 Description

Description is one of the primary content areas.

Markdown source and preview should be clearly accessible.

The editor must preserve Markdown source text.

Do not trim meaningful whitespace from Markdown.

## 5.7 Resolution

Resolution is secondary to the description but still important.

It should not occupy a huge amount of vertical space when empty.

If Steps are present, show their count and provide direct access to Manage Steps.

## 5.8 Steps

Show a compact summary of existing steps.

Example:

Resolution steps                              4 steps

1  Configure the database connection
2  Restart the application
3  Clear the cache
4  Verify the result

[ Manage steps ]

The full step editor belongs in Manage Steps.

## 5.9 Code

Code must have a developer-oriented presentation.

Provide:

- readable code area
- Copy action

Do not use a tiny single-line textbox for code.

Code indentation and whitespace must be preserved.

## 5.10 YouTube

When a valid YouTube URL exists:

- show thumbnail
- show URL or compact video information
- provide Open video

When no video exists, keep the section compact.

Do not reserve a huge empty area for YouTube.

## 5.11 Attachments

Attachments should be compact.

Show:

- file name
- useful path/status information
- date added

Actions should be contextual where possible:

- Open
- Remove

Do not hide important attachment information exclusively in tooltips.

## 5.12 Comments

Comments are secondary information.

They should appear after the main technical content.

Do not allow the comments section to dominate the page.

## 5.13 Save behavior

Save:

- validates required data
- preserves Markdown/code whitespace
- persists the complete aggregate
- persists Steps and Attachments transactionally
- returns to the appropriate previous context

## 5.14 Cancel/discard

If there are unsaved changes, ask for confirmation.

If the user confirms discard:

- changes must actually be abandoned
- the discarded editor instance must not remain restorable through Back navigation

---

# 6. Manage Steps

## 6.1 Purpose

Manage Steps is a focused editor for an ordered sequence.

It should not look like a generic toolbar plus an empty ListView.

## 6.2 Layout

Preferred layout:

┌──────────────────────────────────────────────────────────────┐
│ ← Manage Steps                                  Apply steps │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│ Resolution steps                                  4 steps    │
│                                                              │
│ 01  Configure the database connection                       │
│     Verify the connection string.                            │
│                                                              │
│ 02  Restart the application                                  │
│     Close and reopen KnowledgeTracker.                       │
│                                                              │
│ 03  Clear the local cache                                    │
│     Remove stale cache files.                                │
│                                                              │
│ 04  Verify the result                                        │
│     Run the application and test the operation.              │
│                                                              │
│ + Add step                                                    │
│                                                              │
└──────────────────────────────────────────────────────────────┘

## 6.3 Step representation

Every step must visibly show:

- order number
- description

The order number must be visually distinct.

## 6.4 Selection

Selecting a step must provide a clear selection state.

Contextual actions become available for the selected step:

- Edit
- Delete
- Move up
- Move down

## 6.5 Add

Add Step should be visually obvious but secondary to the step list.

## 6.6 Reordering

At minimum support:

- Move up
- Move down
- keyboard equivalents where practical

Drag-and-drop reordering is desirable but not mandatory for the first implementation.

## 6.7 Empty state

Example:

No resolution steps yet.

Break the solution into clear, reproducible steps.

[ + Add step ]

Do not show a large empty white ListView.

## 6.8 Cancel

Cancel discards staged changes made in Manage Steps.

The parent KnowledgeEntry must remain unchanged.

## 6.9 Apply

Apply transfers the staged step collection to the parent entry.

Persistence happens when the parent entry is saved.

---

# 7. Statistics

## 7.1 Purpose

Statistics should help the user understand and explore their knowledge base.

It should not merely display decorative charts.

## 7.2 Layout

Top:

- Total entries
- Favorites
- Projects
- Tags

Below:

- entries over time
- top projects
- top tags

Projects and tags should be actionable where practical.

For example:

Clicking a project should navigate to Library with that project filter active.

Clicking a tag should navigate to Library with that tag filter active.

## 7.3 Empty state

If there is no data:

Your knowledge statistics will appear here once you create some entries.

[ Create entry ]

[ Open Library ]

## 7.4 Loading

Use a compact loading state.

## 7.5 Error

Show understandable feedback with Retry.

Do not expose raw exception text.

---

# 8. Navigation

## 8.1 History

Navigation history must represent meaningful user navigation.

Do not continuously create duplicate instances of Library or Statistics merely because the user navigated between them.

## 8.2 Back

Back should return to the logical previous context.

Examples:

Library → Edit → Back → Library

Library → Edit → Manage Steps → Apply → Edit

Statistics → Library filtered by project → Back → Statistics

## 8.3 Context restoration

Returning to Library should preserve where appropriate:

- search text
- filters
- sort
- selected entry

Selection restoration must be based on stable identity such as entry ID, not object reference.

---

# 9. Keyboard Interaction

The application should be usable with keyboard and mouse.

Recommended shortcuts:

- Ctrl+N — New Entry
- Ctrl+S — Save
- Escape — Cancel/Back where appropriate
- Delete — Delete selected entry/step after confirmation
- Enter — Open selected library entry
- Ctrl+F — Focus search

Manage Steps:

- Up/Down — navigate
- Ctrl+Up — move step up
- Ctrl+Down — move step down
- Delete — remove selected step

Exact shortcuts may be adapted to WinForms/Syncfusion behavior.

---

# 10. Theme

All controls must respect the application's theme.

Avoid isolated hard-coded colors.

Do not use arbitrary values such as:

Color.Firebrick

for primary application states unless the color is explicitly part of the theme.

Use theme-aware colors for:

- errors
- warnings
- selection
- accent
- success
- secondary text

Standard WinForms controls may be used when appropriate, but their visual treatment must remain consistent with the Syncfusion-based application.

---

# 11. Error Presentation

User-facing errors must be understandable.

Bad:

SQLiteException: SQLite Error 19: FOREIGN KEY constraint failed...

Better:

The knowledge entry could not be saved.

Please try again.

[ Retry ]

Technical exception details may be logged or exposed through a secondary diagnostic mechanism.

---

# 12. Responsive Behavior

The application is a desktop application and does not require mobile responsiveness.

However, the UI must remain usable at the defined minimum window size.

When space becomes constrained:

- reduce secondary metadata
- allow content areas to scroll
- preserve primary actions
- avoid overlapping controls
- avoid extremely narrow editor fields

The primary content must remain accessible.

---

# 13. Visual Density Rules

Prefer:

- compact metadata
- 8–16 px spacing increments
- consistent margins
- compact toolbars
- content-driven section heights

Avoid:

- giant empty containers
- unnecessary vertical padding
- repeated GroupBoxes
- oversized buttons
- multiple independent scrollbars when one content scrollbar is sufficient

Whitespace should separate conceptual areas, not consume the majority of the screen.

---

# 14. What NOT To Do

The following patterns are explicitly rejected.

## Do not create generic CRUD forms

Avoid:

┌───────────────┐
│ GroupBox      │
│ Label TextBox │
│ Label TextBox │
└───────────────┘

repeated many times down the screen.

## Do not use giant empty ListViews

An empty state must communicate what the user can do.

## Do not hide important information in tooltips

Tooltips may supplement information but must not be the only way to discover important state.

## Do not use DisplayText for complex entry cards

Use separate visual elements for:

- title
- metadata
- tags
- counts
- favorite
- excerpt

## Do not put important actions far away from their content

Actions should be close to the thing they affect.

## Do not create unnecessary duplicate navigation

The user should not see Back buttons everywhere without a clear navigation purpose.

## Do not show raw exception messages

User-facing errors must be understandable.

## Do not introduce MVVM solely for UI redesign

The existing architecture remains:

WinForms
→ Application Services
→ Repositories
→ SQLite

The UI redesign must not introduce MVVM, ViewModels, MediatR, CQRS or other architectural complexity without an explicit architectural decision.
Always use syncfusion controls.
---

# 15. Implementation Priorities

UI implementation should happen in this order:

1. Main shell/navigation
2. Library
3. Add/Edit
4. Manage Steps
5. Statistics
6. Empty/loading/error states
7. Keyboard interactions
8. Visual consistency and polish

After each major screen:

- build the application
- run tests
- visually inspect the screen
- verify behavior against this document

Do not redesign persistence or application services as part of UI work.

---

# 16. Acceptance Criteria

The redesign is considered successful when:

- The application no longer looks like a generic CRUD application.
- The Library clearly communicates title, metadata, tags and entry state.
- The Library supports direct selection and contextual actions.
- The Add/Edit screen prioritizes technical content.
- Description and code preserve meaningful whitespace.
- Manage Steps presents an ordered sequence clearly.
- Empty states provide useful next actions.
- Errors are understandable.
- Navigation does not resurrect discarded edits.
- Library context can be restored reliably.
- The UI uses available desktop space efficiently.
- Theme colors are consistent.
- The application remains responsive at its minimum supported size.
- Existing architecture and persistence boundaries remain intact.

---

# 17. Non-Goals

This redesign does NOT include:

- changing the SQLite schema unless separately required
- replacing sqlite-net
- introducing MVVM
- introducing a generic repository abstraction
- introducing CQRS/MediatR
- rewriting application services
- replacing Syncfusion
- changing the domain model unnecessarily
- rewriting working persistence code

The goal is to improve the user experience while preserving the existing sound architecture.