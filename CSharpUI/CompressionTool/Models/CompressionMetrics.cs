using System.Runtime.InteropServices;

namespace CompressionTool.Models;

public enum CompressionAlgorithm
{
    RLE = 0,
    Huffman = 1,
    LZW = 2
}

[StructLayout(LayoutKind.Sequential)]
public struct CompressionMetrics
{
    public ulong OriginalSizeBytes;
    public ulong CompressedSizeBytes;
    public double CompressionRatio;
    public double CompressionTimeMs;
    public double DecompressionTimeMs;
    public double CompressionSpeedMbps;
    public double DecompressionSpeedMbps;
    public int Success;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string ErrorMessage;
}

public class CompressionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ulong OriginalSizeBytes { get; set; }
    public ulong CompressedSizeBytes { get; set; }
    public double CompressionRatio { get; set; }
    public double CompressionTimeMs { get; set; }
    public double DecompressionTimeMs { get; set; }
    public double CompressionSpeedMbps { get; set; }
    public double DecompressionSpeedMbps { get; set; }

    public string OriginalSizeFormatted => FormatFileSize(OriginalSizeBytes);
    public string CompressedSizeFormatted => FormatFileSize(CompressedSizeBytes);
    public string CompressionRatioFormatted => $"{CompressionRatio:F1}%";
    public string CompressionTimeFormatted => $"{CompressionTimeMs:F2} ms";
    public string DecompressionTimeFormatted => $"{DecompressionTimeMs:F2} ms";
    public string CompressionSpeedFormatted => $"{CompressionSpeedMbps:F2} MB/s";
    public string DecompressionSpeedFormatted => $"{DecompressionSpeedMbps:F2} MB/s";

    private static string FormatFileSize(ulong bytes)
    {
        if (bytes == 0) return "0 B";

        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:F2} {sizes[order]}";
    }
}