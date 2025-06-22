using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

public class DynSyntaxReceiver : ISyntaxReceiver {
    public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
        if (syntaxNode is ClassDeclarationSyntax classDecl &&
            classDecl.AttributeLists.Any(a => a.Attributes.Any(attr => attr.Name.ToString().Contains("GenerateDynProperties")))) {
            CandidateClasses.Add(classDecl);
        }
    }
}
