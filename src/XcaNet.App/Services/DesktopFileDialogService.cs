using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace XcaNet.App.Services;

public sealed class DesktopFileDialogService : IDesktopFileDialogService
{
    private Window? _owner;

    public void SetOwner(Window? window)
    {
        _owner = window;
    }

    public async Task<IReadOnlyList<string>> PickImportFilesAsync(CancellationToken cancellationToken)
    {
        if (_owner?.StorageProvider is null)
        {
            return [];
        }

        var files = await _owner.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Import certificate material",
                AllowMultiple = true,
                FileTypeFilter =
                [
                    new FilePickerFileType("Supported certificate material")
                    {
                        Patterns = ["*.pem", "*.der", "*.cer", "*.crt", "*.csr", "*.key", "*.p8", "*.pfx", "*.p12", "*.crl"]
                    }
                ]
            });

        cancellationToken.ThrowIfCancellationRequested();

        return files
            .Select(x => x.TryGetLocalPath())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray();
    }

    public async Task<string?> PickSavePathAsync(string suggestedFileName, CancellationToken cancellationToken)
    {
        if (_owner?.StorageProvider is null)
        {
            return null;
        }

        var file = await _owner.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export certificate material",
                SuggestedFileName = suggestedFileName
            });

        cancellationToken.ThrowIfCancellationRequested();
        return file?.TryGetLocalPath();
    }
}
