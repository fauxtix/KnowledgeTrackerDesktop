using System.Reflection;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.UI.Views;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    public class EntryCardTests
    {
        [Fact]
        public void CardProvidesMoreTitleSpaceWithReadableFont()
        {
            using var card = new EntryCard(new KnowledgeEntry
            {
                Id = 12,
                Title = "A longer title that should fit on a wider card",
            });

            var title = FindControls(card)
                .OfType<Label>()
                .Single(label => label.Text == card.Entry.Title);

            Assert.Equal(340, card.Width);
            Assert.Equal(12f, title.Font.Size);
        }

        [Fact]
        public void CardKeepsItsContentWithoutApplyingAClippingRegion()
        {
            using var card = new EntryCard(new KnowledgeEntry
            {
                Id = 11,
                Title = "Visible card",
                Description = "Description text",
                Tags = "ui, test",
            });

            Assert.Null(card.Region);
            Assert.Contains(
                FindControls(card).OfType<Label>(),
                label => label.Text == "Visible card");
            Assert.Contains(
                FindControls(card).OfType<Label>(),
                label => label.Text.Contains("Description text", StringComparison.Ordinal));
        }

        [Fact]
        public void RightClickSelectsTheCardUnderPointerAndItsMenuUsesThatEntry()
        {
            using var first = new EntryCard(new KnowledgeEntry { Id = 1, Title = "First" });
            using var second = new EntryCard(new KnowledgeEntry { Id = 2, Title = "Second" });
            int? selectedId = null;
            int? openedId = null;
            first.Selected += card => selectedId = card.Entry.Id;
            second.Selected += card => selectedId = card.Entry.Id;
            first.OpenRequested += card => openedId = card.Entry.Id;
            second.OpenRequested += card => openedId = card.Entry.Id;

            InvokeMouseDown(second, MouseButtons.Right);
            second.ContextMenuStrip!.Items[0].PerformClick();

            Assert.Equal(2, selectedId);
            Assert.Equal(2, openedId);
        }

        [Fact]
        public void FavoriteAffordanceDoesNotSelectOrOpenItsEntry()
        {
            using var card = new EntryCard(new KnowledgeEntry { Id = 3, Title = "Favorite me" });
            var selected = false;
            var opened = false;
            var toggled = false;
            card.Selected += _ => selected = true;
            card.OpenRequested += _ => opened = true;
            card.FavoriteRequested += _ => toggled = true;
            var favorite = FindControls(card)
                .OfType<Label>()
                .Single(control => control.AccessibleName == "Add favorite");

            InvokeMouseDown(favorite, MouseButtons.Left);
            InvokeOnClick(favorite);

            Assert.False(selected);
            Assert.False(opened);
            Assert.True(toggled);
        }

        [Fact]
        public void LeftClickSelectsAndOpensTheCardEntry()
        {
            using var card = new EntryCard(new KnowledgeEntry { Id = 7, Title = "Open me" });
            int? selectedId = null;
            int? openedId = null;
            card.Selected += selected => selectedId = selected.Entry.Id;
            card.OpenRequested += opened => openedId = opened.Entry.Id;

            InvokeMouseDown(card, MouseButtons.Left);

            Assert.Equal(7, selectedId);
            Assert.Equal(7, openedId);
        }

        [Fact]
        public void LeftClickOnChildControlSelectsAndOpens()
        {
            using var card = new EntryCard(new KnowledgeEntry { Id = 8, Title = "Select me" });
            int? selectedId = null;
            int? openedId = null;
            card.Selected += selected => selectedId = selected.Entry.Id;
            card.OpenRequested += opened => openedId = opened.Entry.Id;
            var child = FindControls(card).OfType<TableLayoutPanel>().First();

            InvokeMouseDown(child, MouseButtons.Left);

            Assert.Equal(8, selectedId);
            Assert.Equal(8, openedId);
        }

        [Fact]
        public void RightClickOnChildControlSelectsTheCard()
        {
            using var card = new EntryCard(new KnowledgeEntry { Id = 10, Title = "Select me" });
            int? selectedId = null;
            var opened = false;
            card.Selected += selected => selectedId = selected.Entry.Id;
            card.OpenRequested += _ => opened = true;
            var child = FindControls(card).OfType<TableLayoutPanel>().First();

            InvokeMouseDown(child, MouseButtons.Right);

            Assert.Equal(10, selectedId);
            Assert.False(opened);
        }

        private static IEnumerable<Control> FindControls(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var descendant in FindControls(child))
                {
                    yield return descendant;
                }
            }
        }

        private static void InvokeMouseDown(Control control, MouseButtons button)
        {
            var method = typeof(Control).GetMethod(
                "OnMouseDown",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            method.Invoke(control, new object[] { new MouseEventArgs(button, 1, 2, 2, 0) });
        }

        private static void InvokeOnClick(Control control)
        {
            var method = typeof(Control).GetMethod(
                "OnClick",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            method.Invoke(control, new object[] { EventArgs.Empty });
        }
    }
}
