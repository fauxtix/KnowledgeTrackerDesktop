using KnowledgeTrackerDesktop.Core;

namespace KnowledgeTrackerDesktop.UI.Navigation
{
    /// <summary>
    /// Argument passed when opening the step editor for an existing entry.
    /// Steps are staged locally and applied back to the entry on demand
    /// (UI_DESIGN_SPEC section 6.9).
    /// </summary>
    public sealed record ManageStepsRequest(KnowledgeEntry Entry);
}
