using AssimilationSoftware.Buildster.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Environment = AssimilationSoftware.Buildster.Core.Model.Environment;

namespace AssimilationSoftware.Buildster.Core;

public class BuildsContext : DbContext
{
    public DbSet<Build> Builds { get; set; }
    public DbSet<Environment> Environments { get; set; }
    public DbSet<EnvironmentPath> EnvironmentPaths { get; set; }
    public DbSet<Machine> Machines { get; set; }
    public DbSet<Package> Packages { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectPath> ProjectPaths { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=Buildster.sqlite");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seeding the static Environment reference data
        modelBuilder.Entity<Environment>().HasData(
            new Environment { EnvironmentId = 1, Name = "Integration" },
            new Environment { EnvironmentId = 2, Name = "Testing" },
            new Environment { EnvironmentId = 3, Name = "Production" }
        );
    }

    public Environment? FindEnvironment(string environmentName)
    {
        var result = Environments.FirstOrDefault(e => e.Name.ToLower() == environmentName.ToLower());
        return result;
    }

    public Project? FindProject(string projectName)
    {
        return Projects.FirstOrDefault(p => p.Name.ToLower() == projectName.ToLower());
    }

    public Machine? FindMachine(string machineName)
    {
        return Machines.FirstOrDefault(m => m.Name.ToLower() == machineName.ToLower());
    }

    public void UpdateProjectPath(Project project, Machine machine, string updatedSourcePath)
    {
        var projectPath = ProjectPaths.FirstOrDefault(pp => pp.ProjectId == project.ProjectId && pp.MachineId == machine.MachineId);
        if (projectPath is null)
        {
            // Insert
            ProjectPaths.Add(new ProjectPath()
            {
                Path = updatedSourcePath,
                Machine = machine,
                Project = project
            });
        }
        else
        {
            // Update
            projectPath.Path = updatedSourcePath;
        }
    }

    public ProjectPath? FindProjectPath(Project proj, string machineName)
    {
        return ProjectPaths.FirstOrDefault(pp => pp.ProjectId == proj.ProjectId && pp.Machine.Name.ToLower() == machineName.ToLower());
    }

    public Build? FindDeployedBuild(string projectName, string environment)
    {
        var project = FindProject(projectName);
        var env = FindEnvironment(environment);
        if (project == null || env == null)
        {
            return null;
        }
        return Builds.FirstOrDefault(b => b.ProjectId == project.ProjectId && b.EnvironmentId == env.EnvironmentId);
    }
}