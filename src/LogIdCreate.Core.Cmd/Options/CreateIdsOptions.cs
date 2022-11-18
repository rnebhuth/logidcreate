using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Options
{
    [Verb("createid", HelpText = "Init Project.")]
    public class CreateIdsOptions
    {
        [Option('s', "solution", Required = false, HelpText = "The name of the solution.")]
        public string? Solution { get; set; }

        [Option('p', "project", Required = false, HelpText = "The filter of a specific project(s).")]
        public string? Project { get; set; }
    }
}
