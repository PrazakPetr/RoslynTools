using System;
using System.Composition;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editing;

namespace MyFirstAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class MyVarCodeFixProvider : CodeFixProvider
    {
        public const string DiagnosticId = "MyVarCodeFixProvider";

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(MyVarAnalyzer.DiagnosticId);
            }
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            var objectCreation = root.FindNode(context.Span)
                         .FirstAncestorOrSelf<VariableDeclarationSyntax>();

            context.RegisterCodeFix(
                CodeAction.Create("Use explicit type.", c => ChangeToExplicitType(objectCreation, context.Document, c)),
                context.Diagnostics[0]);
        }

        private async Task<Document> ChangeToExplicitType(VariableDeclarationSyntax objectCreation, Document document, CancellationToken c)
        {
            TypeSyntax variableTypeName = objectCreation.Type;

            var semanticmode = await document.GetSemanticModelAsync(c).ConfigureAwait(false);
            var variableType = semanticmode.GetSymbolInfo(variableTypeName).Symbol as INamedTypeSymbol;

            SyntaxNode newNode = null;

            if (variableType.IsGenericType == false)
            {
                newNode = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseTypeName(variableType.ToDisplayString()).
                    WithLeadingTrivia(variableTypeName.GetLeadingTrivia()).
                    WithTrailingTrivia(variableTypeName.GetTrailingTrivia());
            }
            else
            {
                newNode = SyntaxGenerator.GetGenerator(document).GenericName(variableType.Name, variableType.TypeArguments);
            }

            var oldRoot = await document.GetSyntaxRootAsync(c);
            var newRoot = oldRoot.ReplaceNode(objectCreation.DescendantNodes().OfType<IdentifierNameSyntax>().First(), newNode);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}