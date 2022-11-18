using LogIdCreate.Core.Cmd.Entities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Walker
{
    public class FindLogClassName : CSharpSyntaxWalker, IExecuterWalker
    {
        private CreateIdScope scope;

        public void Run(CreateIdScope scope)
        {
            this.scope = scope;
            Visit(scope.Syntax);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (scope.LogClassName == null)
                scope.LogClassName = node.Identifier.ValueText;
            else
                scope.AddWarning("Multiple Classes", skip: true);

            base.VisitClassDeclaration(node);
        }
    }
}
