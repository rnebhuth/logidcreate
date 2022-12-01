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
    public class FindAllEventIdsWalker : CSharpSyntaxWalker
    {
        private Microsoft.CodeAnalysis.SemanticModel? semantic;
        private List<EventIdItem> eventIds;

        public FindAllEventIdsWalker(Microsoft.CodeAnalysis.SemanticModel? semantic)
        {
            this.semantic = semantic;
        }

        public FindAllEventIdsWalker(Microsoft.CodeAnalysis.SemanticModel? semantic, List<EventIdItem> eventIds) : this(semantic)
        {
            this.eventIds = eventIds;
        }

        public override void VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (node.ToFullString().Contains("Get121"))
            {
                var sym = semantic.GetSymbolInfo(node);

            }
            base.VisitQualifiedName(node);
        }


        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            var sym = semantic.GetSymbolInfo(node);
            if (sym.Symbol != null)
            {
                var eventId = eventIds.FirstOrDefault(a => a.FullName == sym.Symbol.OriginalDefinition.ToString() &&
                                                    a.Assembly == sym.Symbol.ContainingAssembly.Name);

                if (eventId != null)
                {
                    var line = node.GetLocation().GetLineSpan();

                    var text = node.SyntaxTree.GetText().Lines[line.StartLinePosition.Line].ToString();

                    eventId.LineText = text.ToString().Trim(); ;
                    eventId.PosText = $"{line.Path}:{line.StartLinePosition.Line + 1}:{line.StartLinePosition.Character + 1}:";
                        //.First(n => lineSpan.Contains(n.Span));
                    //Console.WriteLine($"{text.ToString()}       {line.Path} {line.StartLinePosition.Line+1}:{line.StartLinePosition.Character+1}:");
                }
            }

            /*            if (node.ToFullString().StartsWith("EventIds."))
                        {
                            var line = node.GetLocation().GetLineSpan();
                            Console.WriteLine($"{node.ToString()}-{line.Path} {line.StartLinePosition.Line}:{line.StartLinePosition.Character}:");
                        }*/
        }
    }
}
