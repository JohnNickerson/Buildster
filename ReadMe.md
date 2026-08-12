# Buildster

Buildster is a command-line interface (CLI) application developed in C# and .NET designed to manage a personal software build pipeline. 

The application is targeted for use in Windows environments, with cross-platform capability on other operating systems dependent on .NET runtime compatibility.

## Dependencies
* **Data Management:** Utilizes Entity Framework Core to interact with an underlying SQLite database.
* **Git Interaction:** LibGit2Sharp.
* **Command Line:** CommandLineParser and Spectre.
* **Unit Tests:** xUnit.

## Version History
- 2026-07-29: Build 0.6.0.0
	- Migrate to Entity Framework.
- 2026-08-12: Build 0.6.1.0
	- Allow manual build date.
	- Tidy up project list.
	- Set company name when setting copyright.
	- Optimise environment paths pivot table construction.
