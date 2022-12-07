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

            if (!IsByRef(node, semanticModel))
                return;

            (bool isReadOnly, string symbolType) = IsReadOnly(node, semanticModel);

            if (isReadOnly)
            {
                Diagnostic diagnostic = Diagnostic.Create(
                    Rule,
                    node.Expression.GetLocation(),
                    symbolType,
                    node.Expression.GetText());
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Determine if the given argument is passed by reference.
        /// </summary>
        private static bool IsByRef(SimpleArgumentSyntax node, SemanticModel semanticModel)
        {
            ArgumentListSyntax argumentList = (ArgumentListSyntax)node.Parent;

            if (argumentList.Parent is InvocationExpressionSyntax invocation)
            {
                SymbolInfo functionInfo = semanticModel.GetSymbolInfo(invocation.Expression);
                if (functionInfo.Symbol is IMethodSymbol method)
                {
                    IParameterSymbol thisParameter;

                    if (node.IsNamed)
                    {
                        thisParameter = method.Parameters.First(parameter =>
                            parameter.Name == node.NameColonEquals.Name.ToString());
                    }
                    else
                    {
                        int thisArgumentIndex = argumentList.Arguments.IndexOf(node);
                        thisParameter = method.Parameters[thisArgumentIndex];
                    }

                    RefKind refKind = thisParameter.RefKind;
                    if (refKind != RefKind.None && refKind != RefKind.In)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determine if the given argument is a read-only field or property.
        /// </summary>
        private static (bool isReadOnly, string symbolType) IsReadOnly(SimpleArgumentSyntax node, SemanticModel semanticModel)
        {
            string symbolType = "field or property";
            bool isReadOnly = false;

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

            return (isReadOnly, symbolType);
        }
    }
}
