using System.Text.RegularExpressions;
using Engineering_IntelligenceTools.Models.Analysis;
using Engineering_IntelligenceTools.Models.GitHub;
using Engineering_IntelligenceTools.Services.Interfaces;

namespace Engineering_IntelligenceTools.Services;

public class DependencyAnalyzer : IDependencyAnalyzer
{
    private static readonly string[] ManifestFileNames =
    {
        "csproj", "package.json", "requirements.txt", "pom.xml", "go.mod"
    };

    // <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    private static readonly Regex CsprojPackageRegex = new(
        @"<PackageReference\s+Include=""([^""]+)""(?:\s+Version=""([^""]+)"")?",
        RegexOptions.Compiled);

    // "newtonsoft.json": "13.0.3"
    private static readonly Regex PackageJsonRegex = new(
        @"""([\w@\-/.]+)"":\s*""([^""]+)""",
        RegexOptions.Compiled);

    // requests==2.31.0  /  requests>=2.31.0
    private static readonly Regex RequirementsTxtRegex = new(
        @"^([\w\-]+)\s*(==|>=|<=)?\s*([\w.]*)",
        RegexOptions.Compiled);

    public DependencyChanges Analyze(IReadOnlyList<ChangedFile> files)
    {
        var result = new DependencyChanges();

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Patch) || !IsManifestFile(file.Path))
            {
                continue;
            }

            foreach (var line in file.Patch.Split('\n'))
            {
                if (line.StartsWith("+++") || line.StartsWith("---"))
                {
                    continue;
                }

                if (line.StartsWith('+'))
                {
                    TryExtractDependency(file.Path, line[1..], result.Added);
                }
                else if (line.StartsWith('-'))
                {
                    TryExtractDependency(file.Path, line[1..], result.Removed);
                }
            }
        }

        return result;
    }

    private static bool IsManifestFile(string path)
    {
        var fileName = Path.GetFileName(path);
        return ManifestFileNames.Any(m =>
            fileName.Equals(m, StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith($".{m}", StringComparison.OrdinalIgnoreCase));
    }

    private static void TryExtractDependency(string manifestPath, string line, List<DependencyChange> target)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        var fileName = Path.GetFileName(manifestPath);

        if (fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            var match = CsprojPackageRegex.Match(trimmed);
            if (match.Success)
            {
                target.Add(new DependencyChange
                {
                    Name = match.Groups[1].Value,
                    Version = match.Groups[2].Success ? match.Groups[2].Value : null,
                    ManifestFile = manifestPath
                });
            }
            return;
        }

        if (fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase))
        {
            var match = PackageJsonRegex.Match(trimmed);
            // Skip well-known non-dependency keys so we don't report "name"/"version" of the package itself.
            if (match.Success && match.Groups[1].Value is not ("name" or "version" or "description" or "main" or "scripts"))
            {
                target.Add(new DependencyChange
                {
                    Name = match.Groups[1].Value,
                    Version = match.Groups[2].Value,
                    ManifestFile = manifestPath
                });
            }
            return;
        }

        if (fileName.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase))
        {
            var match = RequirementsTxtRegex.Match(trimmed);
            if (match.Success && match.Groups[1].Value.Length > 0)
            {
                target.Add(new DependencyChange
                {
                    Name = match.Groups[1].Value,
                    Version = match.Groups[3].Success ? match.Groups[3].Value : null,
                    ManifestFile = manifestPath
                });
            }
        }
    }
}
