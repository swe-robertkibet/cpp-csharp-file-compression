using System;
using CompressionTool.Models;
using CompressionTool.Services;

namespace CompressionTool;

public class TestConsole
{
    public static void RunTest()
    {
        Console.WriteLine("=== File Compression Tool - Console Test ===\n");

        var compressionService = new CompressionService();
        string inputFile = "/tmp/test_file.txt";

        var algorithms = new[]
        {
            (CompressionAlgorithm.LZW, "LZW", ".lzw"),
            (CompressionAlgorithm.RLE, "RLE", ".rle"),
            (CompressionAlgorithm.Huffman, "Huffman", ".huf")
        };

        Console.WriteLine($"Input file: {inputFile}");
        var originalContent = System.IO.File.ReadAllText(inputFile);
        Console.WriteLine($"Original content: \"{originalContent.Substring(0, Math.Min(50, originalContent.Length))}...\"\n");

        foreach (var (algorithm, name, extension) in algorithms)
        {
            Console.WriteLine($"=== Testing {name} Algorithm ===");

            string outputFile = $"/tmp/test_compressed{extension}";
            string decompressedFile = $"/tmp/test_decompressed_{name.ToLower()}.txt";

            try
            {
                // Test compression
                Console.WriteLine($"Compressing with {name}...");
                var compressionResult = compressionService.CompressFileAsync(
                    algorithm, inputFile, outputFile).Result;

                Console.WriteLine($"Compression Result:");
                Console.WriteLine($"  Success: {compressionResult.Success}");
                if (compressionResult.Success)
                {
                    Console.WriteLine($"  Original size: {compressionResult.OriginalSizeFormatted}");
                    Console.WriteLine($"  Compressed size: {compressionResult.CompressedSizeFormatted}");
                    Console.WriteLine($"  Compression ratio: {compressionResult.CompressionRatioFormatted}");
                    Console.WriteLine($"  Time: {compressionResult.CompressionTimeFormatted}");
                    Console.WriteLine($"  Speed: {compressionResult.CompressionSpeedFormatted}");
                }
                else
                {
                    Console.WriteLine($"  Error: {compressionResult.ErrorMessage}");
                    continue;
                }

                // Test decompression
                Console.WriteLine($"Decompressing with {name}...");
                var decompressionResult = compressionService.DecompressFileAsync(
                    algorithm, outputFile, decompressedFile).Result;

                Console.WriteLine($"Decompression Result:");
                Console.WriteLine($"  Success: {decompressionResult.Success}");
                if (decompressionResult.Success)
                {
                    Console.WriteLine($"  Time: {decompressionResult.DecompressionTimeFormatted}");
                    Console.WriteLine($"  Speed: {decompressionResult.DecompressionSpeedFormatted}");

                    // Verify files are identical
                    string decompressedContent = System.IO.File.ReadAllText(decompressedFile);
                    bool filesMatch = originalContent == decompressedContent;
                    Console.WriteLine($"  File integrity: {(filesMatch ? "✓ PASSED" : "✗ FAILED")}");
                }
                else
                {
                    Console.WriteLine($"  Error: {decompressionResult.ErrorMessage}");
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed for {name}: {ex.Message}");
                Console.WriteLine();
            }
        }

        Console.WriteLine("=== All Tests Completed ===");
    }
}