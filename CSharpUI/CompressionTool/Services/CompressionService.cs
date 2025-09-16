using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CompressionTool.Models;

namespace CompressionTool.Services;

public class CompressionService
{
    private const string LibraryName = "compression_lib";
    private static readonly List<string> LoadAttempts = new();
    private static bool LibraryLoadSuccessful = false;
    private static string? LoadedLibraryPath = null;

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

    static CompressionService()
    {
        // Load the library at startup
        NativeLibrary.SetDllImportResolver(typeof(CompressionService).Assembly, ImportResolver);
    }

    private static IntPtr ImportResolver(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == LibraryName)
        {
            Debug.WriteLine($"[CompressionService] Attempting to load library: {libraryName}");

            // Try to load the library from various paths
            string[] possiblePaths = GetPossibleLibraryPaths();

            foreach (string path in possiblePaths)
            {
                LoadAttempts.Add($"Trying: {path}");
                Debug.WriteLine($"[CompressionService] Trying to load: {path}");

                if (File.Exists(path))
                {
                    LoadAttempts.Add($"File exists: {path}");
                    Debug.WriteLine($"[CompressionService] File exists: {path}");

                    if (NativeLibrary.TryLoad(path, out IntPtr handle))
                    {
                        LoadAttempts.Add($"SUCCESS: Loaded {path}");
                        Debug.WriteLine($"[CompressionService] Successfully loaded: {path}");
                        LibraryLoadSuccessful = true;
                        LoadedLibraryPath = path;
                        return handle;
                    }
                    else
                    {
                        LoadAttempts.Add($"FAILED: Could not load {path}");
                        Debug.WriteLine($"[CompressionService] Failed to load: {path}");
                    }
                }
                else
                {
                    LoadAttempts.Add($"File not found: {path}");
                    Debug.WriteLine($"[CompressionService] File not found: {path}");
                }
            }

            LoadAttempts.Add("FINAL FAILURE: Could not load library from any path");
            Debug.WriteLine("[CompressionService] Failed to load library from any path");
        }

        return IntPtr.Zero;
    }

    private static string[] GetPossibleLibraryPaths()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "build"));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new[]
            {
                Path.Combine(baseDir, "compression_lib.dll"),
                Path.Combine(projectRoot, "compression_lib.dll"),
                Path.Combine(projectRoot, "Debug", "compression_lib.dll"),
                Path.Combine(projectRoot, "Release", "compression_lib.dll")
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new[]
            {
                Path.Combine(baseDir, "libcompression_lib.so"),
                Path.Combine(projectRoot, "libcompression_lib.so"),
                "/home/robert/cpp-csharp-file-compression/build/libcompression_lib.so"
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new[]
            {
                Path.Combine(baseDir, "libcompression_lib.dylib"),
                Path.Combine(projectRoot, "libcompression_lib.dylib")
            };
        }

        return Array.Empty<string>();
    }

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
            if (!LibraryLoadSuccessful)
            {
                return new CompressionResult
                {
                    Success = false,
                    ErrorMessage = GetLibraryLoadDiagnostics()
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
                ErrorMessage = $"Compression library not found: {dllEx.Message}\n\nDiagnostics:\n{GetLibraryLoadDiagnostics()}"
            };
        }
        catch (Exception ex)
        {
            return new CompressionResult
            {
                Success = false,
                ErrorMessage = $"Error calling compression library: {ex.Message}\n\nDiagnostics:\n{GetLibraryLoadDiagnostics()}"
            };
        }
    }

    private CompressionResult DecompressFile(CompressionAlgorithm algorithm, string inputFile, string outputFile)
    {
        try
        {
            // Check if library was loaded successfully
            if (!LibraryLoadSuccessful)
            {
                return new CompressionResult
                {
                    Success = false,
                    ErrorMessage = GetLibraryLoadDiagnostics()
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
                ErrorMessage = $"Decompression library not found: {dllEx.Message}\n\nDiagnostics:\n{GetLibraryLoadDiagnostics()}"
            };
        }
        catch (Exception ex)
        {
            return new CompressionResult
            {
                Success = false,
                ErrorMessage = $"Error calling decompression library: {ex.Message}\n\nDiagnostics:\n{GetLibraryLoadDiagnostics()}"
            };
        }
    }

    public ulong GetFileSize(string filename)
    {
        try
        {
            // Check if library was loaded successfully before calling native function
            if (!LibraryLoadSuccessful)
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
            if (!LibraryLoadSuccessful)
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
            if (!LibraryLoadSuccessful)
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

    public static string GetLibraryLoadDiagnostics()
    {
        var diagnostics = new List<string>
        {
            $"Library Load Status: {(LibraryLoadSuccessful ? "SUCCESS" : "FAILED")}",
            $"Loaded Library Path: {LoadedLibraryPath ?? "None"}",
            $"Current Directory: {Environment.CurrentDirectory}",
            $"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}",
            $"OS Platform: {RuntimeInformation.OSDescription}",
            $"Runtime Identifier: {RuntimeInformation.RuntimeIdentifier}",
            $"Is Windows: {RuntimeInformation.IsOSPlatform(OSPlatform.Windows)}",
            $"Is Linux: {RuntimeInformation.IsOSPlatform(OSPlatform.Linux)}",
            $"Is macOS: {RuntimeInformation.IsOSPlatform(OSPlatform.OSX)}",
            "",
            "Load Attempts:"
        };

        if (LoadAttempts.Count == 0)
        {
            diagnostics.Add("No load attempts recorded - library resolver may not have been called");
        }
        else
        {
            foreach (string attempt in LoadAttempts)
            {
                diagnostics.Add($"  {attempt}");
            }
        }

        diagnostics.Add("");
        diagnostics.Add("Possible Library Paths:");
        foreach (string path in GetPossibleLibraryPaths())
        {
            bool exists = File.Exists(path);
            diagnostics.Add($"  {(exists ? "✓" : "✗")} {path}");
        }

        return string.Join("\n", diagnostics);
    }

    public static bool IsLibraryLoaded => LibraryLoadSuccessful;
}