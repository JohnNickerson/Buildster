using System.IO.Abstractions.TestingHelpers;
using AssimilationSoftware.Buildster.Core.Interfaces;
using Moq;

namespace AssimilationSoftware.Buildster.UnitTests;

public class CompanyNameTests
{
    [Fact]
    public void Set_Copyright_Should_Add_Company_Attribute_If_Missing()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var projectName = "TestProject";
        var companyName = "TestCompany";
        var baseDirectory = $@"c:\temp\{projectName}";
        var filePath = Path.Combine(baseDirectory, "Buildster.csproj");
        var mockStatusWriter = new Mock<IStatusWriter>();
        // Simulate a project file without a <Company> attribute
        var projectFileContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
                <TargetFramework>net6.0</TargetFramework>
    </PropertyGroup>
</Project>";
        mockFileSystem.AddDirectory(baseDirectory);
        mockFileSystem.AddFile(filePath, new MockFileData(projectFileContent));

        // Act
        // ProjectsController.SetCopyright(projectName, companyName, "2026");
        Core.Utils.VersionInfo.UpdateCopyright(baseDirectory, companyName, DateTime.Now.Year, mockStatusWriter.Object, mockFileSystem);

        // Assert
        var updatedFileContent = mockFileSystem.File.ReadAllText(filePath);
        Assert.Contains($"<Company>{companyName}</Company>", updatedFileContent);
    }
}
