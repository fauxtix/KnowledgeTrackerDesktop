using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.Services
{
    /// <summary>
    /// Pure unit tests for the shared entry validation rules (new and edited
    /// entries go through the same validator before persistence).
    /// </summary>
    public class EntryValidatorTests
    {
        [Fact]
        public void Validate_CompleteEntry_HasNoErrors()
        {
            var errors = EntryValidator.Validate(CompleteEntry());

            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_NullEntry_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => EntryValidator.Validate(null!));
        }

        [Fact]
        public void Validate_MissingTitle_ReportsTitleError()
        {
            var entry = CompleteEntry();
            entry.Title = "   ";

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.Title, error.Field);
            Assert.Equal("A title is required.", error.Message);
        }

        [Fact]
        public void Validate_MissingProject_ReportsProjectError()
        {
            var entry = CompleteEntry();
            entry.ProjectName = null;

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.ProjectName, error.Field);
            Assert.Equal("A project name is required.", error.Message);
        }

        [Fact]
        public void Validate_MissingTechnologies_ReportsRequiredError()
        {
            var entry = CompleteEntry();
            entry.Technologies = " ";

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.Technologies, error.Field);
            Assert.Equal("At least one technology is required.", error.Message);
        }

        [Theory]
        [InlineData("C#;;WinForms")]
        [InlineData("C#, WinForms,")]
        public void Validate_ListWithEmptyItem_ReportsFormatError(string technologies)
        {
            var entry = CompleteEntry();
            entry.Technologies = technologies;

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.Technologies, error.Field);
            Assert.Equal(
                "Technologies must be a comma or semicolon separated list without empty items.",
                error.Message);
        }

        [Fact]
        public void Validate_DuplicateTechnologiesIgnoringCase_ReportsDuplicateError()
        {
            var entry = CompleteEntry();
            entry.Technologies = "C#; c#";

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.Technologies, error.Field);
            Assert.Equal("Technologies cannot contain duplicate items.", error.Message);
        }

        [Fact]
        public void Validate_MissingDate_ReportsRequiredError()
        {
            var entry = CompleteEntry();
            entry.DateResolved = null;

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.DateResolved, error.Field);
            Assert.Equal("A resolution date is required.", error.Message);
        }

        [Fact]
        public void Validate_FutureDate_ReportsFutureDateError()
        {
            var entry = CompleteEntry();
            entry.DateResolved = DateTime.Today.AddDays(1);

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.DateResolved, error.Field);
            Assert.Equal("The resolution date cannot be in the future.", error.Message);
        }

        [Fact]
        public void Validate_TodayAsDate_IsAccepted()
        {
            var entry = CompleteEntry();
            entry.DateResolved = DateTime.Today;

            Assert.Empty(EntryValidator.Validate(entry));
        }

        [Fact]
        public void Validate_MissingTags_ReportsRequiredError()
        {
            var entry = CompleteEntry();
            entry.Tags = null;

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.Tags, error.Field);
            Assert.Equal("At least one tag is required.", error.Message);
        }

        [Fact]
        public void Validate_DuplicateTags_ReportsDuplicateError()
        {
            var entry = CompleteEntry();
            entry.Tags = "sqlite; SQLITE";

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.Tags, error.Field);
            Assert.Equal("Tags cannot contain duplicate items.", error.Message);
        }

        [Fact]
        public void Validate_MissingDescription_ReportsRequiredError()
        {
            var entry = CompleteEntry();
            entry.Description = "\r\n ";

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.Description, error.Field);
            Assert.Equal("A description is required.", error.Message);
        }

        [Fact]
        public void Validate_MissingYouTubeUrl_IsAccepted()
        {
            var entry = CompleteEntry();
            entry.YouTubeUrl = null;

            Assert.Empty(EntryValidator.Validate(entry));
        }

        [Theory]
        [InlineData("youtube.com/watch?v=abc")]
        [InlineData("ftp://example.com/video")]
        [InlineData("not a url")]
        public void Validate_InvalidYouTubeUrl_ReportsUrlError(string url)
        {
            var entry = CompleteEntry();
            entry.YouTubeUrl = url;

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.YouTubeUrl, error.Field);
            Assert.Equal("The YouTube URL must be a valid http(s) link.", error.Message);
        }

        [Fact]
        public void Validate_MissingOrigin_IsAccepted()
        {
            var entry = CompleteEntry();
            entry.ProjectOriginKind = null;
            entry.ProjectOriginLocation = null;

            Assert.Empty(EntryValidator.Validate(entry));
        }

        [Fact]
        public void Validate_OriginWithKindAndLocation_IsAccepted()
        {
            var entry = CompleteEntry();
            entry.ProjectOriginKind = ProjectOriginKind.LocalFolder;
            entry.ProjectOriginLocation = @"C:\NewProjects\KnowledgeTrackerDesktop";

            Assert.Empty(EntryValidator.Validate(entry));
        }

        [Fact]
        public void Validate_OriginKindWithoutLocation_ReportsLocationError()
        {
            var entry = CompleteEntry();
            entry.ProjectOriginKind = ProjectOriginKind.GitRepository;
            entry.ProjectOriginLocation = "   ";

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.OriginLocation, error.Field);
            Assert.Equal(
                "The origin location is required when an origin type is selected.",
                error.Message);
        }

        [Fact]
        public void Validate_OriginLocationWithoutKind_ReportsKindError()
        {
            var entry = CompleteEntry();
            entry.ProjectOriginKind = null;
            entry.ProjectOriginLocation = @"C:\NewProjects\KnowledgeTrackerDesktop";

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.OriginKind, error.Field);
            Assert.Equal(
                "Select the origin type for the recorded location.",
                error.Message);
        }

        [Theory]
        [InlineData("relative/folder")]
        [InlineData("https://github.com/owner/repo")]
        public void Validate_LocalFolderWithRelativePath_ReportsLocationError(string location)
        {
            var entry = CompleteEntry();
            entry.ProjectOriginKind = ProjectOriginKind.LocalFolder;
            entry.ProjectOriginLocation = location;

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.OriginLocation, error.Field);
            Assert.Equal(
                "The origin location must be an absolute folder path.",
                error.Message);
        }

        [Theory]
        [InlineData("git@github.com:owner/repo.git")]
        [InlineData("owner/repo")]
        [InlineData("ftp://example.com/repo")]
        public void Validate_GitRepositoryWithNonHttpUrl_ReportsLocationError(string location)
        {
            var entry = CompleteEntry();
            entry.ProjectOriginKind = ProjectOriginKind.GitRepository;
            entry.ProjectOriginLocation = location;

            var error = Assert.Single(EntryValidator.Validate(entry));

            Assert.Equal(EntryField.OriginLocation, error.Field);
            Assert.Equal(
                "The repository URL must be a valid http(s) link.",
                error.Message);
        }

        [Theory]
        [InlineData("https://stackoverflow.com/questions/1")]
        [InlineData("http://example.com/page")]
        public void Validate_WebPageWithHttpUrl_IsAccepted(string location)
        {
            var entry = CompleteEntry();
            entry.ProjectOriginKind = ProjectOriginKind.WebPage;
            entry.ProjectOriginLocation = location;

            Assert.Empty(EntryValidator.Validate(entry));
        }

        [Fact]
        public void Validate_EmptyEntry_ReportsEveryRuleInFieldOrder()
        {
            var errors = EntryValidator.Validate(new KnowledgeEntry());

            Assert.Equal(
                new[]
                {
                    EntryField.Title,
                    EntryField.ProjectName,
                    EntryField.Technologies,
                    EntryField.DateResolved,
                    EntryField.Tags,
                    EntryField.Description,
                },
                errors.Select(error => error.Field).ToArray());
        }

        private static KnowledgeEntry CompleteEntry() => new()
        {
            Title = "Solved the sync deadlock",
            ProjectName = "KnowledgeTracker",
            Technologies = "C#; WinForms",
            DateResolved = new DateTime(2026, 10, 1),
            Tags = "sqlite, deadlock",
            Description = "The deadlock came from sharing one connection.",
            YouTubeUrl = "https://www.youtube.com/watch?v=abc123",
        };
    }
}
