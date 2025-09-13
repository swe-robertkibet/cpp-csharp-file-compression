using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CompressionTool.Models;
using CompressionTool.Services;

namespace CompressionTool.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly CompressionService _compressionService;

    [ObservableProperty]
    private string _inputFilePath = string.Empty;

    [ObservableProperty]
    private string _outputFilePath = string.Empty;

    [ObservableProperty]
    private CompressionAlgorithm _selectedAlgorithm = CompressionAlgorithm.LZW;

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

    public MainWindowViewModel()
    {
        _compressionService = new CompressionService();
        LogMessages.Add($"Application started - {DateTime.Now:HH:mm:ss}");
    }

    [RelayCommand]
    private async Task BrowseInputFile()
    {
        // This will be implemented with platform-specific file dialogs
        // For now, we'll use a placeholder
        LogMessages.Add($"Browse input file requested - {DateTime.Now:HH:mm:ss}");
    }

    [RelayCommand]
    private async Task BrowseOutputFile()
    {
        // This will be implemented with platform-specific file dialogs
        LogMessages.Add($"Browse output file requested - {DateTime.Now:HH:mm:ss}");
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
        LogMessages.Add($"Algorithm changed to {_compressionService.GetAlgorithmName(value)} - {DateTime.Now:HH:mm:ss}");
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
