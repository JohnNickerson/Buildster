using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Controllers;
using AssimilationSoftware.Buildster.Core.Interfaces;
using AssimilationSoftware.Buildster.Core.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AssimilationSoftware.Buildster.UnitTests;

public class PromotionTests
{
    [Fact]
    public void Promote_Updates_Build_LastPromoted_Date()
    {
        // Check that the promotion action updates the Build.LastPromoted date field.
        // Arrange: Build in Integration
        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        // 2. Pass the connection to DbContextOptions
        var options = new DbContextOptionsBuilder<BuildsContext>()
            .UseSqlite(connection)
            .Options;
        using (var context = new BuildsContext(options))
        {
            context.Database.Migrate();

            context.Add(new Project()
            {
                Name = "TestProject",
                Description = "A dummy project for unit tests",
                ProjectId = 1
            });
            context.Add(new Build()
            {
                BuildId = 1,
                Timestamp = DateTime.Now,
                Version = "0.0.1",
                EnvironmentId = 1, // Integration
                ProjectId = 1
            });
            context.SaveChanges();
        }
        var dummyStatusWriter = new Mock<IStatusWriter>().Object; // Don't care about console output in this case.
        var dummyTableWriter = new Mock<ITableWriter>().Object;
        var controller = new BuildsController(dummyStatusWriter, dummyTableWriter, options);
        var beforePromotionTimestamp = DateTime.Now;

        // Act: Promote to Testing
        controller.Update("TestProject", "Integration", true);

        // Assert: Build.LastPromoted date = Today
        using (var context = new BuildsContext(options))
        {
            var build = context.FindDeployedBuild("TestProject", "Testing");
            Assert.NotNull(build);
            Assert.True(beforePromotionTimestamp < build.LastPromoted, "Last promoted date was not updated");
        }
    }

    [Fact]
    public void New_Build_Sets_LastPromoted_Date()
    {
        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BuildsContext>()
            .UseSqlite(connection)
            .Options;
        using (var context = new BuildsContext(options))
        {
            context.Database.Migrate();

            context.Add(new Project()
            {
                Name = "TestProject",
                Description = "A dummy project for unit tests",
                ProjectId = 1
            });
            context.SaveChanges();
        }

        var dummyStatusWriter = new Mock<IStatusWriter>().Object; // Don't care about console output in this case.
        var dummyTableWriter = new Mock<ITableWriter>().Object;
        var controller = new BuildsController(dummyStatusWriter, dummyTableWriter, options);
        var beforePromotionTimestamp = DateTime.Now;

        // Act
        controller.Add("TestProject", "0.1.0.0", DateTime.Now, "A build to test the initial LastPromoted value", true);

        // Assert
        using (var context = new BuildsContext(options))
        {
              var build = context.FindDeployedBuild("TestProject", "Integration");
            Assert.NotNull(build);
            Assert.True(beforePromotionTimestamp < build.LastPromoted, "Last promoted date was not updated");
          
        }
    }
}