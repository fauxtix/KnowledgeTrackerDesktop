using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.UI.Navigation;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    public class EntryEditorRequestTests
    {
        [Fact]
        public void UnsavedEntryIsTreatedAsNew()
        {
            var request = new EntryEditorRequest(new KnowledgeEntry());

            Assert.True(request.IsNew);
        }

        [Fact]
        public void PersistedEntryIsTreatedAsEdit()
        {
            var request = new EntryEditorRequest(new KnowledgeEntry { Id = 42 });

            Assert.False(request.IsNew);
        }
    }
}
