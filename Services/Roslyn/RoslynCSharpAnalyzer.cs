using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Services.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Engineering_IntelligenceTools.Services.Roslyn;
public class RoslynCSharpAnalyzer : IRoslynCSharpAnalyzer
{
    private const int LongMethodLineThreshold = 60;
    private const int DeepNestingThreshold = 4;

    public RoslynFileMetrics Analyze(string filePath, string sourceText)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceText);
        var root = tree.GetCompilationUnitRoot();

        var methods = root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>().ToList();
        var findings = new List<RoslynFinding>();

        var totalComplexity = 0;
        var maxNesting = 0;
        var longestMethodLines = 0;

        foreach (var method in methods)
        {
            totalComplexity += CalculateCyclomaticComplexity(method);

            var lineSpan = method.GetLocation().GetLineSpan();
            var methodLines = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;
            longestMethodLines = Math.Max(longestMethodLines, methodLines);

            if (methodLines > LongMethodLineThreshold)
            {
                findings.Add(new RoslynFinding
                {
                    Type = "LongMethod",
                    Description = $"Method spans {methodLines} lines - consider splitting it.",
                    Line = lineSpan.StartLinePosition.Line + 1
                });
            }

            var nesting = CalculateMaxNestingDepth(method);
            maxNesting = Math.Max(maxNesting, nesting);

            if (nesting >= DeepNestingThreshold)
            {
                findings.Add(new RoslynFinding
                {
                    Type = "DeepNesting",
                    Description = $"Nesting depth of {nesting} - consider extracting guard clauses or smaller methods.",
                    Line = lineSpan.StartLinePosition.Line + 1
                });
            }
        }

        // Real (not regex-guessed) empty catch block detection.
        foreach (var catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            if (catchClause.Block.Statements.Count == 0)
            {
                findings.Add(new RoslynFinding
                {
                    Type = "EmptyCatchBlock",
                    Description = "Empty catch block silently swallows exceptions.",
                    Line = catchClause.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                });
            }
        }

        return new RoslynFileMetrics
        {
            FilePath = filePath,
            CyclomaticComplexity = totalComplexity,
            MaxNestingDepth = maxNesting,
            MethodCount = methods.Count,
            LongestMethodLines = longestMethodLines,
            Findings = findings
        };
    }

    private static int CalculateCyclomaticComplexity(SyntaxNode method)
    {
        var complexity = 1;

        complexity += method.DescendantNodes().Count(n => n is IfStatementSyntax);
        complexity += method.DescendantNodes().Count(n =>
            n is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax);
        complexity += method.DescendantNodes().Count(n =>
            n is CaseSwitchLabelSyntax or CasePatternSwitchLabelSyntax);
        complexity += method.DescendantNodes().Count(n => n is CatchClauseSyntax);
        complexity += method.DescendantNodes().Count(n => n is ConditionalExpressionSyntax);
        complexity += method.DescendantNodes()
            .OfType<BinaryExpressionSyntax>()
            .Count(b => b.IsKind(SyntaxKind.LogicalAndExpression) || b.IsKind(SyntaxKind.LogicalOrExpression));

        return complexity;
    }

    /// <summary>
    /// Real block-nesting depth (if/for/foreach/while/do/switch/try), walked
    /// from the method body down - not a brace-counting guess over diff text.
    /// </summary>
    private static int CalculateMaxNestingDepth(SyntaxNode method)
    {
        var maxDepth = 0;

        void Walk(SyntaxNode node, int depth)
        {
            maxDepth = Math.Max(maxDepth, depth);

            foreach (var child in node.ChildNodes())
            {
                var increasesDepth = child is IfStatementSyntax or ForStatementSyntax or ForEachStatementSyntax
                    or WhileStatementSyntax or DoStatementSyntax or SwitchStatementSyntax or TryStatementSyntax;

                Walk(child, increasesDepth ? depth + 1 : depth);
            }
        }

        Walk(method, 0);
        return maxDepth;
    }
}
