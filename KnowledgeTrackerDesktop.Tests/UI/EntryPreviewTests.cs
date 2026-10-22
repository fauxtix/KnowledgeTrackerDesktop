using System.Reflection;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.UI;
using KnowledgeTrackerDesktop.UI.Views;
using Syncfusion.WinForms.Controls;
using Syncfusion.Windows.Forms.Tools;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// The preview pane is pure presentation: it renders the entry the selected
    /// card already carries and raises intents for the actions the view owns.
    /// Everything asserted here is observable state of the control (rendered
    /// values, intents, ownership of the created controls); the OS delivery of
    /// mouse messages and the opening of external applications stay out of the
    /// automated scope and are covered by the manual checklist.
    /// </summary>
    public class EntryPreviewTests
    {
        [Fact]
        public void SelectingAnEntryRendersItsFields()
        {
            using var preview = new ctlEntryPreview();
            var entry = new KnowledgeEntry
            {
                Id = 1,
                Title = "Storage queue backpressure",
                ProjectName = "Message broker",
                Tags = "c#; azure",
                DateResolved = new DateTime(2026, 3, 7),
                IsFavorite = true,
                Description = "The queue **filled up**.",
            };

            preview.SetEntry(entry);

            Assert.False(preview.IsEmpty);
            Assert.Same(entry, preview.CurrentEntry);
            Assert.Equal("Storage queue backpressure", preview.TitleText);
            Assert.Equal("Message broker", preview.ProjectText);
            Assert.Equal("c#, azure", preview.TagsText);
            Assert.True(preview.ShowsFavorite);
            Assert.True(preview.ShowsProject);
            Assert.True(preview.ShowsTags);
            Assert.True(preview.ShowsDate);
            Assert.True(preview.ShowsDescription);
        }

        [Fact]
        public void RenderingIsReadOnlyAndNeverOffersInputs()
        {
            using var preview = new ctlEntryPreview();

            preview.SetEntry(new KnowledgeEntry { Id = 1, Title = "Read only" });

            Assert.Empty(Descendants(preview).OfType<TextBox>());
            Assert.Empty(Descendants(preview).OfType<TextBoxExt>());
        }

        [Fact]
        public void ChangingTheSelectionReplacesEveryRenderedValue()
        {
            using var preview = new ctlEntryPreview();
            var first = new KnowledgeEntry
            {
                Id = 1,
                Title = "First",
                ProjectName = "Project A",
                Tags = "alpha",
                Description = "First description",
                CodeSnippet = "var first = 1;",
                Steps = { new Step { Order = 1, Description = "First step" } },
            };
            var second = new KnowledgeEntry
            {
                Id = 2,
                Title = "Second",
                ProjectName = "Project B",
                Tags = "beta",
                Description = "Second description",
                YouTubeUrl = "https://www.youtube.com/watch?v=abc",
                Attachments = { new AttachmentInfo { FileName = "second.png" } },
            };

            preview.SetEntry(first);

            Assert.Equal("First", preview.TitleText);
            Assert.Equal("Project A", preview.ProjectText);
            Assert.Equal("alpha", preview.TagsText);
            Assert.True(preview.ShowsCodeAction);
            Assert.False(preview.ShowsLinkAction);
            Assert.Single(preview.StepTexts);

            preview.SetEntry(second);

            Assert.Equal("Second", preview.TitleText);
            Assert.Equal("Project B", preview.ProjectText);
            Assert.Equal("beta", preview.TagsText);
            Assert.False(preview.ShowsCodeAction);
            Assert.True(preview.ShowsLinkAction);
            Assert.Empty(preview.StepTexts);
            Assert.Equal(new[] { "second.png" }, preview.AttachmentNames);
            Assert.DoesNotContain(
                Descendants(preview).OfType<Label>(),
                label => label.Text.Contains("First", StringComparison.Ordinal));
        }

        [Fact]
        public void ClearingTheSelectionShowsTheEmptyState()
        {
            using var preview = new ctlEntryPreview();
            preview.SetEntry(new KnowledgeEntry { Id = 1, Title = "Selected" });
            Assert.NotEmpty(Descendants(preview).OfType<SfButton>());

            preview.Clear();

            Assert.True(preview.IsEmpty);
            Assert.Null(preview.CurrentEntry);
            Assert.Equal(string.Empty, preview.TitleText);
            Assert.Equal(string.Empty, preview.DescriptionText);
            Assert.Empty(preview.StepTexts);
            Assert.Empty(preview.AttachmentNames);
            Assert.Empty(Descendants(preview).OfType<SfButton>());
            Assert.Contains(
                Descendants(preview).OfType<Label>(),
                label => label.Text == AppLocalization.Translate("Select an entry to preview it."));
        }

        [Fact]
        public void StepsAreListedInOrderWithTheirPositions()
        {
            using var preview = new ctlEntryPreview();

            preview.SetEntry(new KnowledgeEntry
            {
                Id = 1,
                Title = "Out of order steps",
                Steps =
                {
                    new Step { Order = 3, Description = "Third" },
                    new Step { Order = 1, Description = "First" },
                    new Step { Order = 2, Description = "Second" },
                },
            });

            Assert.True(preview.ShowsSteps);
            Assert.Equal(new[] { "First", "Second", "Third" }, preview.StepTexts);
            Assert.Equal(
                new[] { StepHeading(1), StepHeading(2), StepHeading(3) },
                preview.StepHeadings);
        }

        [Fact]
        public void LegacyResolutionTextIsShownWhenThereAreNoStructuredSteps()
        {
            using var preview = new ctlEntryPreview();

            preview.SetEntry(new KnowledgeEntry
            {
                Id = 1,
                Title = "Legacy entry",
                ResolutionSteps = "Restarted the worker.",
            });

            Assert.True(preview.ShowsSteps);
            Assert.Equal(new[] { "Restarted the worker." }, preview.StepTexts);
            Assert.Equal(new[] { StepHeading(1) }, preview.StepHeadings);
        }

        [Fact]
        public void AttachmentsAreListedInOrderAndRaiseTheClickedFile()
        {
            using var preview = new ctlEntryPreview();
            var first = new AttachmentInfo { Id = 3, FileName = "trace.log" };
            var second = new AttachmentInfo { Id = 4, FilePath = @"C:\temp\dump.zip" };
            var entry = new KnowledgeEntry
            {
                Id = 1,
                Title = "With attachments",
                Attachments = { first, second },
            };
            preview.SetEntry(entry);

            Assert.True(preview.ShowsAttachmentsAction);
            Assert.Equal(new[] { "trace.log", "dump.zip" }, preview.AttachmentNames);

            var opened = new List<AttachmentInfo>();
            preview.OpenAttachmentRequested += (raisedEntry, attachment) =>
            {
                Assert.Same(entry, raisedEntry);
                opened.Add(attachment);
            };

            var link = Descendants(preview)
                .OfType<Label>()
                .Single(label => label.Text == "trace.log");
            InvokeOnClick(link);
            preview.RequestOpenAttachment(second);

            // A file from another entry (or a stale one) must not open here.
            preview.RequestOpenAttachment(new AttachmentInfo { FileName = "other.txt" });

            Assert.Equal(new[] { first, second }, opened);

            preview.Clear();
            preview.RequestOpenAttachment(first);

            Assert.Equal(2, opened.Count);
        }

        [Fact]
        public void AbsentOptionalFieldsAreOmittedAndDoNotThrow()
        {
            using var preview = new ctlEntryPreview();

            preview.SetEntry(new KnowledgeEntry { Id = 1, Title = "Bare entry" });

            Assert.False(preview.ShowsProject);
            Assert.False(preview.ShowsTags);
            Assert.False(preview.ShowsDate);
            Assert.False(preview.ShowsFavorite);
            Assert.False(preview.ShowsDescription);
            Assert.False(preview.ShowsSteps);
            Assert.False(preview.ShowsCodeAction);
            Assert.False(preview.ShowsAttachmentsAction);
            Assert.False(preview.ShowsLinkAction);

            var captions = Descendants(preview).OfType<Label>().Select(label => label.Text).ToArray();
            Assert.DoesNotContain("Project", captions);
            Assert.DoesNotContain("Description", captions);
            Assert.DoesNotContain("Resolution steps", captions);
            Assert.DoesNotContain("Attachments", captions);

            // Only the edit action remains available.
            var button = Assert.Single(Descendants(preview).OfType<SfButton>());
            Assert.Equal("Edit Entry", button.Tag);
        }

        [Fact]
        public void DescriptionIsRenderedThroughTheMarkdownRenderer()
        {
            using var preview = new ctlEntryPreview();
            const string markdown = "# Title\n\n- item **one**\n- [link](https://example.com)";

            preview.SetEntry(new KnowledgeEntry
            {
                Id = 1,
                Title = "Rendered description",
                Description = markdown,
            });

            Assert.Equal(MarkdownPreviewRenderer.ToPlainText(markdown), preview.DescriptionText);
            Assert.DoesNotContain("**", preview.DescriptionText);
        }

        [Fact]
        public void EditActionIsAlwaysAvailableAndRaisedWithTheSelectedEntry()
        {
            using var preview = new ctlEntryPreview();
            var entry = new KnowledgeEntry { Id = 12, Title = "Edit me" };
            preview.SetEntry(entry);
            var raised = new List<KnowledgeEntry>();
            preview.EditRequested += raised.Add;

            var button = Descendants(preview)
                .OfType<SfButton>()
                .Single(control => Equals(control.Tag, "Edit Entry"));
            InvokeOnClick(button);

            Assert.Equal(new[] { entry }, raised);

            preview.Clear();
            preview.RequestEditEntry();

            // A cleared preview must not raise anything for a stale entry.
            Assert.Single(raised);
        }

        [Fact]
        public void CodeAndLinkActionsFollowTheEntryAndTheLinkIsValidated()
        {
            using var preview = new ctlEntryPreview();

            preview.SetEntry(new KnowledgeEntry
            {
                Id = 1,
                Title = "With code",
                CodeSnippet = "Console.WriteLine();",
            });

            Assert.True(preview.ShowsCodeAction);
            Assert.False(preview.ShowsLinkAction);
            Assert.Contains(Descendants(preview).OfType<SfButton>(), button => Equals(button.Tag, "Open code"));

            preview.SetEntry(new KnowledgeEntry
            {
                Id = 2,
                Title = "Unsafe link",
                YouTubeUrl = "javascript:alert(1)",
            });

            Assert.False(preview.ShowsLinkAction);
            Assert.DoesNotContain(Descendants(preview).OfType<SfButton>(), button => Equals(button.Tag, "Open video"));

            preview.SetEntry(new KnowledgeEntry
            {
                Id = 3,
                Title = "Video",
                YouTubeUrl = "  https://youtu.be/abc  ",
            });

            Assert.True(preview.ShowsLinkAction);
            Assert.Contains(Descendants(preview).OfType<SfButton>(), button => Equals(button.Tag, "Open video"));
        }

        [Fact]
        public void IntentionsAreOnlyRaisedWhileAnEntryIsSelected()
        {
            using var preview = new ctlEntryPreview();
            var raised = 0;
            preview.EditRequested += _ => raised++;
            preview.OpenCodeRequested += _ => raised++;
            preview.OpenAttachmentsRequested += _ => raised++;
            preview.OpenLinkRequested += _ => raised++;

            preview.RequestEditEntry();
            preview.RequestOpenCode();
            preview.RequestOpenAttachments();
            preview.RequestOpenLink();

            Assert.Equal(0, raised);
        }

        [Fact]
        public void RebuildingThePreviewDisposesThePreviousControls()
        {
            using var preview = new ctlEntryPreview();
            preview.SetEntry(new KnowledgeEntry
            {
                Id = 1,
                Title = "Before",
                Description = "Before description",
                CodeSnippet = "var before = 1;",
            });

            var previous = Descendants(preview)
                .Where(control => control is Label or SfButton)
                .ToArray();
            Assert.NotEmpty(previous);

            preview.SetEntry(new KnowledgeEntry { Id = 2, Title = "After" });

            Assert.All(previous, control => Assert.True(control.IsDisposed));

            var current = Descendants(preview)
                .Where(control => control is Label or SfButton)
                .ToArray();
            Assert.NotEmpty(current);
            Assert.All(current, control => Assert.False(control.IsDisposed));

            preview.Clear();

            Assert.All(current, control => Assert.True(control.IsDisposed));
        }

        [Fact]
        public void TextBlocksUseTheAvailableWidthAndRewrapWhenThePaneResizes()
        {
            using var preview = new ctlEntryPreview();
            preview.SetEntry(new KnowledgeEntry
            {
                Id = 1,
                Title = "A title long enough to wrap once the pane becomes narrow",
                Description = string.Join(" ", Enumerable.Repeat("palavra", 80)),
            });

            preview.Size = new Size(720, 400);
            preview.PerformLayout();

            var description = LabelWithText(preview, preview.DescriptionText);
            var wideWidth = description.Width;
            var wideHeight = description.Height;

            // The text fills the pane instead of wrapping at a fixed width.
            Assert.True(wideWidth > 500, $"description width was {wideWidth}");

            preview.Size = new Size(400, 400);
            preview.PerformLayout();

            Assert.True(description.Width < wideWidth, "narrowing the pane must narrow the text block");
            Assert.True(
                description.Height > wideHeight,
                "narrowing the pane must re-wrap the text on more lines");

            preview.Size = new Size(900, 400);
            preview.PerformLayout();

            Assert.True(description.Width > wideWidth, "widening the pane must widen the text block");
            Assert.True(
                description.Height <= wideHeight,
                "widening the pane must not keep the wrapping of the narrower width");
        }

        [Fact]
        public void TitleAndActionButtonsFollowThePaneWidth()
        {
            using var preview = new ctlEntryPreview();
            preview.SetEntry(new KnowledgeEntry
            {
                Id = 1,
                Title = "A title long enough to wrap once the pane becomes narrow",
                CodeSnippet = "var value = 1;",
                Attachments = { new AttachmentInfo { FileName = "screenshot.png" } },
                YouTubeUrl = "https://youtu.be/abc",
            });

            preview.Size = new Size(900, 500);
            preview.PerformLayout();
            var titleWide = LabelWithText(preview, preview.TitleText).Width;
            var actionsWide = ActionsPanel(preview).Height;

            preview.Size = new Size(392, 500);
            preview.PerformLayout();
            var titleNarrow = LabelWithText(preview, preview.TitleText).Width;
            var actionsNarrow = ActionsPanel(preview).Height;

            Assert.True(titleWide > titleNarrow, $"title width {titleWide} -> {titleNarrow}");
            Assert.True(
                actionsNarrow > actionsWide,
                $"the action buttons must wrap instead of overflowing ({actionsWide} -> {actionsNarrow})");
        }

        [Fact]
        public void EveryLocalizablePreviewTextHasAPortugueseTranslation()
        {
            using var preview = new ctlEntryPreview();
            preview.SetEntry(new KnowledgeEntry
            {
                Id = 1,
                Title = "Localizable",
                ProjectName = "Some project",
                Tags = "one; two",
                DateResolved = new DateTime(2026, 1, 2),
                Description = "A description",
                ResolutionSteps = "The legacy resolution text",
                CodeSnippet = "var x = 1;",
                Attachments = { new AttachmentInfo { FileName = "file.txt" } },
                YouTubeUrl = "https://youtu.be/abc",
            });

            var keys = Descendants(preview)
                .Select(control => control.Tag)
                .OfType<string>()
                .Distinct()
                .ToArray();

            Assert.NotEmpty(keys);
            Assert.All(
                keys,
                key => Assert.NotEqual(
                    key,
                    AppLocalization.Translate(key, AppLanguage.Portuguese)));
        }

        [Fact]
        public void LocalizablePreviewTextsRefreshInPlaceAndStepHeadingsStayLazy()
        {
            using var preview = new ctlEntryPreview();
            preview.SetEntry(new KnowledgeEntry
            {
                Id = 1,
                Title = "With steps",
                Description = "Description text",
                Steps = { new Step { Order = 1, Description = "Do it" } },
            });

            var buttons = Descendants(preview).OfType<SfButton>().ToArray();
            var caption = Descendants(preview)
                .OfType<Label>()
                .Single(label => Equals(label.Tag, "Description"));
            var heading = Descendants(preview)
                .OfType<Label>()
                .Single(label => label.Text == StepHeading(1));

            // The heading is bound through a factory that recomputes from the
            // current language; a frozen string would survive a language switch.
            var headingFactory = Assert.IsAssignableFrom<Func<string>>(heading.Tag);
            Assert.Equal(StepHeading(1), headingFactory());

            AppLocalization.Apply(preview);

            // Re-applying the language refreshes the same controls in place.
            Assert.All(buttons, button => Assert.False(button.IsDisposed));
            Assert.Contains(buttons, button => Equals(button.Tag, "Edit Entry"));
            Assert.Equal(AppLocalization.Translate("Description"), caption.Text);
            Assert.Equal(headingFactory(), heading.Text);
            Assert.False(heading.IsDisposed);
        }

        [Fact]
        public void DisposingThePreviewKeepsSharedThemeFontsUsable()
        {
            var borrowed = new[]
            {
                AppTheme.Fonts.Body,
                AppTheme.Fonts.CardTitle,
                AppTheme.Fonts.Caption,
                AppTheme.Fonts.Small,
            };

            using (var preview = new ctlEntryPreview())
            {
                preview.SetEntry(new KnowledgeEntry
                {
                    Id = 1,
                    Title = "Fonts",
                    Description = "A description",
                    ResolutionSteps = "A step",
                    CodeSnippet = "var x = 1;",
                });

                // The preview owns buttons and labels that borrow the shared theme
                // fonts; disposing them must not dispose the fonts themselves.
                preview.Clear();
                preview.SetEntry(new KnowledgeEntry { Id = 2, Title = "Again" });
            }

            Assert.All(borrowed, font => Assert.True(font.GetHeight() > 0));
        }

        [Fact]
        public void ActionButtonsShareTheWidestButtonsWidth()
        {
            using var preview = new ctlEntryPreview();
            preview.SetEntry(new KnowledgeEntry
            {
                Id = 1,
                Title = "Equal buttons",
                CodeSnippet = "var value = 1;",
                Attachments =
                {
                    new AttachmentInfo { FileName = "a.png" },
                    new AttachmentInfo { FileName = "b.png" },
                },
                YouTubeUrl = "https://youtu.be/abc",
            });

            preview.Size = new Size(900, 500);
            preview.PerformLayout();

            var buttons = Descendants(preview).OfType<SfButton>().ToArray();
            Assert.True(buttons.Length > 1, "expected several action buttons");

            // Every button is as wide as the widest caption, so the row lines up
            // instead of each sizing to its own (differently long) text.
            var widths = buttons.Select(button => button.Width).Distinct().ToArray();
            Assert.Single(widths);
            Assert.Equal(buttons.Max(button => button.Width), widths[0]);

            // The equalised width fits the longest caption in either language.
            // The English "Open attachments (2)" is longer than the Portuguese
            // translation, so it must be the one the width is measured against.
            var englishAttachments = AppLocalization.Translate(
                "Open attachments ({0})",
                AppLanguage.English);
            var portugueseAttachments = AppLocalization.Translate(
                "Open attachments ({0})",
                AppLanguage.Portuguese);
            var englishWidth = TextRenderer.MeasureText(
                string.Format(AppLocalization.Culture, englishAttachments, 2),
                buttons[0].Font).Width;
            var portugueseWidth = TextRenderer.MeasureText(
                string.Format(AppLocalization.Culture, portugueseAttachments, 2),
                buttons[0].Font).Width;
            Assert.True(
                englishWidth > portugueseWidth,
                "the English caption is expected to be the widest one");
            Assert.True(
                widths[0] >= englishWidth,
                $"button width {widths[0]} must fit the English caption ({englishWidth})");

            // The visual hierarchy is untouched: Primary stays taller than Secondary.
            var primary = buttons.Single(button => Equals(button.Tag, "Edit Entry"));
            var secondary = buttons.First(button => !Equals(button.Tag, "Edit Entry"));
            Assert.True(
                primary.Height > secondary.Height,
                $"primary height {primary.Height} should exceed secondary {secondary.Height}");
        }

        [Fact]
        public void TheOriginIsRenderedWithItsKindAndFullLocation()
        {
            using var preview = new ctlEntryPreview();

            preview.SetEntry(new KnowledgeEntry
            {
                Id = 1,
                Title = "Sourced entry",
                ProjectOriginKind = ProjectOriginKind.GitRepository,
                ProjectOriginLocation = "https://github.com/owner/repo",
            });

            Assert.True(preview.ShowsOrigin);
            Assert.Equal("Git repository", preview.OriginKindText);
            Assert.Equal("https://github.com/owner/repo", preview.OriginLocationText);

            var captions = Descendants(preview).OfType<Label>().Select(label => label.Text).ToArray();
            Assert.Contains("Origin", captions);
            Assert.Contains("https://github.com/owner/repo", captions);
        }

        [Fact]
        public void TheOriginActionIsOfferedOnlyWhenThereIsAnOrigin()
        {
            using var preview = new ctlEntryPreview();
            var entry = new KnowledgeEntry
            {
                Id = 1,
                Title = "Sourced entry",
                ProjectOriginKind = ProjectOriginKind.LocalFolder,
                ProjectOriginLocation = @"C:\projects\app",
            };
            var raised = new List<KnowledgeEntry>();
            preview.OpenOriginRequested += raised.Add;

            preview.SetEntry(entry);

            Assert.True(preview.ShowsOrigin);
            Assert.Contains(
                Descendants(preview).OfType<SfButton>(),
                button => Equals(button.Tag, "Open origin"));

            InvokeOnClick(Descendants(preview)
                .OfType<SfButton>()
                .Single(button => Equals(button.Tag, "Open origin")));

            Assert.Equal(new[] { entry }, raised);

            preview.SetEntry(new KnowledgeEntry { Id = 2, Title = "No origin" });

            Assert.False(preview.ShowsOrigin);
            Assert.DoesNotContain(
                Descendants(preview).OfType<SfButton>(),
                button => Equals(button.Tag, "Open origin"));
        }

        [Fact]
        public void TheOriginIntentIsNotRaisedWithoutAnEntry()
        {
            using var preview = new ctlEntryPreview();
            var raised = 0;
            preview.OpenOriginRequested += _ => raised++;

            preview.RequestOpenOrigin();

            Assert.Equal(0, raised);
        }

        private static Label LabelWithText(Control root, string text) =>
            Descendants(root).OfType<Label>().Single(label => label.Text == text);

        private static FlowLayoutPanel ActionsPanel(Control root) =>
            Descendants(root).OfType<FlowLayoutPanel>().Single();

        private static string StepHeading(int position) =>
            string.Format(
                AppLocalization.Culture,
                AppLocalization.Translate("Step {0}"),
                position);

        private static IEnumerable<Control> Descendants(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
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
