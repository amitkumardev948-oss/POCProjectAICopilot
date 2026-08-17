using System.Text.RegularExpressions;
using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Models.Enums;
using Engineering_IntelligenceTools.Models.GitHub;
using Engineering_IntelligenceTools.Services.Interfaces;

namespace Engineering_IntelligenceTools.Services;

public class CodeSmellDetector : ICodeSmellDetector
{
    private readonly ILogger<CodeSmellDetector> _logger;

    public CodeSmellDetector(ILogger<CodeSmellDetector> logger)
    {
        _logger = logger;
    }

    private static readonly List<SmellRule> Rules = new()
    {
        new SmellRule("TodoOrFixme",
            new Regex(@"//\s*(TODO|FIXME)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "Unresolved TODO/FIXME left in changed code.",
            CodeSmellSeverity.Low),

        new SmellRule(
            "MagicNumber",
            new Regex(@"[^\w.](?<!case\s)\b\d{3,}\b(?!\s*[;,)]?\s*//)", RegexOptions.Compiled),
            "Multi-digit literal with no named constant - consider extracting it.",
            CodeSmellSeverity.Info),

        new SmellRule(
            "EmptyCatchBlock",
            new Regex(@"catch\s*(\([^)]*\))?\s*\{\s*\}", RegexOptions.Compiled),
            "Empty catch block silently swallows exceptions.",
            CodeSmellSeverity.High),

        new SmellRule(
            "UnguardedRowAccess",
            new Regex(@"\.Rows\[0\]|\.Tables\[0\]\.Rows\[0\]", RegexOptions.Compiled),
            "DataSet/DataTable row accessed by index without a null/count guard.",
            CodeSmellSeverity.High),

        new SmellRule(
            "HardcodedSecret",
            new Regex(@"(password|secret|apikey|api_key)\s*=\s*""[^""]+""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "Possible hardcoded credential/secret in source.",
            CodeSmellSeverity.High),

        new SmellRule(
            "ConsoleDebugLeftIn",
            new Regex(@"Console\.WriteLine\(|console\.log\(", RegexOptions.Compiled),
            "Debug print statement left in changed code.",
            CodeSmellSeverity.Info)
    };

    public List<CodeSmell> Detect(IReadOnlyList<ChangedFile> files)
    {
        var smells = new List<CodeSmell>();

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Patch))
            {
                continue;
            }

            var lines = file.Patch.Split('\n');
            var lineNumberInNewFile = 0;

            foreach (var line in lines)
            {
                if (line.StartsWith("@@"))
                {
                    lineNumberInNewFile = ParseHunkStartLine(line);
                    continue;
                }

                if (line.StartsWith('+') && !line.StartsWith("+++"))
                {
                    lineNumberInNewFile++;
                    var content = line[1..];

                    foreach (var rule in Rules)
                    {
                        if (rule.Pattern.IsMatch(content))
                        {
                            smells.Add(new CodeSmell
                            {
                                File = file.Path,
                                Line = lineNumberInNewFile,
                                Type = rule.Name,
                                Description = rule.Description,
                                Severity = rule.Severity
                            });
                        }
                    }
                }
                else if (!line.StartsWith('-'))
                {
                    lineNumberInNewFile++;
                }
            }
        }

        _logger.LogInformation("Detected {SmellCount} code smell(s) across {FileCount} file(s).", smells.Count, files.Count);
        return smells;
    }
    private static int ParseHunkStartLine(string hunkHeader)
    {
        var match = Regex.Match(hunkHeader, @"\+(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) - 1 : 0;
    }

    private record SmellRule(string Name, Regex Pattern, string Description, CodeSmellSeverity Severity);
}
