using AssimilationSoftware.Buildster.CLI.Options;
using AssimilationSoftware.Buildster.Core;
using AssimilationSoftware.Buildster.Core.Model;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace AssimilationSoftware.Buildster.CLI.Controllers;

public class PackagesController
{
    public int List(ListPackagesOptions? opts = null)
    {
        using (var context = new BuildsContext())
        {
            Table table = new Table();
            table.AddColumns("Project", "Package", "Type", "Source folder", "Deploy folder");
            bool firstRow = true;
            var packageList = context.Packages.Include(p => p.Project).OrderBy(p => p.Project.Name).ThenBy(p => p.SourceFolder);
            if (opts is not null && !string.IsNullOrEmpty(opts.ProjectName))
            {
                packageList = (IOrderedQueryable<Package>)packageList.Where(p => p.Project.Name.ToLower() == opts.ProjectName.ToLower());
            }
            foreach (var package in packageList)
            {
                if (!firstRow)
                {
                    table.AddEmptyRow();
                }
                firstRow = false;

                table.AddRow(package.Project.Name, package.PackageId.ToString(), package.IsNuGet ? "NuGet" : "Library", package.SourceFolder, package.DeployFolder);
            }
            if (packageList.Any())
            {
                AnsiConsole.Write(table);
            }
            else
            {
                Console.WriteLine("No packages found");
            }
        }
        return 0;
    }

    public int Add(AddPackageOptions opts)
    {
        using (var context = new BuildsContext())
        {
            var project = context.FindProject(opts.ProjectName);
            if (project is null)
            {
                Console.WriteLine($"Could not find project {opts.ProjectName}");
                return 0;
            }
            var package = new Package()
            {
                DeployFolder = opts.DeployFolder,
                SourceFolder = opts.SourceFolder,
                IsNuGet = opts.IsNuGet,
                ProjectId = project.ProjectId
            };
            context.Packages.Add(package);
            context.SaveChanges();
            List(new ListPackagesOptions() { ProjectName = opts.ProjectName });
        }
        return 0;
    }
}