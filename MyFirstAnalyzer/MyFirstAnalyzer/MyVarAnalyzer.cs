using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MyFirstAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MyVarAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PEPR0001";
        internal static readonly LocalizableString Title = "Do not use 'var' keyword.";
        internal static readonly LocalizableString MessageFormat = "Don't use 'var'. Use explicit type '{0}' instead.";
        internal static readonly LocalizableString Desctiption = "The 'var' keyword makes the code less legible.";
        internal const string Category = "MyVarAnalyzer Category";
        

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            Desctiption);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(c => AnalyzeVariableDeclaration(c), SyntaxKind.VariableDeclaration);
        }

        private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declatationStatement = (VariableDeclarationSyntax)context.Node;

            var identifierNode = declatationStatement.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();

            if (identifierNode != null && identifierNode.IsVar == true && declatationStatement.DescendantNodes().OfType<InvocationExpressionSyntax>().Any())
            {
                TypeSyntax variableTypeName = declatationStatement.Type;

                var variableType = context.SemanticModel.GetSymbolInfo(variableTypeName).Symbol as INamedTypeSymbol;

                if (variableType != null)
                {

                    if (variableType.IsAnonymousType == true)
                    {
                        return;
                    }

                    if (variableType.IsGenericType == true && variableType.TypeArguments.Any(t => t.IsAnonymousType == true) == true)
                    {
                        return;
                    }

                    context.ReportDiagnostic(
                        Diagnostic.Create(Rule, declatationStatement.Type.GetLocation(), variableType.ToDisplayString()));
                }
            }
        }
    }
}