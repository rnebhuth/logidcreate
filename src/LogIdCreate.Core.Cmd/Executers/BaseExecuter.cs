using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Executers
{
    public class BaseExecuter
    {
        public Microsoft.CodeAnalysis.Solution solution { get; set; }

        public void InitMSBuild()
        {
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var newestVersion = visualStudioInstances.OrderByDescending(a => a.Version).FirstOrDefault();
            if (newestVersion == null)
                throw new Exception("No version found.");
            Console.WriteLine($"Using MSBuild at '{newestVersion.MSBuildPath}' to load projects.");
            MSBuildLocator.RegisterInstance(newestVersion);
        }

        public async Task<MSBuildWorkspace> OpenWorkspaceAsync(string solutionPath)
        {
            var workspace = MSBuildWorkspace.Create(new Dictionary<string, string> { { "Configuration", "Debug_Global" } });

            workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);
            
            

            // Attach progress reporter so we print projects as they are loaded.
            solution = await workspace.OpenSolutionAsync(solutionPath, new ConsoleProgressReporter());
            

            Console.WriteLine($"Finished loading solution '{solutionPath}'");
            

            return workspace;
        }
    }
}
