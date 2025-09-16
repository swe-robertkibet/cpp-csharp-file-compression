using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CompressionTool.Models;
using CompressionTool.Services;

namespace CompressionTool.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly CompressionServiceManual _compressionService;
    private readonly IFileDialogService _fileDialogService;

    [ObservableProperty]
    private string _inputFilePath = string.Empty;

    [ObservableProperty]
    private string _outputFilePath = string.Empty;

    [ObservableProperty]
    private CompressionAlgorithm _selectedAlgorithm = CompressionAlgorithm.LZW;

    [ObservableProperty]
    private AlgorithmOption _selectedAlgorithmOption;

    [ObservableProperty]
    private bool _isCompression = true;

    [ObservableProperty]
    private bool _isDecompression = false;

    [ObservableProperty]
    private bool _isOperationInProgress = false;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private CompressionResult? _lastResult;

    [ObservableProperty]
    private ObservableCollection<string> _logMessages = new();

    public ObservableCollection<AlgorithmOption> AvailableAlgorithms { get; } = new()
    {
        new AlgorithmOption { Name = "LZW (Recommended)", Algorithm = CompressionAlgorithm.LZW, Description = "General-purpose, consistent performance" },
        new AlgorithmOption { Name = "Run-Length Encoding", Algorithm = CompressionAlgorithm.RLE, Description = "Best for data with long runs of identical values" },
        new AlgorithmOption { Name = "Huffman Coding", Algorithm = CompressionAlgorithm.Huffman, Description = "Optimal for text with uneven character frequencies" }
    };

    // File type filters for dialogs
    private static readonly FilePickerFileType TextFileType = new("Text Files")
    {
        Patterns = new[] { "*.txt", "*.text" },
        MimeTypes = new[] { "text/plain" }
    };

    private static readonly FilePickerFileType CompressedFileType = new("Compressed Files")
    {
        Patterns = new[] { "*.huf", "*.lzw", "*.rle" }
    };

    private static readonly FilePickerFileType HuffmanFileType = new("Huffman Files")
    {
        Patterns = new[] { "*.huf" }
    };

    private static readonly FilePickerFileType LzwFileType = new("LZW Files")
    {
        Patterns = new[] { "*.lzw" }
    };

    private static readonly FilePickerFileType RleFileType = new("RLE Files")
    {
        Patterns = new[] { "*.rle" }
    };

    public MainWindowViewModel(IFileDialogService fileDialogService)
    {
        _compressionService = new CompressionServiceManual();
        _fileDialogService = fileDialogService;
        _selectedAlgorithmOption = AvailableAlgorithms[0]; // Default to LZW
        LogMessages.Add($"Application started - {DateTime.Now:HH:mm:ss}");
        LogMessages.Add($"Library Load Status: {(CompressionServiceManual.IsLibraryLoaded ? "SUCCESS" : "FAILED")}");
    }

    [RelayCommand]
    private async Task BrowseInputFileAsync()
    {
        try
        {
            var fileTypes = GetInputFileTypes();
            var selectedFile = await _fileDialogService.OpenFileAsync("Select Input File", fileTypes);

            if (!string.IsNullOrEmpty(selectedFile))
            {
                InputFilePath = selectedFile;
                LogMessages.Add($"Selected input file: {Path.GetFileName(selectedFile)} - {DateTime.Now:HH:mm:ss}");
            }
        }
        catch (Exception ex)
        {
            LogMessages.Add($"Error browsing input file: {ex.Message} - {DateTime.Now:HH:mm:ss}");
        }
    }

    [RelayCommand]
    private async Task BrowseOutputFileAsync()
    {
        try
        {
            var fileTypes = GetOutputFileTypes();
            string defaultName = GetSuggestedOutputFileName();
            var selectedFile = await _fileDialogService.SaveFileAsync("Save Output File", defaultName, fileTypes);

            if (!string.IsNullOrEmpty(selectedFile))
            {
                OutputFilePath = selectedFile;
                LogMessages.Add($"Selected output file: {Path.GetFileName(selectedFile)} - {DateTime.Now:HH:mm:ss}");
            }
        }
        catch (Exception ex)
        {
            LogMessages.Add($"Error browsing output file: {ex.Message} - {DateTime.Now:HH:mm:ss}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteOperation))]
    private async Task ExecuteOperation()
    {
        IsOperationInProgress = true;
        StatusMessage = IsCompression ? "Compressing..." : "Decompressing...";

        try
        {
            CompressionResult result;

            if (IsCompression)
            {
                result = await _compressionService.CompressFileAsync(SelectedAlgorithm, InputFilePath, OutputFilePath);
                LogMessages.Add($"Compression completed - {DateTime.Now:HH:mm:ss}");
            }
            else
            {
                result = await _compressionService.DecompressFileAsync(SelectedAlgorithm, InputFilePath, OutputFilePath);
                LogMessages.Add($"Decompression completed - {DateTime.Now:HH:mm:ss}");
            }

            LastResult = result;

            if (result.Success)
            {
                StatusMessage = IsCompression ? "Compression successful!" : "Decompression successful!";
                LogMessages.Add($"Operation succeeded: {result.CompressionRatioFormatted} ratio, {(IsCompression ? result.CompressionTimeFormatted : result.DecompressionTimeFormatted)}");
            }
            else
            {
                StatusMessage = $"Operation failed: {result.ErrorMessage}";
                LogMessages.Add($"Operation failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            LogMessages.Add($"Error: {ex.Message} - {DateTime.Now:HH:mm:ss}");
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }

    private bool CanExecuteOperation()
    {
        return !IsOperationInProgress &&
               !string.IsNullOrWhiteSpace(InputFilePath) &&
               !string.IsNullOrWhiteSpace(OutputFilePath) &&
               File.Exists(InputFilePath);
    }

    partial void OnInputFilePathChanged(string value)
    {
        ExecuteOperationCommand.NotifyCanExecuteChanged();

        if (!string.IsNullOrWhiteSpace(value) && File.Exists(value))
        {
            // Auto-suggest output file name based on algorithm and operation
            string extension = IsCompression ? GetCompressionExtension() : ".txt";
            string baseName = IsCompression ?
                Path.GetFileNameWithoutExtension(value) :
                Path.GetFileNameWithoutExtension(value).Replace("_compressed", "").Replace("_decompressed", "");

            string directory = Path.GetDirectoryName(value) ?? "";
            string suggestedOutput = Path.Combine(directory, $"{baseName}{(IsCompression ? "_compressed" : "_decompressed")}{extension}");

            if (string.IsNullOrWhiteSpace(OutputFilePath))
            {
                OutputFilePath = suggestedOutput;
            }
        }
    }

    partial void OnOutputFilePathChanged(string value)
    {
        ExecuteOperationCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsCompressionChanged(bool value)
    {
        if (value)
        {
            IsDecompression = false;
        }
        UpdateSuggestedOutputPath();
    }

    partial void OnIsDecompressionChanged(bool value)
    {
        if (value)
        {
            IsCompression = false;
        }
        UpdateSuggestedOutputPath();
    }

    partial void OnSelectedAlgorithmChanged(CompressionAlgorithm value)
    {
        UpdateSuggestedOutputPath();
        // Use fallback algorithm names to avoid UI thread blocking from native calls
        string algorithmName = value switch
        {
            CompressionAlgorithm.RLE => "Run-Length Encoding",
            CompressionAlgorithm.Huffman => "Huffman Coding",
            CompressionAlgorithm.LZW => "LZW",
            _ => "Unknown"
        };
        LogMessages.Add($"Algorithm changed to {algorithmName} - {DateTime.Now:HH:mm:ss}");
    }

    partial void OnSelectedAlgorithmOptionChanged(AlgorithmOption value)
    {
        if (value != null)
        {
            SelectedAlgorithm = value.Algorithm;
        }
    }

    private void UpdateSuggestedOutputPath()
    {
        if (!string.IsNullOrWhiteSpace(InputFilePath) && File.Exists(InputFilePath))
        {
            OnInputFilePathChanged(InputFilePath);
        }
    }

    private string GetCompressionExtension()
    {
        return SelectedAlgorithm switch
        {
            CompressionAlgorithm.RLE => ".rle",
            CompressionAlgorithm.Huffman => ".huf",
            CompressionAlgorithm.LZW => ".lzw",
            _ => ".compressed"
        };
    }

    private IReadOnlyList<FilePickerFileType> GetInputFileTypes()
    {
        if (IsCompression)
        {
            return new[] { TextFileType, FilePickerFileTypes.All };
        }
        else
        {
            return SelectedAlgorithm switch
            {
                CompressionAlgorithm.Huffman => new[] { HuffmanFileType, CompressedFileType, FilePickerFileTypes.All },
                CompressionAlgorithm.LZW => new[] { LzwFileType, CompressedFileType, FilePickerFileTypes.All },
                CompressionAlgorithm.RLE => new[] { RleFileType, CompressedFileType, FilePickerFileTypes.All },
                _ => new[] { CompressedFileType, FilePickerFileTypes.All }
            };
        }
    }

    private IReadOnlyList<FilePickerFileType> GetOutputFileTypes()
    {
        if (IsCompression)
        {
            return SelectedAlgorithm switch
            {
                CompressionAlgorithm.Huffman => new[] { HuffmanFileType, FilePickerFileTypes.All },
                CompressionAlgorithm.LZW => new[] { LzwFileType, FilePickerFileTypes.All },
                CompressionAlgorithm.RLE => new[] { RleFileType, FilePickerFileTypes.All },
                _ => new[] { FilePickerFileTypes.All }
            };
        }
        else
        {
            return new[] { TextFileType, FilePickerFileTypes.All };
        }
    }

    private string GetSuggestedOutputFileName()
    {
        if (string.IsNullOrWhiteSpace(InputFilePath))
            return string.Empty;

        string baseName = Path.GetFileNameWithoutExtension(InputFilePath);
        string extension = IsCompression ? GetCompressionExtension() : ".txt";

        if (!IsCompression)
        {
            baseName = baseName.Replace("_compressed", "").Replace("_decompressed", "");
        }

        return $"{baseName}{(IsCompression ? "_compressed" : "_decompressed")}{extension}";
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogMessages.Clear();
        LogMessages.Add($"Log cleared - {DateTime.Now:HH:mm:ss}");
    }
}

public class AlgorithmOption
{
    public string Name { get; set; } = string.Empty;
    public CompressionAlgorithm Algorithm { get; set; }
    public string Description { get; set; } = string.Empty;
}
