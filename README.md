# Engineering Intelligence Tools — Phase 1

Rule-based (no LLM yet) code analyzer & PR risk engine for ASP.NET Core 8.
Works against any language, since analysis runs off file extensions and diff
text, not language-specific parsing.

## What it does

1. Receives GitHub webhooks (`pull_request` and `push` events)
2. Verifies the payload signature (HMAC-SHA256)
3. Fetches the changed files (diff/patch) for that PR or push via the GitHub API
4. Runs them through the analyzer pipeline:
   - Language/tech detection
   - Complexity scoring (lines changed, nesting depth heuristic)
   - Code smell detection (regex rules: empty catch, hardcoded secrets, unguarded
     DataSet row access, debug prints left in, TODO/FIXME, magic numbers)
   - Dependency diffing (csproj / package.json / requirements.txt)
   - Impact analysis (critical-path files touched: auth, payment, security, etc.)
   - Risk scoring (LOW / MEDIUM / HIGH / CRITICAL)
5. Returns a single JSON `AnalysisResult`

Phase 2 will feed this same structured `AnalysisResult` to an LLM as grounding
context for natural-language review comments — nothing in this pipeline needs
to change for that, the orchestrator just gets a new consumer.

## Project layout

```
Controllers/
  GitHubWebhookController.cs   - webhook receiver (signature check, enqueue)
  AnalysisController.cs        - manual test trigger + fetch-latest endpoint
Services/
  Interfaces/                  - one interface per pipeline stage
  GitHubClientService.cs       - Octokit wrapper (PR files, compare, contents)
  WebhookSignatureValidator.cs - HMAC-SHA256 X-Hub-Signature-256 check
  LanguageDetectionService.cs
  ComplexityAnalyzer.cs
  CodeSmellDetector.cs
  DependencyAnalyzer.cs
  ImpactAnalyzer.cs
  RiskEngine.cs
  AnalyzerOrchestrator.cs      - runs all stages, builds AnalysisResult
  InMemoryAnalysisResultStore.cs
  BackgroundAnalysisQueue.cs
  BackgroundAnalysisWorker.cs  - drains the queue so webhooks respond in <1s
Models/
  GitHub/                      - webhook payload DTOs + normalized ChangedFile
  Analysis/                    - AnalysisResult and all its nested types
  Enums/
Configuration/
  GitHubOptions.cs
Extensions/
  ServiceCollectionExtensions.cs - all DI wiring, keeps Program.cs clean
```

## 1. Configure

Edit `appsettings.json` (or better, use `dotnet user-secrets` locally so tokens
never get committed):

```bash
dotnet user-secrets init
dotnet user-secrets set "GitHub:AccessToken" "ghp_xxxxxxxxxxxx"
dotnet user-secrets set "GitHub:WebhookSecret" "your-webhook-secret"
```

- `AccessToken`: a Personal Access Token (classic or fine-grained) with
  `repo` → contents:read + pull-requests:read scopes. Good enough for Phase 1
  testing; swap for a GitHub App installation token later for per-repo,
  least-privilege access.
- `WebhookSecret`: any random string — must match what you configure on the
  GitHub webhook in step 3.

## 2. Run it

```bash
dotnet restore
dotnet run
```

Swagger UI opens at `https://localhost:7186/swagger` (or the port shown in
your terminal). You'll see both controllers listed there.

## 3. Test without a webhook (fastest way to see it work)

Use the manual endpoint — works against any public repo, any language,
no webhook/GitHub App setup needed:

```bash
curl -X POST https://localhost:7186/api/analysis/run \
  -H "Content-Type: application/json" \
  -d '{
        "owner": "dotnet",
        "repo": "runtime",
        "pullRequestNumber": 100000
      }'
```

Response is the full `AnalysisResult` JSON. Fetch it again later with:

```bash
curl https://localhost:7186/api/analysis/dotnet/runtime?pullRequestNumber=100000
```

## 4. Connect a real GitHub repo (webhook flow)

1. On your repo (or a GitHub App): **Settings → Webhooks → Add webhook**
   - Payload URL: `https://<your-public-url>/api/webhooks/github`
   - Content type: `application/json`
   - Secret: same value as `GitHub:WebhookSecret`
   - Events: select "Pull requests" and "Pushes" (or "Send me everything")
2. Expose your local API to the internet for testing:
   - `smee.io` (GitHub's official proxy) — create a channel, run the smee
     client locally, point it at `http://localhost:5184/api/webhooks/github`
   - or `ngrok http 5184`
3. Open a test PR (or push a commit) on that repo.
4. Check your app logs — you'll see "Queued analysis for owner/repo PR #N",
   then shortly after "Analysis complete ... risk=...".
5. Pull the result: `GET /api/analysis/{owner}/{repo}?pullRequestNumber={n}`

## Sample response shape

```json
{
  "repoName": "org/repo",
  "pullRequestNumber": 42,
  "baseSha": "abc123",
  "headSha": "def456",
  "languages": ["C#", ".NET project"],
  "complexity": { "score": 7.2, "totalLinesChanged": 340, "filesChanged": 6, "maxNestingDepthDelta": 3, "averageChangeSizePerFile": 56.7 },
  "risk": "High",
  "dependencies": { "added": [{ "name": "Newtonsoft.Json", "version": "13.0.3", "manifestFile": "src/Api.csproj" }], "removed": [] },
  "codeSmells": [
    { "file": "Repositories/PGGetEarningsRepository.cs", "line": 45, "type": "UnguardedRowAccess", "description": "DataSet/DataTable row accessed by index without a null/count guard.", "severity": "High" }
  ],
  "impact": { "filesAffected": 6, "criticalPathTouched": true, "criticalFiles": ["Auth/TokenService.cs"] },
  "files": [{ "path": "Repositories/PGGetEarningsRepository.cs", "status": "Modified", "additions": 30, "deletions": 5 }],
  "recommendations": [
    "Add a null/row-count check before indexing into DataSet/DataTable rows.",
    "Risk is elevated - recommend an additional reviewer before merging."
  ],
  "generatedAtUtc": "2026-08-15T10:00:00Z"
}
```

## Roslyn-based deep analysis (C# only)

For any changed `.cs` file, the orchestrator now fetches the **full file
content** at `HeadSha` (not just the diff patch) and runs it through
`RoslynCSharpAnalyzer` (`Services/Roslyn/`), which uses `Microsoft.CodeAnalysis`
to build a real syntax tree instead of guessing from diff text. It computes:

- **Cyclomatic complexity** (McCabe: 1 + decision points - `if`, loops, `case`
  labels, `catch`, ternaries, `&&`/`||`)
- **Real nesting depth** (walked from the method body, not brace-counting)
- **AST-verified empty catch blocks** (no false positives from formatting)
- **Long methods** (line-span based)

Results land in two places on the JSON response:
- `roslynMetrics` - one entry per analyzed `.cs` file, with the raw metrics
- `codeSmells` - Roslyn findings are folded in here too, so the risk engine
  and recommendations already account for them without knowing Roslyn exists

Non-`.cs` files keep using the diff-based heuristic analyzers - nothing about
the pipeline changes for other languages. This is a good spot to later plug
in a real `DiagnosticAnalyzer`/`CodeFixProvider` pair if you want to reuse
existing Roslyn analyzer packages (e.g. `Microsoft.CodeAnalysis.NetAnalyzers`)
instead of the hand-rolled walker in `RoslynCSharpAnalyzer.cs`.

## Notes / what to verify on your machine

- This was hand-written without a live NuGet restore in the sandbox it was
  built in (no internet access to nuget.org there). Run `dotnet build` first
  thing — if Octokit's method/type names differ slightly from what's used in
  `GitHubClientService.cs` for your installed version (`13.0.1`), the fix is
  usually a one-line rename.
- `InMemoryAnalysisResultStore` is intentionally swappable — implement
  `IAnalysisResultStore` against PostgreSQL (matches the rest of your stack)
  when you're ready to persist results instead of keeping them in memory.
- All rule weights (risk engine, complexity scoring) live in one place each
  (`RiskEngine.cs`, `ComplexityAnalyzer.cs`) — tune them once you have real
  PR history to calibrate against.
