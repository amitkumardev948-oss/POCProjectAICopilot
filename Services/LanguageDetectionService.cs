using Engineering_IntelligenceTools.Models.GitHub;
using Engineering_IntelligenceTools.Services.Interfaces;

namespace Engineering_IntelligenceTools.Services;

public class LanguageDetectionService : ILanguageDetectionService
{
    private static readonly Dictionary<string, string> ExtensionToLanguage = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cs"] = "C#",
        ["csproj"] = ".NET project",
        ["sln"] = ".NET solution",
        ["ts"] = "TypeScript",
        ["tsx"] = "TypeScript (React)",
        ["js"] = "JavaScript",
        ["jsx"] = "JavaScript (React)",
        ["py"] = "Python",
        ["java"] = "Java",
        ["go"] = "Go",
        ["rb"] = "Ruby",
        ["php"] = "PHP",
        ["sql"] = "SQL",
        ["json"] = "JSON config",
        ["yml"] = "YAML config",
        ["yaml"] = "YAML config",
        ["html"] = "HTML",
        ["css"] = "CSS",
        ["scss"] = "SCSS"
    };

    private static readonly Dictionary<string, string> ManifestFileToLanguage = new(StringComparer.OrdinalIgnoreCase)
    {
        ["package.json"] = "Node.js",
        ["requirements.txt"] = "Python (pip)",
        ["pyproject.toml"] = "Python (poetry)",
        ["pom.xml"] = "Java (Maven)",
        ["build.gradle"] = "Java/Kotlin (Gradle)",
        ["go.mod"] = "Go modules",
        ["Gemfile"] = "Ruby (Bundler)",
        ["composer.json"] = "PHP (Composer)"
    };

    public List<string> Detect(IReadOnlyList<ChangedFile> files)
    {
        var languages = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file.Path);

            if (ManifestFileToLanguage.TryGetValue(fileName, out var manifestLanguage))
            {
                languages.Add(manifestLanguage);
                continue;
            }

            if (ExtensionToLanguage.TryGetValue(file.Extension, out var language))
            {
                languages.Add(language);
            }
        }

        return languages.ToList();
    }
}
