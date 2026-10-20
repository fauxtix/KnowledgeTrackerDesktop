# Help - Knowledge Tracker

## 1. Introduction

**Knowledge Tracker** is an application for recording, organizing, and accessing your knowledge. It lets you save entries with descriptions, notes, tags, categories, resolution steps, code snippets, attachments, and links so you can quickly find what you need.

## 2. Getting Started

1. **Launch the application** - Start Knowledge Tracker. The main window shows the navigation bar on the left and the content area on the right.
2. **Explore the Library** - The **Library** view shows your entries as cards. It's the starting point for viewing, creating, editing, and managing your knowledge.
3. **View Statistics** - The **Statistics** view shows aggregated information about your entries.
4. **Change the language** - You can switch between Portuguese and English using the PT/EN buttons at the bottom of the navigation bar.

## 3. Main Features

### 3.1 Library

In the **Library** view you have:

#### Header
- **+ New Entry** - Create a new entry in the editor.
- **Import** - Import a library from a JSON file exported by Knowledge Tracker.
- **Export** - Export the entire current library to a JSON file.

#### Filters and search
- **Search** - Filter entries by the text you type.
- **Favorites** - Show only entries marked as favorites.
- **Tags** - Filter by one or more tags.
- **Project** - Filter entries by project.
- **Sort** - Sort by newest/oldest, title, or project.

#### Card list
Each entry is displayed as a card. From the card or its action menu you can (depending on context):

- **Open/select** - Click the card to view it in the preview panel on the right.
- **Add/Remove from favorites** - Mark or unmark the entry as favorite.
- **Duplicate entry** - Create a copy of the selected entry to quickly build a new one based on it.
- **Delete entry** - Remove the entry from the library (this action asks for confirmation).

### 3.2 Entry Preview

When you select a card, the right-hand preview panel shows a read-only view of the entry with the following actions (when applicable):

- **Edit** - Open the entry in the editor to make changes.
- **View code** - Open a preview of the code snippet associated with the entry.
- **View attachments** - Open the gallery of attachments/images associated with the entry.
- **Open link** - If a YouTube link is present, open it in your browser.

### 3.3 Entry Editor (Create/Edit)

When creating or editing an entry, you can fill in several fields:

- **Title** - A clear identifier for the entry.
- **Project** - Associate the entry with a specific project.
- **Description / Notes** - Describe the problem, context, or observations.
- **Tags** - Add one or more tags to make cross-search easier.
- **Favorite** - Mark the entry as favorite for quick access.
- **Resolution steps** - Manage individual steps through the steps manager (`Manage steps`), with descriptions and ordering.
- **Code snippet** - Add a formatted code snippet for future reference.
- **Attachments / Images** - Add files/images to the entry. They can be viewed in the integrated gallery.
- **YouTube link** - Associate a reference video if applicable.
- **Comments** - Additional notes space.

### 3.4 Import and Export

- **Export** - Generate a `.json` file with your entire library. Useful for backups or migrating between machines.
- **Import** - Load a Knowledge Tracker export `.json` file. If entries with the same content already exist, the app will ask whether to keep existing entries or import them as copies.

### 3.5 Statistics

The **Statistics** view gives you an overview of your library, with totals, distribution by project/tags, and other aggregated information to better understand your knowledge collection.

## 4. Tips & Tricks

- **Use favorites** - Mark your most consulted entries as favorites to filter them quickly.
- **Stay consistent with tags and projects** - This makes filtering much easier as your library grows.
- **Take advantage of the preview** - Instead of always opening the editor, use the right-hand preview panel for quick lookups.
- **Back up regularly** - Use the **Export** option to save a copy of your library.
- **Help shortcut** - Press **F1** at any time to open this help dialog (or click **Help (F1)** in the side menu).

## 5. Frequently Asked Questions (FAQ)

**Q: How do I change the app language?**  
A: Use the PT/EN buttons at the bottom of the left navigation bar. The help will update automatically to the selected language.

**Q: How do I add a new entry?**  
A: In the Library view, click **+ New Entry** in the header, or use the available actions in the interface.

**Q: Can I duplicate an existing entry?**  
A: Yes. Using the card actions (depending on context), there's a **Duplicate entry** option to create an editable copy.

**Q: Where is my data stored?**  
A: The app uses a local database (`knowledge.db3`). In Debug mode it's stored in the solution root. In non-Debug builds it's stored in `%LOCALAPPDATA%\KnowledgeTrackerApp\`.

**Q: Can I open attachments/images and code from the preview?**  
A: Yes. If the entry has code or attachments, the preview panel shows buttons to view them (code preview or attachments/images gallery).

**Q: Can I open this help without using the menu?**  
A: Yes, just press **F1**.

## 6. Credits & Version

- **Application:** Knowledge Tracker
- **Version:** 1.0.0
- **Authors:** fauxtix
- **Copyright:** © 2026 fauxtix

---

*This help was updated based on the current features of the application. If new features are added, this document should be updated.*
