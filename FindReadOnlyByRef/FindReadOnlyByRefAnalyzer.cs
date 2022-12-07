using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace FindReadOnlyByRef
{
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    public class FindReadOnlyByRefAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "FindReadOnlyByRef";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SimpleArgument);
        }

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            SimpleArgumentSyntax node = (SimpleArgumentSyntax)context.Node;
            SemanticModel semanticModel = context.SemanticModel;

            bool isByRef = false;
            string symbolType = "field or property";
            bool isReadOnly = false;

            ArgumentListSyntax argumentList = (ArgumentListSyntax)node.Parent;
            int thisArgumentIndex = argumentList.Arguments.IndexOf(node);

            if (argumentList.Parent is InvocationExpressionSyntax invocation)
            {
                SymbolInfo functionInfo = semanticModel.GetSymbolInfo(invocation.Expression);
                if (functionInfo.Symbol is IMethodSymbol method)
                {
                    RefKind refKind = method.Parameters[thisArgumentIndex].RefKind;
                    if (refKind != RefKind.None && refKind != RefKind.In)
                        isByRef = true;
                }
            }

            if (!isByRef)
                return;

            if (node.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                // TODO: we want to check if the member is read-only
                SymbolInfo memberInfo = semanticModel.GetSymbolInfo(memberAccess.Name);

                if (memberInfo.Symbol is IPropertySymbol propertySymbol && propertySymbol.IsReadOnly)
                {
                    symbolType = "property";
                    isReadOnly = true;
                }

                if (memberInfo.Symbol is IFieldSymbol fieldSymbol && fieldSymbol.IsReadOnly)
                {
                    symbolType = "field";
                    isReadOnly = true;
                }
            }

            if (isReadOnly)
            {
                Diagnostic diagnostic = Diagnostic.Create(Rule, node.GetLocation(), symbolType, node.GetText());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
