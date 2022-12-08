using LogIdCreate.Core.Cmd.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
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
    internal class FindIncompleteLogMessagesSerilogRewriter : CSharpSyntaxRewriter, IExecuterWalker
    {
        private CreateIdScope scope;

        public FindIncompleteLogMessagesSerilogRewriter()
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
            if (fe != null && (fe.Name.ToString() == "Information" || fe.Name.ToString() == "Warning" || fe.Name.ToString() == "Error")) // To make it faster.
            {
                IInvocationOperation opInvocation = null;
                IDynamicInvocationOperation opDynamicInvocation = null;
                
                string containingType = string.Empty;
                string methodName = string.Empty;
                int argumentsCount = 0;

                var op = scope.Semantic.GetOperation(node);
                if (op is IInvocationOperation)
                {
                    opInvocation = op as IInvocationOperation;
                    containingType = opInvocation.TargetMethod.ContainingType.ToString();
                    methodName = opInvocation.TargetMethod.Name;
                    argumentsCount = opInvocation.Arguments.Count();
                }
                else if (op is IDynamicInvocationOperation)
                {
                    opDynamicInvocation = op as IDynamicInvocationOperation;
                    containingType = ((IDynamicMemberReferenceOperation)opDynamicInvocation.Operation).Instance.Type.ToString();
                    methodName = opDynamicInvocation.Operation.Syntax.TryGetInferredMemberName();
                    argumentsCount = opDynamicInvocation.Arguments.Count();
                }

                if (opInvocation == null && opDynamicInvocation == null)
                {
                    Console.WriteLine("Invalid Node:" + node);
                }
                else
                {
                    if (containingType == "Serilog.ILogger")
                    {
                        int messageTemplateIndex = 0;
                        bool addNew = false;


                        ArgumentSyntax exceptionParamSyntax = null;
                        ArgumentSyntax messageTemplateParamSyntax = null;
                        ArgumentSyntax propertyValueParamSyntax = null;


                        // Check if the first parameter is Exception
                        var firstexpression = scope.Semantic.GetTypeInfo(node.ArgumentList.Arguments[0].Expression);
                        if (firstexpression.Type.ToString().Contains("Exception"))
                        {
                            exceptionParamSyntax = node.ArgumentList.Arguments[0];
                            messageTemplateIndex = 1;
                        }

                        messageTemplateParamSyntax = node.ArgumentList.Arguments[messageTemplateIndex];
                        if (argumentsCount > messageTemplateIndex + 1)
                        {
                            propertyValueParamSyntax = node.ArgumentList.Arguments[messageTemplateIndex + 1];
                        }

                        var messageTemplateExpression = messageTemplateParamSyntax.Expression.ToString();

                        // Calculate Severity parameter value, depending on invoked logging method.
                        var severity = "INF";
                        if (methodName == "Error")
                            severity = "ERR";
                        else if (methodName == "Warning")
                            severity = "WRN";

                        // If propertyValueParamSyntax is not set with severity, then we need to adapt the code and create all required propertyValues parameters.
                        if (propertyValueParamSyntax == null || propertyValueParamSyntax.Expression.ToString().Trim('"') != severity)
                        {
                            var list = node.ArgumentList.Arguments.ToList();

                            // Remove existing paramValues if any. They will be recreated later again as param01 - paramN by using interpolation values from messageTemplate.
                            if (propertyValueParamSyntax != null)
                            {
                                for (var i = list.Count - 1; i > messageTemplateIndex; i--)
                                    list.RemoveAt(i);
                            }

                            // Check if the messageTemplateExpression contains quotes. 
                            if (messageTemplateExpression.IndexOf('"') != -1)
                            {
                                // Generate next Event ID
                                var newId = scope.IdStore.CreateId(scope.LogClassName);

                                // Add parameters for severity, FixableBy and EventId
                                list.Insert(messageTemplateIndex + 1, SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"\"{severity}\"")));
                                list.Insert(messageTemplateIndex + 2, SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"\"ADM\"")));
                                list.Insert(messageTemplateIndex + 3, SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"EventIds.{newId}")));

                                var counter = 1;
                                // First load the value of the messageTemplateParamSyntax to check how many placeholders with variables it contains.
                                foreach (var childNode in messageTemplateParamSyntax.Expression.ChildNodes())
                                {
                                    if (childNode.Kind() == SyntaxKind.Interpolation)
                                    {
                                        // Add existing interpolated parameter from messageTemplate as log method parameter.
                                        list.Insert(counter + messageTemplateIndex + 3, SyntaxFactory.Argument(SyntaxFactory.ParseExpression(childNode.ToString().TrimStart('{').TrimEnd('}'))));
                                        // Now replace the interpolated expression in messageTEmplate with parameter name. 
                                        messageTemplateExpression = messageTemplateExpression.Replace(childNode.ToString(), $"{{Param{counter.ToString("D2")}}}");
                                        counter++;
                                    }
                                }
                                
                                // Remove everything prior and after quotes (this might be $, ()...).
                                messageTemplateExpression = messageTemplateExpression.Substring(messageTemplateExpression.IndexOf('"'));
                                messageTemplateExpression = messageTemplateExpression.Substring(0, messageTemplateExpression.LastIndexOf('"') + 1);
                                // Finally add 3 interpolated parameters to the begin of the messageTemplate variable content.
                                messageTemplateExpression = messageTemplateExpression.Insert(1, "<{Severity}{FixableBy}{EventId}> ");
                                list[messageTemplateIndex] = node.ArgumentList.Arguments[messageTemplateIndex].WithExpression(SyntaxFactory.ParseExpression(messageTemplateExpression));

                                var sargList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(list));

                                var newItem = node.WithArgumentList(sargList.NormalizeWhitespace());

                                scope.Save = true;
                                node = newItem;
                            }
                            else
                            {
                                // If messageTemplateExpression doesn't contain quotes, then it is probably a string variable and this is something we can't deal with. The code needs to be manually adapted.  
                                //messageTemplateExpression = messageTemplateExpression.Insert(1, "[{Severity}{FixableBy}{EventId}] ");
                                var comment = $@"// TODO: Repair this logging line manually
";
                                var newItem = node.WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(comment));

                                scope.Save = true;
                                node = newItem;
                            }


                        }
                    }
                }
                
            }

            return node;
            //  return base.VisitInvocationExpression(node);

            //return null;
        }


    }
}
