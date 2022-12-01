using LogIdCreate.Core.Cmd.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Walker
{
    /// <summary>
    /// Finds all "Log" Messages which have not an EventId yet and set it to EventIds.NOTSET
    /// </summary>
    internal class FindIncompleteLogMessagesRewriter : CSharpSyntaxRewriter, IExecuterWalker
    {
        private CreateIdScope scope;

        public FindIncompleteLogMessagesRewriter()
        {
        }

        public void Run(CreateIdScope scope)
        {
            this.scope = scope;
            this.scope.UpdateSyntax(this.Visit(scope.Syntax));
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var fe = node.Expression as MemberAccessExpressionSyntax;
            if (fe != null && fe.Name.ToString().StartsWith("Log")) // To make it faster.
            {
                var op = scope.Semantic.GetOperation(node) as IInvocationOperation;
                if (op == null)
                {
                    Console.WriteLine("Invalid Node:" + node);
                }
                else if (op.TargetMethod.ContainingType.ToString() == "Microsoft.Extensions.Logging.LoggerExtensions")
                {
                    int? index = null;
                    bool addNew = false;

                    // It its starts with log.
                    if (op.TargetMethod.Name == "Log")
                    {
                        // Check if the method is an extension or not,to get the right position.
                        var firstexpression = scope.Semantic.GetTypeInfo(node.ArgumentList.Arguments[0].Expression);
                        index = firstexpression.Type.ToString().Contains("ILogger") ? 2 : 1;
                        addNew = op.Arguments[2].Parameter.Name != "eventId";
                    }
                    else
                    {
                        // Check if the method is an extension or not,to get the right position.
                        var firstexpression = scope.Semantic.GetTypeInfo(node.ArgumentList.Arguments[0].Expression);
                        index = firstexpression.Type.ToString().Contains("ILogger") ? 1 : 0;
                        addNew = op.Arguments[1].Parameter.Name != "eventId";

                    }
                    


                    if (addNew)
                    {
                        var newId = scope.IdStore.CreateId(scope.LogClassName);
                        var list = node.ArgumentList.Arguments.ToList();
                        list.Insert(index.Value, SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"EventIds.{newId}")));

                        var sargList = SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(list));
                        var newItem = node.WithArgumentList(sargList);
                        scope.Save = true;
                        node = newItem;
                    }
                }
                
            }


            return node;
            //  return base.VisitInvocationExpression(node);

            //return null;
        }


    }
}
