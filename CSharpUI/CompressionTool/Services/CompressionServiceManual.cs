using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CompressionTool.Models;

namespace CompressionTool.Services;

/// <summary>
/// Compression service that uses manual native library loading instead of DllImport.
/// This bypasses single-file deployment issues where DllImportResolver is not called.
/// </summary>
public class CompressionServiceManual
{
    private const string LibraryName = "compression_lib";
    private static readonly List<string> LoadAttempts = new();
    private static bool LibraryLoadSuccessful = false;
    private static string? LoadedLibraryPath = null;
    private static IntPtr LibraryHandle = IntPtr.Zero;

    // Function pointer delegates
    private delegate int CompressFileDelegate(
        CompressionAlgorithm algorithm,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string inputFile,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string outputFile,
        ref CompressionMetrics metrics
    );

    private delegate int DecompressFileDelegate(
        CompressionAlgorithm algorithm,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string inputFile,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string outputFile,
        ref CompressionMetrics metrics
    );

    private delegate int GetFileSizeDelegate(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string filename,
        ref ulong size
    );

    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    private delegate string GetAlgorithmNameDelegate(CompressionAlgorithm algorithm);

    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    private delegate string GetLastErrorDelegate();

    // Function pointers
    private static CompressFileDelegate? compress_file;
    private static DecompressFileDelegate? decompress_file;
    private static GetFileSizeDelegate? get_file_size;
    private static GetAlgorithmNameDelegate? get_algorithm_name;
    private static GetLastErrorDelegate? get_last_error;

    static CompressionServiceManual()
    {
        LoadNativeLibrary();
    }

    private static void LoadNativeLibrary()
    {
        Debug.WriteLine("[CompressionServiceManual] Starting manual library loading...");
        LoadAttempts.Add($"Manual library loading started at {DateTime.Now:HH:mm:ss.fff}");

        try
        {
            // Get possible library paths
            string[] possiblePaths = GetPossibleLibraryPaths();

            foreach (string path in possiblePaths)
            {
                LoadAttempts.Add($"Trying: {path}");
                Debug.WriteLine($"[CompressionServiceManual] Trying to load: {path}");

                if (File.Exists(path))
                {
                    LoadAttempts.Add($"File exists: {path}");
                    Debug.WriteLine($"[CompressionServiceManual] File exists: {path}");

                    try
                    {
                        // Use NativeLibrary.Load directly - this bypasses DllImportResolver
                        LibraryHandle = NativeLibrary.Load(path);

                        if (LibraryHandle != IntPtr.Zero)
                        {
                            LoadAttempts.Add($"SUCCESS: Loaded {path}");
                            Debug.WriteLine($"[CompressionServiceManual] Successfully loaded: {path}");
                            LoadedLibraryPath = path;

                            // Load function pointers
                            if (LoadFunctionPointers())
                            {
                                LibraryLoadSuccessful = true;
                                LoadAttempts.Add("All function pointers loaded successfully");
                                Debug.WriteLine("[CompressionServiceManual] All function pointers loaded successfully");
                                return;
                            }
                            else
                            {
                                LoadAttempts.Add("Failed to load function pointers");
                                Debug.WriteLine("[CompressionServiceManual] Failed to load function pointers");
                                NativeLibrary.Free(LibraryHandle);
                                LibraryHandle = IntPtr.Zero;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoadAttempts.Add($"EXCEPTION: Error loading {path} - {ex.Message}");
                        Debug.WriteLine($"[CompressionServiceManual] Exception loading: {path} - {ex.Message}");
                    }
                }
                else
                {
                    LoadAttempts.Add($"File not found: {path}");
                    Debug.WriteLine($"[CompressionServiceManual] File not found: {path}");
                }
            }

            LoadAttempts.Add("FINAL FAILURE: Could not load library from any path");
            Debug.WriteLine("[CompressionServiceManual] Failed to load library from any path");
        }
        catch (Exception ex)
        {
            LoadAttempts.Add($"CRITICAL ERROR: {ex.Message}");
            Debug.WriteLine($"[CompressionServiceManual] Critical error: {ex.Message}");
        }
    }

    private static bool LoadFunctionPointers()
    {
        try
        {
            // Load each function pointer
            IntPtr compressPtr = NativeLibrary.GetExport(LibraryHandle, "compress_file");
            compress_file = Marshal.GetDelegateForFunctionPointer<CompressFileDelegate>(compressPtr);

            IntPtr decompressPtr = NativeLibrary.GetExport(LibraryHandle, "decompress_file");
            decompress_file = Marshal.GetDelegateForFunctionPointer<DecompressFileDelegate>(decompressPtr);

            IntPtr fileSizePtr = NativeLibrary.GetExport(LibraryHandle, "get_file_size");
            get_file_size = Marshal.GetDelegateForFunctionPointer<GetFileSizeDelegate>(fileSizePtr);

            IntPtr algorithmNamePtr = NativeLibrary.GetExport(LibraryHandle, "get_algorithm_name");
            get_algorithm_name = Marshal.GetDelegateForFunctionPointer<GetAlgorithmNameDelegate>(algorithmNamePtr);

            IntPtr lastErrorPtr = NativeLibrary.GetExport(LibraryHandle, "get_last_error");
            get_last_error = Marshal.GetDelegateForFunctionPointer<GetLastErrorDelegate>(lastErrorPtr);

            return true;
        }
        catch (Exception ex)
        {
            LoadAttempts.Add($"Function pointer loading failed: {ex.Message}");
            Debug.WriteLine($"[CompressionServiceManual] Function pointer loading failed: {ex.Message}");
            return false;
        }
    }

    private static string[] GetPossibleLibraryPaths()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "build"));

        // Get more potential project root paths
        List<string> possibleProjectRoots = new()
        {
            projectRoot,
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "build-windows")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "build-windows-fixed")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "dist", "MultiAlgorithmCompressionTool-StaticLinked-v3.0")),
            Environment.CurrentDirectory,
            Path.Combine(Environment.CurrentDirectory, "build"),
            Path.Combine(Environment.CurrentDirectory, "build-windows"),
            Path.Combine(Environment.CurrentDirectory, "build-windows-fixed")
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            List<string> paths = new()
            {
                Path.Combine(baseDir, "compression_lib.dll")
            };

            // Add all possible project root combinations
            foreach (string root in possibleProjectRoots)
            {
                paths.AddRange(new[]
                {
                    Path.Combine(root, "compression_lib.dll"),
                    Path.Combine(root, "libcompression_lib.dll"),
                    Path.Combine(root, "Debug", "compression_lib.dll"),
                    Path.Combine(root, "Release", "compression_lib.dll"),
                    Path.Combine(root, "build", "compression_lib.dll"),
                    Path.Combine(root, "build", "Debug", "compression_lib.dll"),
                    Path.Combine(root, "build", "Release", "compression_lib.dll")
                });
            }

            return paths.Distinct().ToArray();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            List<string> paths = new()
            {
                Path.Combine(baseDir, "libcompression_lib.so")
            };

            foreach (string root in possibleProjectRoots)
            {
                paths.AddRange(new[]
                {
                    Path.Combine(root, "libcompression_lib.so"),
                    Path.Combine(root, "build", "libcompression_lib.so")
                });
            }

            // Add hardcoded fallback paths
            paths.Add("/home/robert/cpp-csharp-file-compression/build/libcompression_lib.so");
            paths.Add("/home/robert/cpp-csharp-file-compression/build-fixed/libcompression_lib.so");

            return paths.Distinct().ToArray();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            List<string> paths = new()
            {
                Path.Combine(baseDir, "libcompression_lib.dylib")
            };

            foreach (string root in possibleProjectRoots)
            {
                paths.AddRange(new[]
                {
                    Path.Combine(root, "libcompression_lib.dylib"),
                    Path.Combine(root, "build", "libcompression_lib.dylib")
                });
            }

            return paths.Distinct().ToArray();
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
            if (!LibraryLoadSuccessful || compress_file == null)
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
            if (!LibraryLoadSuccessful || decompress_file == null)
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
            if (!LibraryLoadSuccessful || get_file_size == null)
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
        // Always use fallback algorithm names to avoid UI thread blocking
        // Native function calls should be avoided in UI context
        return algorithm switch
        {
            CompressionAlgorithm.RLE => "Run-Length Encoding",
            CompressionAlgorithm.Huffman => "Huffman Coding",
            CompressionAlgorithm.LZW => "LZW",
            _ => "Unknown"
        };
    }

    public string GetLastError()
    {
        try
        {
            // Check if library was loaded successfully before calling native function
            if (!LibraryLoadSuccessful || get_last_error == null)
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
    public static string GetLibraryLoadDiagnostics()
    {
        var diagnostics = new List<string>
        {
            $"Manual Library Load Status: {(LibraryLoadSuccessful ? "SUCCESS" : "FAILED")}",
            $"Loaded Library Path: {LoadedLibraryPath ?? "None"}",
            $"Library Handle: {LibraryHandle}",
            $"Current Directory: {Environment.CurrentDirectory}",
            $"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}",
            $"OS Platform: {RuntimeInformation.OSDescription}",
            $"Runtime Identifier: {RuntimeInformation.RuntimeIdentifier}",
            $"Is Windows: {RuntimeInformation.IsOSPlatform(OSPlatform.Windows)}",
            $"Is Linux: {RuntimeInformation.IsOSPlatform(OSPlatform.Linux)}",
            $"Is macOS: {RuntimeInformation.IsOSPlatform(OSPlatform.OSX)}",
            "",
            "Manual Load Attempts:"
        };

        if (LoadAttempts.Count == 0)
        {
            diagnostics.Add("No load attempts recorded - static constructor may not have been called");
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

    /// <summary>
    /// Gets whether the native library was successfully loaded.
    /// </summary>
    public static bool IsLibraryLoaded => LibraryLoadSuccessful;
}