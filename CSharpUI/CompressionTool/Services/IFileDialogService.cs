using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace CompressionTool.Services;

public interface IFileDialogService
{
    Task<string?> OpenFileAsync(string title = "Open File", IReadOnlyList<FilePickerFileType>? fileTypes = null);
    Task<string?> SaveFileAsync(string title = "Save File", string? defaultName = null, IReadOnlyList<FilePickerFileType>? fileTypes = null);
}