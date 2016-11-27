using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Data;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Simplification;

namespace StandAloneCodeAnalysisTool
{
    class Program
    {
        static void Main(string[] args)
        {
            // Syntax01();

            //  SyntaxWalker();

            // Compilation01();

            Refactoring();
        }

        private static void Refactoring()
        {

            SyntaxTree tree = CSharpSyntaxTree.ParseText(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HelloWorld
{
    class Program
    {
private int b;
        private static void Main(string[] args)
        {
            var o = new List<int>();
        
            int i = 1;

            int a = o.ToCount(i, b).ToNull();

        }
    }
}");

            var root = (CompilationUnitSyntax)tree.GetRoot();

            var declaration1 = root.DescendantNodes().OfType<InvocationExpressionSyntax>().First();


            var compilation = CSharpCompilation.Create("HelloWorld")
                                               .AddReferences(
                                                    MetadataReference.CreateFromFile(
                                                        typeof(object).Assembly.Location))
                                               .AddReferences(
                                                       MetadataReference.CreateFromFile(
                                                        typeof(Enumerable).Assembly.Location))
                                               .AddSyntaxTrees(tree);



            SemanticModel model = compilation.GetSemanticModel(tree);

            var expression = declaration1.Expression as MemberAccessExpressionSyntax;

            var expression2 = declaration1.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().LastOrDefault().Expression as MemberAccessExpressionSyntax;

            var expressionObject = model.GetSymbolInfo(expression2.Expression);

            var name = model.GetSymbolInfo(expression.Name);

            if (expressionObject.Symbol != null && name.Symbol == null)
            {
                var argList = ProcessArgs(model, declaration1.ArgumentList);
                CompilationUnitSyntax comp = GetCompilationUnit(declaration1, model, expression, expressionObject);

                string rest = Formatter.Format(comp, new AdhocWorkspace()).ToFullString();
            }
        }

        private static CompilationUnitSyntax GetCompilationUnit(InvocationExpressionSyntax declaration1, SemanticModel model, MemberAccessExpressionSyntax expression, SymbolInfo expressionObject)
        {
            var aa = (expressionObject.Symbol as ILocalSymbol);


            char[] a = aa.Type.Name.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            string className = new string(a) + "Extension";

            var ns = declaration1.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();

            string namespaceName = "Hello";

            if (ns != null)
            {
                namespaceName = ns.Name.WithoutTrivia().ToFullString();
            }

            var comp = SyntaxFactory.CompilationUnit()
            .AddMembers(
                 SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(namespaceName))
                         .AddMembers(
                         SyntaxFactory.ClassDeclaration(className)
                         .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                             .AddMembers(
                                 SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), expression.Name.Identifier.Text)
                                         .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                                         .AddParameterListParameters(
                                            SyntaxFactory.Parameter(SyntaxFactory.Identifier(((IdentifierNameSyntax)expression.Expression).Identifier.Text))
                                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.ThisKeyword))
                                                .WithType(SyntaxFactory.ParseTypeName(aa.Type.ToDisplayString())))
                                         .AddParameterListParameters(ProcessArgs(model, declaration1.ArgumentList))
                                         .WithBody(SyntaxFactory.Block()
                                            .AddStatements(SyntaxFactory.ThrowStatement(SyntaxFactory.ParseExpression("new System.NotImplementedException()").WithAdditionalAnnotations(Simplifier.Annotation)))
                                         )
                                 )
                         )
                 );
            return comp;
        }

        public static ParameterSyntax[] ProcessArgs(SemanticModel  model, ArgumentListSyntax list)
        {
            List<ParameterSyntax> result = new List<ParameterSyntax>();

            foreach (var item in list.Arguments)
            {
                var tt = model.GetSymbolInfo(item.Expression);

                ITypeSymbol type = null;

                if (tt.Symbol is ILocalSymbol)
                {
                    type = (tt.Symbol as ILocalSymbol).Type;
                }
                else if (tt.Symbol is IFieldSymbol)
                {
                    type = (tt.Symbol as IFieldSymbol).Type;
                }

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

            //SyntaxFactory.Parameter(SyntaxFactory.Identifier("obj"))
            //                                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.ThisKeyword))
            //                                        .WithType(SyntaxFactory.ParseTypeName(aa.Type.ToDisplayString())))
        }

        private static void Compilation01()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
@"using System;
using System.Collections.Generic;
using System.Text;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}");


            var root = (CompilationUnitSyntax)tree.GetRoot();

            var compilation = CSharpCompilation.Create("HelloWorld")
                                               .AddReferences(
                                                    MetadataReference.CreateFromFile(
                                                        typeof(object).Assembly.Location))
                                               .AddSyntaxTrees(tree);

            var model = compilation.GetSemanticModel(tree);

            var nameInfo = model.GetSymbolInfo(root.Usings[0].Name);

            var systemSymbol = (INamespaceSymbol)nameInfo.Symbol;


        }

        static void Syntax01()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
@"using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}");

            var root = (CompilationUnitSyntax)tree.GetRoot();

            var firstMember = root.Members[0];

            var helloWorldDeclaration = (NamespaceDeclarationSyntax)firstMember;

            var programDeclaration = (ClassDeclarationSyntax)helloWorldDeclaration.Members[0];

            var mainDeclaration = (MethodDeclarationSyntax)programDeclaration.Members[0];

            var argsParameter = mainDeclaration.ParameterList.Parameters[0];

            var firstParameters = from methodDeclaration in root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                                  where methodDeclaration.Identifier.ValueText == "Main"
                                  select methodDeclaration.ParameterList.Parameters.First();

            var argsParameter2 = firstParameters.Single();
        }

        static void SyntaxWalker()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TopLevel
{
    using Microsoft;
    using System.ComponentModel;

    namespace Child1
    {
        using Microsoft.Win32;
        using System.Runtime.InteropServices;

        class Foo { }
    }

    namespace Child2
    {
        using System.CodeDom;
        using Microsoft.CSharp;

        class Bar { }
    }
}");

            var root = (CompilationUnitSyntax)tree.GetRoot();

            var collector = new UsingCollector();
            collector.Visit(root);

            foreach (var directive in collector.Usings)
            {
                Console.WriteLine(directive.Name);
            }
        }
    }
}
