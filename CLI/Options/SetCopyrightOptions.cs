using AssimilationSoftware.Buildster.Core.Model;
using CommandLine;
using System;

namespace AssimilationSoftware.Buildster.CLI.Options
{
    [Verb("set-copyright", HelpText = "Updates the company and year info for the copyright notice.")]
    public class SetCopyrightOptions
    {
        [Option('p', "project", HelpText = "The name of the project to search for", Required = true)]
        public string ProjectName { get; set; }

        [Option("company", HelpText = "The company name to set in copyright information")]
        public string CompanyName { get; set; }

        [Option("year", HelpText = "The copyright year to set in copyright information")]
        public string YearString { get; set; }

        public int Year => int.TryParse(YearString, out var intYear) ? intYear : DateTime.Now.Year;
    }
}
