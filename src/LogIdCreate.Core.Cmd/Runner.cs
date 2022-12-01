using LogIdCreate.Core.Cmd.Entities;
using LogIdCreate.Core.Cmd.Walker;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd
{
    internal class Runner
    {
        private Microsoft.CodeAnalysis.Solution? solution;

        internal async Task RunAsync()
        {
            InitMSBuild();

            using (var workspace = await OpenWorkspaceAsync(@"C:\TFS\Privat\LogCreate\WebApplication1\WebApplication1.sln"))
            {
                foreach (var project in solution!.Projects)
                {
                    await RunProjectAsync(project);
                }

                workspace.TryApplyChanges(solution);
            }

        }

        private async Task RunProjectAsync(Microsoft.CodeAnalysis.Project project)
        {
            var eventDoc = project.Documents.FirstOrDefault(a => a.Name == "EventIds.cs");
            if (eventDoc == null)
            {
                var doc = project.AddDocument("EventIds.cs", "test");
                Console.WriteLine("Could not find file EventIds.cs");
                return;
            }
            var eventIds = await ParseEventIdFileAsync(eventDoc);

            foreach (var document in project.Documents)
            {
                var syntax = await document.GetSyntaxRootAsync();
                var semantic = await document.GetSemanticModelAsync();

                FindIncompleteLogMessagesRewriter b = new FindIncompleteLogMessagesRewriter(semantic);
                var newDocSytax = b.Visit(syntax);

                FindNotSetsRewriter s = new FindNotSetsRewriter(semantic, eventIds);
                newDocSytax = s.Visit(newDocSytax);

                solution = solution.WithDocumentSyntaxRoot(document.Id, newDocSytax);
            }

            foreach (var document in project.Documents)
            {
                var eventSyntax = await document.GetSyntaxRootAsync();
                var semantic = await document.GetSemanticModelAsync();
                var finder = new FindAllEventIdsWalker(semantic, eventIds);
                finder.Visit(eventSyntax);
            }




            await UpdateEventIdAsync(eventDoc, eventIds);
        }

        private async Task UpdateEventIdAsync(Document eventDoc, List<EventIdItem> eventIds)
        {
            var eventSyntax = await eventDoc.GetSyntaxRootAsync();
            var editor = await DocumentEditor.CreateAsync(eventDoc);
            foreach (var item in eventIds.GroupBy(a => a.Class))
            {
                var classDef = eventSyntax.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(a => a.Identifier.Text == item.Key);
                if (classDef == null)
                {
                    var classDefEv = eventSyntax.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(a => a.Identifier.Text == "EventIds");
                    classDef = SyntaxFactory.ClassDeclaration(item.Key);
                    classDef = classDef.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                    editor.InsertAfter(classDefEv.ChildNodes().Last(), classDef);


                    var b = item.FirstOrDefault(a => a.Name == "BaseId");
                    var mem = SyntaxFactory.ParseMemberDeclaration($"public const int BaseId = EventIds.BaseId + {b.Relative};" + Environment.NewLine);
                    editor.AddMember(classDef, mem);
                }
                if (classDef != null)
                {
                    foreach (var subItem in item.Where(a => a.Name != "BaseId"))
                    {
                        if (subItem.IsNew)
                        {
                            var testDocumentation = SyntaxFactory.ParseLeadingTrivia(@"
/// <summary>
/// This class provides extension methods for the class1.
/// </summary>
");
                            var mem = SyntaxFactory.ParseMemberDeclaration($"public const int {subItem.Name} = BaseId + {subItem.Relative};" + Environment.NewLine);
                            //mem = mem.WithAdditionalAnnotations(comment);
                            //mem = mem.WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Trivia(testDocumentation)));
                            mem = mem.WithLeadingTrivia(testDocumentation);
                            editor.AddMember(classDef, mem);
                        }
                        else
                        {
                            var propDev = classDef.DescendantNodes().OfType<FieldDeclarationSyntax>().Where(a => a.ToString().Contains(subItem.Name)).FirstOrDefault();
                            if(propDev != null)
                            {
                                var testDocumentation = SyntaxFactory.ParseLeadingTrivia(@"
/// <summary>
/// This class provides extension methods for the class1.
/// </summary>
");
                                var newProp = propDev.WithoutTrivia().WithLeadingTrivia(testDocumentation);
                                editor.ReplaceNode(propDev, newProp);
                            }
                        }
                    }
                }
            }
            var newRoot = Formatter.Format(editor.GetChangedRoot(), solution.Workspace);
            solution = solution.WithDocumentSyntaxRoot(eventDoc.Id, newRoot);
        }

        private async Task<List<EventIdItem>> ParseEventIdFileAsync(Microsoft.CodeAnalysis.Document eventDoc)
        {
            var eventSyntax = await eventDoc.GetSyntaxRootAsync();
            var eventSemantic = await eventDoc.GetSemanticModelAsync();

            var parser = new ParseEventIdsWalker(eventSemantic);
            parser.Visit(eventSyntax);

            return parser.EventIds;
        }

        private void InitMSBuild()
        {
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var newestVersion = visualStudioInstances.OrderByDescending(a => a.Version).FirstOrDefault();
            if (newestVersion == null)
                throw new Exception("No version found.");
            Console.WriteLine($"Using MSBuild at '{newestVersion.MSBuildPath}' to load projects.");
            MSBuildLocator.RegisterInstance(newestVersion);
        }

        private async Task<MSBuildWorkspace> OpenWorkspaceAsync(string solutionPath)
        {
            var workspace = MSBuildWorkspace.Create();

            workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);

            // Attach progress reporter so we print projects as they are loaded.
            solution = await workspace.OpenSolutionAsync(solutionPath, new ConsoleProgressReporter());

            Console.WriteLine($"Finished loading solution '{solutionPath}'");

            return workspace;
        }
    }
}
