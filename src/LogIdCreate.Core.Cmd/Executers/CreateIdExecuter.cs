using LogIdCreate.Core.Cmd.Entities;
using LogIdCreate.Core.Cmd.Options;
using LogIdCreate.Core.Cmd.Walker;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Executers
{
    public class CreateIdExecuter : BaseExecuter
    {
        private readonly LogIdConfig config;
        private static Compilation? compilation;

        public CreateIdExecuter(LogIdConfig config)
        {
            this.config = config;
        }

        public async Task Run(CreateIdsOptions o)
        {
            InitMSBuild();

            using (var workspace = await OpenWorkspaceAsync(o.Solution))
            {
                for (int i = 0; i < solution.Projects.Count(); i++)
                {
                    var project = solution.Projects.ToList()[i];
                    if (string.IsNullOrEmpty(o.Project) ||
                        o.Project.Split(',').Any(a=>a == project.Name))
                    {
                        compilation = await project.GetCompilationAsync();
                        var errors = compilation.GetDiagnostics().Where(a => a.Severity == DiagnosticSeverity.Error).ToList();
                        await RunProjectAsync(project);
                    }
                }

                workspace.TryApplyChanges(solution);
            }
        }

        private async Task RunProjectAsync(Microsoft.CodeAnalysis.Project project)
        {
            var eventIds = await ParseEventIdsAsync(project);
            if (eventIds == null)
                return;

            foreach (var document in project.Documents)
            {
                if (document.Name.Contains("AssemblyEventId"))
                    continue;

                var scope = new CreateIdScope(eventIds);
                scope.Syntax = await document.GetSyntaxRootAsync();
                scope.Semantic = compilation.GetSemanticModel(scope.Syntax.SyntaxTree);
                    //await document.GetSemanticModelAsync();
                scope.Solution = solution;
                scope.Document = document;

                if (document.Name == "WeatherForecastController.cs" || true)
                {
                    List<Type> types = new List<Type>()
                    {
                        typeof(FindLogClassName),
                        typeof(AddUsingRewriter),
                        typeof(FindIncompleteLogMessagesRewriter),
                        typeof(FindAllEventIdsWalker)
                    };

                    foreach (var type in types)
                    {
                        var ew = Activator.CreateInstance(type) as IExecuterWalker;
                        if (ew == null)
                            throw new Exception("Not a IExecuterWalker");
                        ew.Run(scope);

                        if (scope.IsSkipped)
                            break;

                        if (scope.IsError)
                            throw new Exception("We have an error");

                    }

                    if (scope.Save)
                        solution = scope.Solution;

                }
            }

            solution = await UpdateEventIdsAsync(project, eventIds);
        }



        private async Task<EventIdStore?> ParseEventIdsAsync(Microsoft.CodeAnalysis.Project project)
        {
            var eventDoc = project.Documents.FirstOrDefault(a => a.Name == "AssemblyEventId.cs");
            if (eventDoc == null)
            {
                Console.WriteLine("Could not find file EventIds.cs");
                return null;
            }

            var eventSyntax = await eventDoc.GetSyntaxRootAsync();
            var eventSemantic = await eventDoc.GetSemanticModelAsync();

            var parser = new ParseEventIdsWalker(eventSemantic);
            parser.Visit(eventSyntax);

            return parser.EventIds;
        }

        private async Task<Solution> UpdateEventIdsAsync(Project project, EventIdStore eventIds)
        {
            var eventDoc = project.Documents.FirstOrDefault(a => a.Name == "AssemblyEventId.cs");
            if (eventDoc == null)
            {
                Console.WriteLine("Could not find file EventIds.cs");
                return null;
            }

            var eventSyntax = await eventDoc.GetSyntaxRootAsync();
            var eventSemantic = await eventDoc.GetSemanticModelAsync();

            var parser = new UpdateEventIds(eventIds, eventSemantic);
            var newSyntax = parser.Visit(eventSyntax);

            solution = solution.WithDocumentSyntaxRoot(eventDoc.Id, newSyntax);

            var newDoc = solution.GetDocument(eventDoc.Id);
            newDoc = await Formatter.FormatAsync(newDoc);
            solution = newDoc.Project.Solution;

            return solution;

        }


    }
}
