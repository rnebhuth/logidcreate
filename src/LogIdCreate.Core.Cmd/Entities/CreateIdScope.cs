using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogIdCreate.Core.Cmd.Entities
{
    public class CreateIdScope
    {
        public Solution Solution { get; internal set; }
        public Document Document { get; internal set; }

        internal SemanticModel? Semantic { get; set; }

        public SyntaxNode? Syntax { get; internal set; }

        public EventIdStore IdStore { get; set; }
      
        public string LogClassName { get; internal set; }
        public bool IsSkipped { get; internal set; }
        public bool IsError { get; internal set; }
        public bool Save { get; internal set; }

        public CreateIdScope(EventIdStore eventIdStore)
        {
            IdStore = eventIdStore;
        }

        internal void UpdateSyntax(SyntaxNode? newSyntax)
        {
            Solution = Solution.WithDocumentSyntaxRoot(Document.Id, newSyntax);
            Document = Solution.GetDocument(Document.Id);
            Syntax = Document.GetSyntaxRootAsync().Result;
            Semantic= Document.GetSemanticModelAsync().Result;

        }

        internal void AddWarning(string message, bool skip)
        {

        }
    }
}
