using LogIdCreate.Core.Cmd.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Walker
{
    public class FindAllEventIdsWalker : CSharpSyntaxWalker, IExecuterWalker
    {
        private CreateIdScope scope;


        public void Run(CreateIdScope scope)
        {
            this.scope = scope;
            Visit(scope.Syntax);
        }



        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.ToFullString().Contains("EventIds"))
            {
                var parentStatement = node.FirstAncestorOrSelf<StatementSyntax>();
                if (parentStatement != null)
                {
                    var occurrence = String.Format("{0}",
                                          parentStatement.WithoutLeadingTrivia().WithoutTrailingTrivia().WithoutAnnotations().ToFullString());
                    scope.IdStore.AddOccurence(scope.LogClassName, node.ToFullString(), occurrence);
                }
                else
                {
                    var attributeListSyntax = node.FirstAncestorOrSelf<AttributeListSyntax>();
                    if (attributeListSyntax != null)
                    {
                        var occurrence = String.Format("{0}",
                                        attributeListSyntax.WithoutLeadingTrivia().WithoutTrailingTrivia().WithoutAnnotations().ToFullString());
                        scope.IdStore.AddOccurence(scope.LogClassName, node.ToFullString(), occurrence);
                    }
                    else
                        Console.WriteLine(node.ToFullString());


                }


            }




            base.VisitMemberAccessExpression(node);



        }
    }
}
