using System;
using System.IO;
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

    static CompressionService()
    {
        // Load the library at startup
        NativeLibrary.SetDllImportResolver(typeof(CompressionService).Assembly, ImportResolver);
    }

    private static IntPtr ImportResolver(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == LibraryName)
        {
            // Try to load the library from various paths
            string[] possiblePaths = GetPossibleLibraryPaths();

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    if (NativeLibrary.TryLoad(path, out IntPtr handle))
                    {
                        return handle;
                    }
                }
            }
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
                ErrorMessage = $"Error calling compression library: {ex.Message}"
            };
        }
    }

    private CompressionResult DecompressFile(CompressionAlgorithm algorithm, string inputFile, string outputFile)
    {
        try
        {
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
                ErrorMessage = $"Error calling decompression library: {ex.Message}"
            };
        }
    }

    public ulong GetFileSize(string filename)
    {
        try
        {
            ulong size = 0;
            get_file_size(filename, ref size);
            return size;
        }
        catch
        {
            return 0;
        }
    }

    public string GetAlgorithmName(CompressionAlgorithm algorithm)
    {
        try
        {
            return get_algorithm_name(algorithm);
        }
        catch
        {
            return "Unknown";
        }
    }

    public string GetLastError()
    {
        try
        {
            return get_last_error();
        }
        catch
        {
            return "Unable to retrieve error message";
        }
    }
}