namespace XcaNet.App.Services;

public interface IDesktopFileDialogService
{
    void SetOwner(Avalonia.Controls.Window? window);

    Task<IReadOnlyList<string>> PickImportFilesAsync(CancellationToken cancellationToken);

    Task<string?> PickSavePathAsync(string suggestedFileName, CancellationToken cancellationToken);

    Task<string?> GetClipboardTextAsync(CancellationToken cancellationToken);
}
