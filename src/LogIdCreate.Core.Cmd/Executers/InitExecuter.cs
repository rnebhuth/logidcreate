using LogIdCreate.Core.Cmd.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Executers
{
    public class InitExecuter : BaseExecuter
    {
        private readonly LogIdConfig config;

        public InitExecuter(LogIdConfig config)
        {
            this.config = config;
        }
        public async Task Run(InitOptions o)
        {
            InitMSBuild();

            using (var workspace = await OpenWorkspaceAsync(o.Solution))
            {
                for (int i = 0; i < solution.Projects.Count(); i++)
                {
                    var project = solution.Projects.ToList()[i];
                        await RunProjectAsync(project);
                }

                workspace.TryApplyChanges(solution);
            }
        }

        private async Task RunProjectAsync(Microsoft.CodeAnalysis.Project project)
        {
            var eventDoc = project.Documents.FirstOrDefault(a => a.Name == config.AssemblyEventId.FileName);
            if (eventDoc == null)
            {
                var source = await File.ReadAllTextAsync(config.AssemblyEventId.Template);
                source = source.Replace("{{NAMESPACE}}", project.DefaultNamespace);
                // This is not working because this adds something to the project
                // https://github.com/dotnet/roslyn/issues/36781
                //var doc = project.AddDocument(config.AssemblyEventId.FileName, source);
                //solution = doc.Project.Solution;

                var newFile = Path.Combine(Path.GetDirectoryName(project.FilePath), config.AssemblyEventId.FileName);
                await File.WriteAllTextAsync(newFile, source);

            }
        }
    }
}
