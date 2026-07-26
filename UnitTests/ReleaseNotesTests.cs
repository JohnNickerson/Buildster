using System;
using Xunit;
using AssimilationSoftware.Buildster.Core.Utils;

namespace AssimilationSoftware.Buildster.Tests
{
    public class ReleaseNotesTests
    {
        [Fact]
        public void Constructor_WithDummyPath_DoesNotThrow()
        {
            // Arrange
            string dummyPath = "dummy_release_notes.md";

            // Act
            // Record.Exception captures any exception thrown during the execution of the lambda
            var exception = Record.Exception(() => new ReleaseNotes(dummyPath, new System.IO.Abstractions.TestingHelpers.MockFileSystem()));

            // Assert
            Assert.Null(exception);
        }
    }
}