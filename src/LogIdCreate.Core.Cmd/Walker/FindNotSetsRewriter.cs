using LogIdCreate.Core.Cmd.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Walker
{
    public class FindNotSetsRewriter : CSharpSyntaxRewriter
    {
        private SemanticModel semantic;
        private string className = null;
        private string functionName = null;
        private List<EventIdItem> eventIds;

        public FindNotSetsRewriter(SemanticModel semantic)
        {
            this.semantic = semantic;
        }

        public FindNotSetsRewriter(SemanticModel semantic, List<EventIdItem> eventIds) : this(semantic)
        {
            this.eventIds = eventIds;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            className = node.Identifier.ValueText;
            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            functionName = node.Identifier.ValueText;
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.ToString().Contains("NOTSET"))
            {
                
                var list = eventIds.Where(a => a.Class == className).ToList();
                var ba = list.FirstOrDefault(a => a.Name == "BaseId");
                if (ba == null)
                {
                    var eBase = eventIds.FirstOrDefault(a => a.Name == "BaseId" && a.Class == "EventIds");
                    var max = eventIds.Where(a => a.Name == "BaseId" && a.Relative.HasValue).Select(a => a.Relative).Max() ?? 0;
                    ba = new EventIdItem
                    {
                        IsNew = true,
                        Class = className,
                        Name = "BaseId",
                        Relative = max + 100,
                        Value = eBase.Value + max + 100,
                    };
                    eventIds.Add(ba);
                }
                for (int i = 1; i < 1000; i++)
                {
                    if (list.FirstOrDefault(a => a.Relative == i) == null)
                    {
                        var ev = new EventIdItem
                        {
                            IsNew = true,
                            Class = className,
                            Name = "Id_" + i,
                            Relative = i,
                            Value = ba.Value + i
                        };
                        eventIds.Add(ev);
                        return SyntaxFactory.ParseExpression($"EventIds.{className}.{ev.Name}");
                    }
                }
            }
            return base.VisitMemberAccessExpression(node);
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            return base.VisitIdentifierName(node);
        }
    }
}
