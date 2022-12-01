using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Options
{
    [Verb("init", HelpText = "Init Project.")]
    public class InitOptions
    {
        [Option('s', "solution", Required = false, HelpText = "The name of the solution.")]
        public string? Solution { get; set; }
    }
}
