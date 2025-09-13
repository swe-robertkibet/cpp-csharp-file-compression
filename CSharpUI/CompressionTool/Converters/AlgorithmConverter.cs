using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CompressionTool.Models;
using CompressionTool.ViewModels;

namespace CompressionTool.Views;

public class AlgorithmConverter : IValueConverter
{
    public static readonly AlgorithmConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CompressionAlgorithm algorithm && parameter is AlgorithmOption[] options)
        {
            return Array.Find(options, o => o.Algorithm == algorithm) ?? options[0];
        }
        return new AlgorithmOption { Name = "Unknown", Algorithm = CompressionAlgorithm.LZW, Description = "Default algorithm" };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AlgorithmOption option)
        {
            return option.Algorithm;
        }
        return CompressionAlgorithm.LZW;
    }
}

public class OperationTextConverter : IValueConverter
{
    public static readonly OperationTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isCompression)
        {
            return isCompression ? "🗜️ Compress File" : "📦 Decompress File";
        }
        return "Process File";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}