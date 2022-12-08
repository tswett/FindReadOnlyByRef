// Copyright 2022 by Medallion Instrumentation Systems
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.

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
                    IParameterSymbol thisParameter = null;

                    if (node.IsNamed)
                    {
                        thisParameter = method.Parameters.FirstOrDefault(parameter =>
                            parameter.Name == node.NameColonEquals.Name.ToString());
                    }
                    else
                    {
                        int thisArgumentIndex = argumentList.Arguments.IndexOf(node);
                        if (thisArgumentIndex < method.Parameters.Length)
                            thisParameter = method.Parameters[thisArgumentIndex];
                    }

                    // If we couldn't find the parameter for some reason, the
                    // best we can do is just accept it.
                    if (thisParameter == null)
                        return false;

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
