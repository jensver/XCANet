using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace XcaNet.App.ViewModels;

public sealed class ShellViewModel
{
    public ShellViewModel(IConfiguration configuration, ILogger<ShellViewModel> logger)
    {
        logger.LogInformation("Initializing XcaNet shell.");
        Title = configuration["App:Name"] ?? "XcaNet";
        Subtitle = configuration["App:Subtitle"] ?? "Milestone 1 skeleton";
    }

    public string Title { get; }

    public string Subtitle { get; }
}
