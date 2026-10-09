using KnowledgeTrackerDesktop.Core;

namespace KnowledgeTrackerDesktop.UI.Navigation
{
    /// <summary>
    /// Argument passed when navigating to the Add/Edit editor. A brand-new entry
    /// has an unsaved <see cref="KnowledgeEntry"/> with Id 0.
    /// </summary>
    public sealed record EntryEditorRequest(KnowledgeEntry Entry)
    {
        public bool IsNew => Entry.Id <= 0;
    }
}
