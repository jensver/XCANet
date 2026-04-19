using System.Text.RegularExpressions;
using XcaNet.App.ViewModels.Notifications;

namespace XcaNet.Integration.Tests;

public sealed class ThemeAndDeveloperToolingTests
{
    private static readonly string[] KeyViewPaths =
    [
        "src/XcaNet.App/Views/MainWindow.axaml",
        "src/XcaNet.App/Views/Pages/CertificatesPageView.axaml",
        "src/XcaNet.App/Views/Pages/PrivateKeysPageView.axaml",
        "src/XcaNet.App/Views/Pages/CertificateRequestsPageView.axaml",
        "src/XcaNet.App/Views/Pages/CertificateRevocationListsPageView.axaml",
        "src/XcaNet.App/Views/Pages/TemplatesPageView.axaml",
        "src/XcaNet.App/Views/Pages/SettingsSecurityPageView.axaml",
        "src/XcaNet.App/Views/Pages/DashboardPageView.axaml"
    ];

    [Fact]
    public void NotificationItemViewModel_ShouldExposeSemanticStateFlags()
    {
        var success = new NotificationItemViewModel("Success", "ok");
        var error = new NotificationItemViewModel("Error", "no");
        var info = new NotificationItemViewModel("Info", "note");

        Assert.True(success.IsSuccess);
        Assert.False(success.IsError);
        Assert.True(error.IsError);
        Assert.False(error.IsInfo);
        Assert.True(info.IsInfo);
    }

    [Fact]
    public void KeyViews_ShouldAvoidHardCodedBrushLiterals()
    {
        var repoRoot = FindRepoRoot();
        var forbiddenPattern = new Regex("(Background|Foreground|BorderBrush)=\"(White|Black|#[0-9A-Fa-f]{3,8})\"", RegexOptions.Compiled);

        foreach (var relativePath in KeyViewPaths)
        {
            var fullPath = Path.Combine(repoRoot.FullName, relativePath);
            var content = File.ReadAllText(fullPath);

            Assert.DoesNotMatch(forbiddenPattern, content);
        }
    }

    [Fact]
    public void ThemeDocs_ShouldDescribeSharedResourcesAndVerification()
    {
        var repoRoot = FindRepoRoot();
        var content = File.ReadAllText(Path.Combine(repoRoot.FullName, "docs/ui/theming.md"));

        Assert.Contains("ThemeResources.axaml", content, StringComparison.Ordinal);
        Assert.Contains("light and dark mode", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Manual verification checklist", content, StringComparison.Ordinal);
        Assert.Contains("Certificates page", content, StringComparison.Ordinal);
    }

    [Fact]
    public void McpDocs_ShouldStayOptionalAndListBothServers()
    {
        var repoRoot = FindRepoRoot();
        var docs = File.ReadAllText(Path.Combine(repoRoot.FullName, "docs/developer/mcp.md"));
        var readme = File.ReadAllText(Path.Combine(repoRoot.FullName, "README.md"));
        var example = File.ReadAllText(Path.Combine(repoRoot.FullName, "tooling/mcp/workspace.mcp.example.json"));

        Assert.Contains("optional developer tooling only", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not required", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Microsoft Learn MCP", docs, StringComparison.Ordinal);
        Assert.Contains("Avalonia Build MCP", docs, StringComparison.Ordinal);
        Assert.Contains("https://learn.microsoft.com/api/mcp", docs, StringComparison.Ordinal);
        Assert.Contains("https://docs-mcp.avaloniaui.net/mcp", docs, StringComparison.Ordinal);
        Assert.Contains("optional developer guidance", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"microsoft-learn\"", example, StringComparison.Ordinal);
        Assert.Contains("\"avalonia-docs\"", example, StringComparison.Ordinal);
    }

    private static DirectoryInfo FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "README.md")) &&
                Directory.Exists(Path.Combine(current.FullName, "src")) &&
                Directory.Exists(Path.Combine(current.FullName, "tests")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
