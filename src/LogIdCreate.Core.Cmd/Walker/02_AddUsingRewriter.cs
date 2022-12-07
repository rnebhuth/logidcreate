using LogIdCreate.Core.Cmd.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Walker
{
    public class AddUsingRewriter : CSharpSyntaxRewriter, IExecuterWalker
    {
        private CreateIdScope scope;


        public void Run(CreateIdScope scope)
        {
            this.scope = scope;
            this.scope.UpdateSyntax(this.Visit(scope.Syntax));
        }



        [return: NotNullIfNotNull("node")]
        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            return base.Visit(node);
        }

        public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node)
        {
            node = (CompilationUnitSyntax)base.VisitCompilationUnit(node);
            var us = SyntaxFactory.UsingDirective(
                SyntaxFactory.NameEquals("EventIds"),
                SyntaxFactory.ParseName($"{scope.IdStore.FullName}.{scope.LogClassName}"));
            us = us.NormalizeWhitespace();
            if (!node.Usings.Any(u => u.NormalizeWhitespace().GetText().ToString() == us.GetText().ToString()))
                node = node.AddUsings(us);
            
            return node;
        }




    }
}
