using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace AutoSalesApp.Services;

// Диалоги выбора файла через StorageProvider главного окна
public static class FileDialogHelper
{
    private static IStorageProvider? GetStorageProvider()
        => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow?.StorageProvider;

    public static async Task<string?> PickSaveFileAsync(string title, string extension)
    {
        var provider = GetStorageProvider();
        if (provider == null) return null;

        var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = $"{title}.{extension}",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(extension.ToUpperInvariant())
                {
                    Patterns = new[] { $"*.{extension}" }
                }
            }
        });
        return file?.TryGetLocalPath();
    }

    public static async Task<string?> PickOpenFileAsync(string title, string extension)
    {
        var provider = GetStorageProvider();
        if (provider == null) return null;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(extension.ToUpperInvariant())
                {
                    Patterns = new[] { $"*.{extension}" }
                }
            }
        });
        return files.FirstOrDefault()?.TryGetLocalPath();
    }
}
