using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using XcaNet.App.ViewModels;

namespace XcaNet.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not ShellViewModel shellViewModel || !e.DataTransfer.Contains(DataFormat.File))
        {
            return;
        }

        var items = e.DataTransfer.TryGetFiles();
        var filePaths = items?
            .Select(x => x.TryGetLocalPath())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray()
            ?? [];

        if (filePaths.Length == 0)
        {
            return;
        }

        await shellViewModel.ImportFilesFromDropAsync(filePaths);
        e.Handled = true;
    }
}
