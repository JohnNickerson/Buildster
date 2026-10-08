using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Interfaces;
using AssimilationSoftware.Buildster.Core.Model;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace AssimilationSoftware.Buildster.CLI.Controllers;

public class PackagesController
{
    private DbContextOptions<BuildsContext> _contextOptions;
    private readonly IStatusWriter _statusWriter;

    public PackagesController(IStatusWriter statusWriter, DbContextOptions<BuildsContext>? dbContextOptions = null)
    {
        _statusWriter = statusWriter;
        if (dbContextOptions == null)
        {
            _contextOptions = new DbContextOptionsBuilder<BuildsContext>()
            .UseSqlite("Data Source=Buildster.sqlite")
            .Options;
        }
        else
        {
            _contextOptions = dbContextOptions;
        }
    }

    public int List(string? projectName = null)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            Table table = new Table();
            table.AddColumns("Project", "Package", "Type", "Source folder", "Deploy folder");
            bool firstRow = true;
            var packageList = context.Packages.Include(p => p.Project).OrderBy(p => p.Project.Name).ThenBy(p => p.SourceFolder);
            if (!string.IsNullOrEmpty(projectName))
            {
                packageList = (IOrderedQueryable<Package>)packageList.Where(p => p.Project.Name.ToLower() == projectName.ToLower());
            }
            foreach (var package in packageList)
            {
                if (!firstRow)
                {
                    table.AddEmptyRow();
                }
                firstRow = false;

                table.AddRow(package.Project.Name, package.PackageId.ToString(), package.IsNuGet ? "NuGet" : "Executable", package.SourceFolder, package.DeployFolder);
            }
            if (packageList.Any())
            {
                AnsiConsole.Write(table);
            }
            else
            {
                _statusWriter.Write("No packages found");
            }
        }
        return 0;
    }

    public int Add(string projectName, string sourceFolder, string deployFolder, bool isNuGet)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var project = context.FindProject(projectName);
            if (project is null)
            {
                _statusWriter.Write($"Could not find project {projectName}");
                return 0;
            }
            var package = new Package()
            {
                DeployFolder = deployFolder,
                SourceFolder = sourceFolder,
                IsNuGet = isNuGet,
                ProjectId = project.ProjectId
            };
            context.Packages.Add(package);
            context.SaveChanges();
            List(projectName);
        }
        return 0;
    }

    internal void Delete(string projectName, int packageId, string? packageSource)
    {
        using (var context = new BuildsContext(_contextOptions))
        {
            var package = string.IsNullOrEmpty(packageSource)
                ? context.Packages.First(p => p.PackageId == packageId)
                : context.FindPackageBySource(projectName, packageSource);
            if (package is null)
            {
                _statusWriter.Write("Package not found");
                return;
            }
            context.Packages.Remove(package);
            _statusWriter.Write($"Removed package {package.SourceFolder} from {projectName}");
            context.SaveChanges();
            List();
        }
    }
}