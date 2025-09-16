using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CompressionTool.Models;

namespace CompressionTool.Services;

/// <summary>
/// Module initializer to set up native library loading before any static constructors run.
/// This fixes timing issues with DllImportResolver in Avalonia applications.
/// </summary>
internal static class NativeLibraryInitializer
{
    private const string LibraryName = "compression_lib";
    private static readonly List<string> LoadAttempts = new();
    private static bool LibraryLoadSuccessful = false;
    private static string? LoadedLibraryPath = null;

    [ModuleInitializer]
    internal static void Initialize()
    {
        Debug.WriteLine("[NativeLibraryInitializer] ModuleInitializer starting...");
        LoadAttempts.Add($"ModuleInitializer called at {DateTime.Now:HH:mm:ss.fff}");

        try
        {
            // Register the DLL import resolver before any P/Invoke calls
            NativeLibrary.SetDllImportResolver(typeof(CompressionService).Assembly, ImportResolver);
            LoadAttempts.Add("SetDllImportResolver registered successfully");
            Debug.WriteLine("[NativeLibraryInitializer] DllImportResolver registered successfully");
        }
        catch (Exception ex)
        {
            LoadAttempts.Add($"Failed to register DllImportResolver: {ex.Message}");
            Debug.WriteLine($"[NativeLibraryInitializer] Failed to register DllImportResolver: {ex.Message}");
        }
    }

    private static IntPtr ImportResolver(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == LibraryName)
        {
            Debug.WriteLine($"[NativeLibraryInitializer] ImportResolver called for: {libraryName}");
            LoadAttempts.Add($"ImportResolver called for {libraryName} at {DateTime.Now:HH:mm:ss.fff}");

            // Try to load the library from various paths
            string[] possiblePaths = GetPossibleLibraryPaths();

            foreach (string path in possiblePaths)
            {
                LoadAttempts.Add($"Trying: {path}");
                Debug.WriteLine($"[NativeLibraryInitializer] Trying to load: {path}");

                if (File.Exists(path))
                {
                    LoadAttempts.Add($"File exists: {path}");
                    Debug.WriteLine($"[NativeLibraryInitializer] File exists: {path}");

                    try
                    {
                        if (NativeLibrary.TryLoad(path, out IntPtr handle))
                        {
                            LoadAttempts.Add($"SUCCESS: Loaded {path}");
                            Debug.WriteLine($"[NativeLibraryInitializer] Successfully loaded: {path}");
                            LibraryLoadSuccessful = true;
                            LoadedLibraryPath = path;
                            return handle;
                        }
                        else
                        {
                            // Try to get more detailed error information
                            try
                            {
                                // This will throw a more specific exception
                                IntPtr testHandle = NativeLibrary.Load(path);
                                NativeLibrary.Free(testHandle);
                                LoadAttempts.Add($"STRANGE: TryLoad failed but Load succeeded for {path}");
                            }
                            catch (Exception ex)
                            {
                                LoadAttempts.Add($"FAILED: Could not load {path} - {ex.Message}");
                                Debug.WriteLine($"[NativeLibraryInitializer] Failed to load: {path} - {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoadAttempts.Add($"EXCEPTION: Error loading {path} - {ex.Message}");
                        Debug.WriteLine($"[NativeLibraryInitializer] Exception loading: {path} - {ex.Message}");
                    }
                }
                else
                {
                    LoadAttempts.Add($"File not found: {path}");
                    Debug.WriteLine($"[NativeLibraryInitializer] File not found: {path}");
                }
            }

            LoadAttempts.Add("FINAL FAILURE: Could not load library from any path");
            Debug.WriteLine("[NativeLibraryInitializer] Failed to load library from any path");
        }

        return IntPtr.Zero;
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

            // Add hardcoded fallback path
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

    /// <summary>
    /// Gets diagnostic information about library loading attempts.
    /// This can be called from CompressionService to get the loading status.
    /// </summary>
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
            diagnostics.Add("No load attempts recorded - ModuleInitializer may not have been called");
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
    /// Gets whether the library was successfully loaded during module initialization.
    /// </summary>
    public static bool IsLibraryLoaded => LibraryLoadSuccessful;
}