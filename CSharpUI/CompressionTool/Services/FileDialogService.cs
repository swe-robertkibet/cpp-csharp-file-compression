using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace CompressionTool.Services;

public class FileDialogService : IFileDialogService
{
    private readonly TopLevel _topLevel;

    public FileDialogService(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    public async Task<string?> OpenFileAsync(string title = "Open File", IReadOnlyList<FilePickerFileType>? fileTypes = null)
    {
        if (!_topLevel.StorageProvider.CanOpen)
            return null;

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileTypes ?? new[] { FilePickerFileTypes.All }
        };

        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(options);

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    public async Task<string?> SaveFileAsync(string title = "Save File", string? defaultName = null, IReadOnlyList<FilePickerFileType>? fileTypes = null)
    {
        if (!_topLevel.StorageProvider.CanSave)
            return null;

        var options = new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultName,
            FileTypeChoices = fileTypes ?? new[] { FilePickerFileTypes.All }
        };

        var file = await _topLevel.StorageProvider.SaveFilePickerAsync(options);

        return file?.Path.LocalPath;
    }
}