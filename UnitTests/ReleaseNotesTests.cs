using System;
using System.IO.Abstractions.TestingHelpers;
using Xunit;
using AssimilationSoftware.Buildster.Core.Utils;

namespace AssimilationSoftware.Buildster.Tests
{
    public class ReleaseNotesTests
    {
        // --- Constructor Tests ---

        [Fact]
        public void Constructor_WithStandardFilePath_SetsFilePathCorrectly()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var expectedPath = @"c:\temp\notes.md";

            // Act
            var releaseNotes = new ReleaseNotes(expectedPath, mockFileSystem);

            // Assert
            Assert.Equal(expectedPath, releaseNotes.FilePath);
        }

        [Fact]
        public void Constructor_WhenPathIsExistingDirectory_AppendsReadMeMd()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var directoryPath = @"c:\temp\releases";
            // Simulate that the directory exists on disk
            mockFileSystem.AddDirectory(directoryPath);
            
            // Act
            var releaseNotes = new ReleaseNotes(directoryPath, mockFileSystem);

            // Assert
            // The constructor should automatically append "ReadMe.md" if the path is an existing directory
            var expectedPath = mockFileSystem.Path.Combine(directoryPath, "ReadMe.md");
            Assert.Equal(expectedPath, releaseNotes.FilePath);
        }

        // --- AppendNotes (Instance) Tests ---

        [Fact]
        public void AppendNotes_CreatesNewFile_WithCorrectFormatting()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var filePath = @"c:\temp\ReleaseNotes.md";
            var releaseNotesObj = new ReleaseNotes(filePath, mockFileSystem);
            
            var date = new DateTime(2026, 7, 27);
            
            // Passing one note with a period, and one without
            var notes = new[] { "Fixed a critical bug.", "Added a new dashboard feature" }; 

            // Act
            // Note: Passing null for VersionNumber since its concrete implementation is external to ReleaseNotes.cs
            releaseNotesObj.AppendNotes(date, null, notes);

            // Assert
            Assert.True(mockFileSystem.FileExists(filePath));
            var content = mockFileSystem.File.ReadAllText(filePath);
            
            // Verify the date/build header was added
            Assert.Contains("- 2026-07-27: Build ", content);
            
            // Verify notes formatting and that the missing period was appended
            Assert.Contains("\t- Fixed a critical bug.", content);
            Assert.Contains("\t- Added a new dashboard feature.", content); 
        }

        // --- AppendNotes (Static) Tests ---

        [Fact]
        public void AppendNotesStatic_WhenAppendedToExistingFileWithoutNewline_PrependsNewline()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var filePath = @"c:\temp\ReleaseNotes.md";
            
            // Create an existing file that does NOT end with a newline
            mockFileSystem.AddFile(filePath, new MockFileData("Initial legacy content without newline"));
            var notes = new[] { "Routine update." };

            // Act
            ReleaseNotes.AppendNotes(filePath, DateTime.Today, null, notes, mockFileSystem);

            // Assert
            var content = mockFileSystem.File.ReadAllText(filePath);
            
            // Verify a newline was injected between the old text and the new notes
            Assert.StartsWith($"Initial legacy content without newline{Environment.NewLine}", content);
            Assert.Contains("\t- Routine update.", content);
        }

        [Fact]
        public void AppendNotesStatic_WhenNotesArrayIsNull_OnlyAppendsHeader()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var filePath = @"c:\temp\ReleaseNotes.md";
            var date = new DateTime(2026, 1, 1);

            // Act
            // Passing null for the releaseNotes array
            ReleaseNotes.AppendNotes(filePath, date, null, null, mockFileSystem);

            // Assert
            var content = mockFileSystem.File.ReadAllText(filePath);
            Assert.Contains("- 2026-01-01: Build ", content);
            
            // Ensure no tabbed bullet points were added
            Assert.DoesNotContain("\t- ", content);
        }
    }
}