using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CompressionTool.Models;

namespace CompressionTool.Services;

public class CompressionService
{
    private const string LibraryName = "compression_lib";

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int compress_file(
        CompressionAlgorithm algorithm,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string inputFile,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string outputFile,
        ref CompressionMetrics metrics
    );

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int decompress_file(
        CompressionAlgorithm algorithm,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string inputFile,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string outputFile,
        ref CompressionMetrics metrics
    );

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int get_file_size(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string filename,
        ref ulong size
    );

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    private static extern string get_algorithm_name(CompressionAlgorithm algorithm);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    private static extern string get_last_error();



    public async Task<CompressionResult> CompressFileAsync(CompressionAlgorithm algorithm, string inputFile, string outputFile)
    {
        return await Task.Run(() => CompressFile(algorithm, inputFile, outputFile));
    }

    public async Task<CompressionResult> DecompressFileAsync(CompressionAlgorithm algorithm, string inputFile, string outputFile)
    {
        return await Task.Run(() => DecompressFile(algorithm, inputFile, outputFile));
    }

    private CompressionResult CompressFile(CompressionAlgorithm algorithm, string inputFile, string outputFile)
    {
        try
        {
            // Check if library was loaded successfully
            if (!NativeLibraryInitializer.IsLibraryLoaded)
            {
                return new CompressionResult
                {
                    Success = false,
                    ErrorMessage = NativeLibraryInitializer.GetLibraryLoadDiagnostics()
                };
            }

            var metrics = new CompressionMetrics();
            int result = compress_file(algorithm, inputFile, outputFile, ref metrics);

            return new CompressionResult
            {
                Success = result != 0 && metrics.Success != 0,
                ErrorMessage = metrics.Success == 0 ? metrics.ErrorMessage : null,
                OriginalSizeBytes = metrics.OriginalSizeBytes,
                CompressedSizeBytes = metrics.CompressedSizeBytes,
                CompressionRatio = metrics.CompressionRatio,
                CompressionTimeMs = metrics.CompressionTimeMs,
                DecompressionTimeMs = 0,
                CompressionSpeedMbps = metrics.CompressionSpeedMbps,
                DecompressionSpeedMbps = 0
            };
        }
        catch (DllNotFoundException dllEx)
        {
            return new CompressionResult
            {
                Success = false,
                ErrorMessage = $"Compression library not found: {dllEx.Message}\n\nDiagnostics:\n{NativeLibraryInitializer.GetLibraryLoadDiagnostics()}"
            };
        }
        catch (Exception ex)
        {
            return new CompressionResult
            {
                Success = false,
                ErrorMessage = $"Error calling compression library: {ex.Message}\n\nDiagnostics:\n{NativeLibraryInitializer.GetLibraryLoadDiagnostics()}"
            };
        }
    }

    private CompressionResult DecompressFile(CompressionAlgorithm algorithm, string inputFile, string outputFile)
    {
        try
        {
            // Check if library was loaded successfully
            if (!NativeLibraryInitializer.IsLibraryLoaded)
            {
                return new CompressionResult
                {
                    Success = false,
                    ErrorMessage = NativeLibraryInitializer.GetLibraryLoadDiagnostics()
                };
            }

            var metrics = new CompressionMetrics();
            int result = decompress_file(algorithm, inputFile, outputFile, ref metrics);

            return new CompressionResult
            {
                Success = result != 0 && metrics.Success != 0,
                ErrorMessage = metrics.Success == 0 ? metrics.ErrorMessage : null,
                OriginalSizeBytes = metrics.OriginalSizeBytes,
                CompressedSizeBytes = metrics.CompressedSizeBytes,
                CompressionRatio = metrics.CompressionRatio,
                CompressionTimeMs = 0,
                DecompressionTimeMs = metrics.DecompressionTimeMs,
                CompressionSpeedMbps = 0,
                DecompressionSpeedMbps = metrics.DecompressionSpeedMbps
            };
        }
        catch (DllNotFoundException dllEx)
        {
            return new CompressionResult
            {
                Success = false,
                ErrorMessage = $"Decompression library not found: {dllEx.Message}\n\nDiagnostics:\n{NativeLibraryInitializer.GetLibraryLoadDiagnostics()}"
            };
        }
        catch (Exception ex)
        {
            return new CompressionResult
            {
                Success = false,
                ErrorMessage = $"Error calling decompression library: {ex.Message}\n\nDiagnostics:\n{NativeLibraryInitializer.GetLibraryLoadDiagnostics()}"
            };
        }
    }

    public ulong GetFileSize(string filename)
    {
        try
        {
            // Check if library was loaded successfully before calling native function
            if (!NativeLibraryInitializer.IsLibraryLoaded)
            {
                // Fallback to .NET file size calculation
                if (File.Exists(filename))
                {
                    var fileInfo = new FileInfo(filename);
                    return (ulong)fileInfo.Length;
                }
                return 0;
            }

            ulong size = 0;
            get_file_size(filename, ref size);
            return size;
        }
        catch
        {
            // Fallback to .NET file size calculation on error
            try
            {
                if (File.Exists(filename))
                {
                    var fileInfo = new FileInfo(filename);
                    return (ulong)fileInfo.Length;
                }
            }
            catch
            {
                // Ignore fallback errors
            }
            return 0;
        }
    }

    public string GetAlgorithmName(CompressionAlgorithm algorithm)
    {
        try
        {
            // Check if library was loaded successfully before calling native function
            if (!NativeLibraryInitializer.IsLibraryLoaded)
            {
                // Return fallback algorithm names when library isn't loaded
                return algorithm switch
                {
                    CompressionAlgorithm.RLE => "Run-Length Encoding",
                    CompressionAlgorithm.Huffman => "Huffman Coding",
                    CompressionAlgorithm.LZW => "LZW",
                    _ => "Unknown"
                };
            }

            return get_algorithm_name(algorithm);
        }
        catch
        {
            // Fallback to built-in algorithm names if native call fails
            return algorithm switch
            {
                CompressionAlgorithm.RLE => "Run-Length Encoding",
                CompressionAlgorithm.Huffman => "Huffman Coding",
                CompressionAlgorithm.LZW => "LZW",
                _ => "Unknown"
            };
        }
    }

    public string GetLastError()
    {
        try
        {
            // Check if library was loaded successfully before calling native function
            if (!NativeLibraryInitializer.IsLibraryLoaded)
            {
                return "Compression library not loaded";
            }

            return get_last_error();
        }
        catch
        {
            return "Unable to retrieve error message";
        }
    }

    /// <summary>
    /// Gets detailed diagnostic information about the compression service and library loading.
    /// </summary>
    public static string GetServiceDiagnostics()
    {
        return NativeLibraryInitializer.GetLibraryLoadDiagnostics();
    }

    /// <summary>
    /// Gets whether the native library was successfully loaded.
    /// </summary>
    public static bool IsLibraryLoaded => NativeLibraryInitializer.IsLibraryLoaded;
}