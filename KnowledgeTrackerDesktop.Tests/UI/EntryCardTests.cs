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
            Assert.Equal(11f, title.Font.Size);
        }

        [Fact]
        public void CardShowsFieldsWithoutClippingRegionOrDescription()
        {
            using var card = new EntryCard(new KnowledgeEntry
            {
                Id = 11,
                Title = "Visible card",
                Description = "Hidden description",
                ProjectName = "Internal tool",
                Tags = "ui, test",
            });

            var texts = FindControls(card).OfType<Label>().Select(label => label.Text).ToArray();

            Assert.Null(card.Region);
            Assert.Contains("Visible card", texts);
            Assert.DoesNotContain(texts, text => text == "Hidden description");
            Assert.Contains("#ui", texts);
            Assert.Contains("#test", texts);
        }

        [Fact]
        public void CardOmitsTechnologiesAndUsesFieldCaptions()
        {
            using var card = new EntryCard(new KnowledgeEntry
            {
                Id = 14,
                Title = "Captioned card",
                ProjectName = "Internal tool",
                Technologies = "C#, Sqlite",
            });

            var texts = FindControls(card).OfType<Label>().Select(label => label.Text).ToArray();

            Assert.Contains("Project", texts);
            Assert.Contains("Internal tool", texts);
            Assert.DoesNotContain(texts, text => text == "Technologies");
            Assert.DoesNotContain(texts, text => text == "C#, Sqlite");
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
        public void DeleteAffordanceDoesNotSelectOrOpenAndRaisesDelete()
        {
            using var card = new EntryCard(new KnowledgeEntry { Id = 4, Title = "Delete me" });
            var selected = false;
            var opened = false;
            var deleted = false;
            card.Selected += _ => selected = true;
            card.OpenRequested += _ => opened = true;
            card.DeleteRequested += _ => deleted = true;
            var delete = FindControls(card)
                .OfType<Label>()
                .Single(control => control.AccessibleName == "Delete");

            InvokeMouseDown(delete, MouseButtons.Left);
            InvokeOnClick(delete);

            Assert.False(selected);
            Assert.False(opened);
            Assert.True(deleted);
        }

        [Fact]
        public void LeftClickSelectsWithoutOpeningTheCardEntry()
        {
            using var card = new EntryCard(new KnowledgeEntry { Id = 7, Title = "Open me" });
            int? selectedId = null;
            int? openedId = null;
            card.Selected += selected => selectedId = selected.Entry.Id;
            card.OpenRequested += opened => openedId = opened.Entry.Id;

            InvokeMouseDown(card, MouseButtons.Left);

            // Selection is what feeds the library preview; opening needs a
            // deliberate gesture so consulting an entry never leaves the list.
            Assert.Equal(7, selectedId);
            Assert.Null(openedId);
        }

        [Fact]
        public void DoubleClickOpensTheEntryOnce()
        {
            using var card = new EntryCard(new KnowledgeEntry { Id = 21, Title = "Double click me" });
            var opened = 0;
            card.OpenRequested += _ => opened++;

            InvokeMouseDown(card, MouseButtons.Left);

            Assert.Equal(0, opened);

            InvokeMouseDown(card, MouseButtons.Left, clicks: 2);
            InvokeMouseDoubleClick(card, MouseButtons.Left);

            // The second mouse-down and the double-click event describe the same
            // gesture; the card's debounce collapses them into a single open.
            Assert.Equal(1, opened);
        }

        [Fact]
        public void LeftClickOnChildControlOnlySelects()
        {
            using var card = new EntryCard(new KnowledgeEntry { Id = 8, Title = "Select me" });
            int? selectedId = null;
            int? openedId = null;
            card.Selected += selected => selectedId = selected.Entry.Id;
            card.OpenRequested += opened => openedId = opened.Entry.Id;
            var child = FindControls(card).OfType<TableLayoutPanel>().First();

            InvokeMouseDown(child, MouseButtons.Left);

            Assert.Equal(8, selectedId);
            Assert.Null(openedId);
        }

        [Fact]
        public void DoubleClickOnChildControlOpensTheEntry()
        {
            using var card = new EntryCard(new KnowledgeEntry { Id = 22, Title = "Open from child" });
            int? selectedId = null;
            int? openedId = null;
            card.Selected += selected => selectedId = selected.Entry.Id;
            card.OpenRequested += opened => openedId = opened.Entry.Id;
            var child = FindControls(card).OfType<TableLayoutPanel>().First();

            InvokeMouseDown(child, MouseButtons.Left, clicks: 2);

            Assert.Equal(22, selectedId);
            Assert.Equal(22, openedId);
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

        [Fact]
        public void GestureRulesSelectOnLeftClickAndOpenOnlyOnTheSecondClick()
        {
            using var card = new EntryCard(new KnowledgeEntry { Id = 30, Title = "Gesture" });
            var selected = 0;
            var opened = 0;
            card.Selected += _ => selected++;
            card.OpenRequested += _ => opened++;

            card.HandleGesture(MouseButtons.Left, 1);

            Assert.Equal(1, selected);
            Assert.Equal(0, opened);

            card.HandleGesture(MouseButtons.Left, 2);

            Assert.Equal(2, selected);
            Assert.Equal(1, opened);

            // The double-click event describes the same gesture as the second
            // mouse-down; the debounce keeps it a single open.
            card.HandleDoubleClick(MouseButtons.Left);

            Assert.Equal(2, selected);
            Assert.Equal(1, opened);

            card.HandleGesture(MouseButtons.Right, 1);

            Assert.Equal(3, selected);
            Assert.Equal(1, opened);
        }

        [Fact]
        public void RightAndMiddleGesturesNeverOpenTheEntry()
        {
            using var card = new EntryCard(new KnowledgeEntry { Id = 31, Title = "Right only" });
            var selected = 0;
            var opened = 0;
            card.Selected += _ => selected++;
            card.OpenRequested += _ => opened++;

            card.HandleGesture(MouseButtons.Right, 2);
            card.HandleDoubleClick(MouseButtons.Right);
            card.HandleGesture(MouseButtons.Middle, 1);

            Assert.Equal(1, selected);
            Assert.Equal(0, opened);
        }

        [Fact]
        public void CardGrowsTallerToFitMultipleTagRows()
        {
            using var card = new EntryCard(new KnowledgeEntry
            {
                Id = 13,
                Title = "Many tags",
                Tags = "alpha;beta;gamma;delta;epsilon;zeta;eta;theta",
            });

            Assert.True(card.Height >= 200);
            Assert.True(card.MinimumSize.Height >= 200);

            var tagFlow = FindControls(card)
                .OfType<FlowLayoutPanel>()
                .Single(flow => flow.Controls.OfType<Label>().Any(label => label.Text == "#alpha"));

            Assert.True(tagFlow.WrapContents);
        }

        [Fact]
        public void CardShowsTheOriginCaptionAndAShortLocation()
        {
            using var card = new EntryCard(new KnowledgeEntry
            {
                Id = 40,
                Title = "Sourced card",
                ProjectOriginKind = ProjectOriginKind.GitRepository,
                ProjectOriginLocation = "https://github.com/owner/repo",
            });

            var texts = FindControls(card).OfType<Label>().Select(label => label.Text).ToArray();

            Assert.Contains("Origin", texts);
            // The card shows a short form; the full location lives in the preview.
            Assert.Contains("owner/repo", texts);
            Assert.DoesNotContain("https://github.com/owner/repo", texts);
        }

        [Fact]
        public void CardWithoutOrigin_ShowsNoOriginCaption()
        {
            using var card = new EntryCard(new KnowledgeEntry
            {
                Id = 41,
                Title = "No origin",
            });

            var texts = FindControls(card).OfType<Label>().Select(label => label.Text).ToArray();

            Assert.DoesNotContain("Origin", texts);
        }

        [Fact]
        public void OriginAffordanceDoesNotSelectOrOpenAndRaisesOriginOpen()
        {
            using var card = new EntryCard(new KnowledgeEntry
            {
                Id = 42,
                Title = "Open the origin",
                ProjectOriginKind = ProjectOriginKind.LocalFolder,
                ProjectOriginLocation = @"C:\projects\app",
            });
            var selected = false;
            var opened = false;
            var originOpened = false;
            card.Selected += _ => selected = true;
            card.OpenRequested += _ => opened = true;
            card.OriginOpenRequested += _ => originOpened = true;
            var link = FindControls(card)
                .OfType<Label>()
                .Single(control => control.AccessibleName == "Open origin");

            InvokeMouseDown(link, MouseButtons.Left);
            InvokeOnClick(link);

            Assert.False(selected);
            Assert.False(opened);
            Assert.True(originOpened);
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

        private static void InvokeMouseDown(Control control, MouseButtons button, int clicks = 1)
        {
            var method = typeof(Control).GetMethod(
                "OnMouseDown",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            method.Invoke(control, new object[] { new MouseEventArgs(button, clicks, 2, 2, 0) });
        }

        private static void InvokeMouseDoubleClick(Control control, MouseButtons button)
        {
            var method = typeof(Control).GetMethod(
                "OnMouseDoubleClick",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            method.Invoke(control, new object[] { new MouseEventArgs(button, 2, 2, 2, 0) });
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
