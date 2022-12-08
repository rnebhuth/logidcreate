using LogIdCreate.Core.Cmd.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Walker
{
    public class UpdateEventIds : CSharpSyntaxRewriter
    {
        private EventIdStore eventIds;
        private readonly SemanticModel eventSemantic;

        public List<EventIdItem> TotalIds { get; internal set; }

        public class UpdateEventIdItem
        {
            public string ClassName { get; set; }
            public List<EventIdItem> Ids { get; internal set; }
        }

        public Stack<UpdateEventIdItem> Stack { get; set; } = new Stack<UpdateEventIdItem>();

        public UpdateEventIds(EventIdStore eventIds, Microsoft.CodeAnalysis.SemanticModel eventSemantic)
        {
            this.eventIds = eventIds;
            this.eventSemantic = eventSemantic;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (Stack.Count == 0)
            {
                TotalIds = eventIds.Ids.ToList();
            }
            string className = node.Identifier.ValueText;
            Stack.Push(new UpdateEventIdItem
            {
                ClassName = className,
                Ids = eventIds.Ids.Where(a => a.Class == className).ToList(),
            });
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

            foreach (var item in Stack.Peek().Ids)
            {
                var testDocumentation = SyntaxFactory.ParseLeadingTrivia($@"

/// <summary>
/// Value: {item.Value}
{string.Join(Environment.NewLine, item.Occurrence.Select(a => "/// " + a))}
/// </summary>
");
                var mem = SyntaxFactory.ParseMemberDeclaration($"public const int {item.Name} = BaseId + {item.Relative};" + Environment.NewLine)
                    .NormalizeWhitespace()
                    .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                    .WithAdditionalAnnotations(Formatter.Annotation)
                    .WithLeadingTrivia(testDocumentation); ;
                TotalIds.Remove(item);
                node = node.AddMembers(mem);
            }

            Stack.Pop();

            if (Stack.Count == 0 &&
                TotalIds.Count > 0)
            {
                foreach (var classGroup in TotalIds.GroupBy(a => a.Class))
                {
                    var logClass = SyntaxFactory.ClassDeclaration(classGroup.Key)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

                    foreach (var item in classGroup.Where(a => a.IsBase))
                    {
                        var mem = SyntaxFactory.ParseMemberDeclaration($"public const int BaseId = AssemblyEventIds.BaseId + {item.Relative};" + Environment.NewLine)
                            .NormalizeWhitespace()
                            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                            .WithAdditionalAnnotations(Formatter.Annotation);
                        logClass = logClass.AddMembers(mem);
                    }
                    foreach (var item in classGroup.Where(a => !a.IsBase).OrderBy(a => a.Relative))
                    {
                        var testDocumentation = SyntaxFactory.ParseLeadingTrivia($@"
/// <summary>
/// Value: {item.Value}
{string.Join(Environment.NewLine, item.Occurrence.Select(a => "/// " + a))}
/// </summary>
");
                        var mem = SyntaxFactory.ParseMemberDeclaration($"public const int {item.Name} = BaseId + {item.Relative};" + Environment.NewLine)
                           .NormalizeWhitespace()
                           .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                           .WithAdditionalAnnotations(Formatter.Annotation)
                           .WithLeadingTrivia(testDocumentation);
                        logClass = logClass.AddMembers(mem);
                    }

                    node = node.AddMembers(logClass);


                }
            }

            return node;
        }

        public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (var variable in node.Declaration.Variables)
            {

                var c = eventSemantic.GetDeclaredSymbol(variable) as IFieldSymbol;
                var ev = Stack.Peek().Ids.FirstOrDefault(a => a.Name == c.Name);
                if (ev != null)
                {
                    if(ev.Name != "BaseId")
                    {
                        var testDocumentation = SyntaxFactory.ParseLeadingTrivia($@"

/// <summary>
/// Value: {ev.Value}
{string.Join(Environment.NewLine, ev.Occurrence.Select(a => "/// " + a))}
/// </summary>
");
                        node = node.WithoutLeadingTrivia().NormalizeWhitespace()
                            .WithLeadingTrivia(testDocumentation);
                            
                    }
                    Stack.Peek().Ids.Remove(ev);
                    TotalIds.Remove(ev);
                }
            }
            return base.VisitFieldDeclaration(node);
        }
    }
}
