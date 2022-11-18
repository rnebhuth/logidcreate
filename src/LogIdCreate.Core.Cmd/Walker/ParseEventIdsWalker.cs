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
   

    /// <summary>
    /// Parses the EventId Classes.
    /// </summary>
    public class ParseEventIdsWalker : CSharpSyntaxWalker
    {
        public EventIdStore EventIds { get; set; } = new EventIdStore();

        private string className = null;
        private SemanticModel eventSemantic;

        public ParseEventIdsWalker(SemanticModel eventSemantic)
        {
            this.eventSemantic = eventSemantic;
        }

        public override void Visit(SyntaxNode? node)
        {
            base.Visit(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if(this.EventIds.FullName == null)
            {
                var ns = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                if(ns != null)
                {
                    this.EventIds.FullName = $"{ns.Name.ToString()}.{node.Identifier.ValueText}";
                }
            }

            className = node.Identifier.ValueText;
            base.VisitClassDeclaration(node);
        }

        public override void VisitXmlComment(XmlCommentSyntax node)
        {
            base.VisitXmlComment(node);
        }

        public override void VisitTrivia(SyntaxTrivia trivia)
        {
            base.VisitTrivia(trivia);
        }



        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (var variable in node.Declaration.Variables)
            {

                var c = eventSemantic.GetDeclaredSymbol(variable) as IFieldSymbol;
                EventIdItem eventId = new EventIdItem
                {
                    Class = className,
                    Name = c.Name,
                    Value = Convert.ToInt32(c.ConstantValue),
                    FullName = c.OriginalDefinition.ToString(),
                    Assembly = c.ContainingAssembly.Name,
                };
                if (variable.Initializer != null &&
                    variable.Initializer.Value != null &&
                    variable.Initializer.Value is BinaryExpressionSyntax)
                {
                    var left = ((BinaryExpressionSyntax)variable.Initializer.Value).Left.ToString();
                    var right = ((BinaryExpressionSyntax)variable.Initializer.Value).Right.ToString();
                    var op = ((BinaryExpressionSyntax)variable.Initializer.Value).OperatorToken.ToString();
                    if (left == "BaseId" && op == "+")
                    {
                        eventId.Relative = Convert.ToInt32(right);
                    }
                    else if (left.EndsWith(".BaseId") && op == "+")
                    {
                        eventId.Relative = Convert.ToInt32(right);
                        eventId.IsBase = true;
                    }
                }
                EventIds.Ids.Add(eventId);
            }
            base.VisitFieldDeclaration(node);
        }
    }
}
