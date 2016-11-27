using System;
using System.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Simplification;

namespace MyFirstAnalyzer
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class MyExtensionMethodRefactoring : CodeRefactoringProvider
    {
        public async sealed override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var node = root.FindNode(context.Span);

            var entryNode = node.Parent as MemberAccessExpressionSyntax;

            if (entryNode != null && entryNode.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var invocationNode = entryNode.Parent as InvocationExpressionSyntax;

                if (invocationNode != null)
                {
                    var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

                    var variableNode = entryNode.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().Last().Expression;

                    var methodNode = entryNode.Name;

                    var variableObject = model.GetSymbolInfo(variableNode);

                    var methodObject = model.GetSymbolInfo(methodNode);

                    if (variableObject.Symbol != null && methodObject.Symbol == null)
                    {

                        ParameterSyntax[] parameters = ProcessArguments(model, invocationNode.ArgumentList);

                        if (parameters != null)
                        {
                            List<NamespaceDeclarationSyntax> namespaces = entryNode.Ancestors().OfType<NamespaceDeclarationSyntax>().ToList();
                            string className = GetSymbolType(variableObject.Symbol).Name.ToUpperLead() + "Extension";
                            string methodName = methodNode.Identifier.Text;
                            string mainArgumentName = ((IdentifierNameSyntax)variableNode).Identifier.Text;
                            string mainArgumentType = GetSymbolType(variableObject.Symbol).ToDisplayString();



                            var action = CodeAction.Create("Create a fluent API extension method",
                                c => CreateExtensionMethodAsync(context.Document, namespaces, className, methodName, mainArgumentName, mainArgumentType, parameters, c));

                            context.RegisterRefactoring(action);
                        }
                    }
                }
            }
        }

        private async Task<Document> CreateExtensionMethodAsync(Document document, List<NamespaceDeclarationSyntax> namespaces, string className, string methodName, string mainArgumentName, string mainArgumentType, ParameterSyntax[] parameters, CancellationToken c)
        {
            var namespaceDeclartion  = namespaces.FirstOrDefault();

            string namespaceName = namespaceDeclartion.Name.WithoutTrivia().ToFullString();

            var newMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(mainArgumentType), methodName)
                                             .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                                             .AddParameterListParameters(
                                                SyntaxFactory.Parameter(SyntaxFactory.Identifier(mainArgumentName))
                                                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.ThisKeyword))
                                                    .WithType(SyntaxFactory.ParseTypeName(mainArgumentType)))
                                             .AddParameterListParameters(parameters)
                                             .WithBody(SyntaxFactory.Block()
                                                .AddStatements(SyntaxFactory.ThrowStatement(SyntaxFactory.ParseExpression("new System.NotImplementedException()").WithAdditionalAnnotations(Simplifier.Annotation)))
                                             );

            var compilation = await document.Project.GetCompilationAsync(c).ConfigureAwait(false);

            var availableClasses = compilation
                .SyntaxTrees
                .Select(st => compilation.GetSemanticModel(st))
                .SelectMany(sm => sm.SyntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>());

            var myClass = availableClasses
                .FirstOrDefault(cl => cl.Identifier.Text == className);

            if (myClass != null)
            {

                var myDocuement = document.Project.GetDocument(myClass.SyntaxTree);
                var oldRoot = await myDocuement.GetSyntaxRootAsync(c);
                var newRoot = oldRoot.ReplaceNode(myClass, myClass.AddMembers(newMethod));

                return myDocuement.WithSyntaxRoot(newRoot);
            }
            else
            {
                var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddMembers(
                    SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(namespaceName))
                        .AddMembers(
                            SyntaxFactory.ClassDeclaration(className)
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                                .AddMembers(newMethod)
                        )
                 );

                return document.Project.AddDocument(className + ".cs", compilationUnit);
            }
        }

        private static ParameterSyntax[] ProcessArguments(SemanticModel model, ArgumentListSyntax list)
        {
            List<ParameterSyntax> result = new List<ParameterSyntax>();

            foreach (var item in list.Arguments)
            {
                ITypeSymbol type = GetSymbolType(model.GetSymbolInfo(item.Expression).Symbol);

                if (type != null)
                {
                    result.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(((IdentifierNameSyntax)item.Expression).Identifier.Text))
                                                .WithType(SyntaxFactory.ParseTypeName(type.ToDisplayString()))
                    );
                }
                else
                {
                    return null;
                }

            }

            return result.ToArray();
        }

        private static ITypeSymbol GetSymbolType(ISymbol symbol)
        {
            ITypeSymbol type = null;

            if (symbol is ILocalSymbol)
            {
                type = (symbol as ILocalSymbol).Type;
            }
            else if (symbol is IFieldSymbol)
            {
                type = (symbol as IFieldSymbol).Type;
            }

            return type;
        }
    }
}