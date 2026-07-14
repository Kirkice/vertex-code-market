using System.ComponentModel;
using System.IO.Compression;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class LoadApkTool
{
    private readonly AssetManagerService _assetManager;

    public LoadApkTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Load Unity assets from an APK file. Extracts the APK (ZIP format), scans for Unity asset files (bundles, .assets files) in assets/bin/Data/, and loads them. Returns information about extracted and loaded assets.")]
    public string LoadApk(
        [Description("Path to the APK file.")] string apkPath,
        [Description("Optional: Specific subdirectory within the APK to scan (e.g., 'assets/bin/Data'). Default scans common Unity asset locations.")] string? subDirectory = null)
    {
        if (!File.Exists(apkPath))
        {
            return $"Error: APK file not found: {apkPath}";
        }

        try
        {
            // Create temporary extraction directory
            var tempDir = Path.Combine(Path.GetTempPath(), "AssetStudio_APK_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);

            var extractedFiles = new List<string>();
            var assetFiles = new List<string>();

            // Extract APK
            using (var archive = ZipFile.OpenRead(apkPath))
            {
                foreach (var entry in archive.Entries)
                {
                    // Filter by subdirectory if specified
                    if (!string.IsNullOrEmpty(subDirectory))
                    {
                        if (!entry.FullName.StartsWith(subDirectory, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                    else
                    {
                        // Default: scan common Unity asset locations
                        if (!IsUnityAssetPath(entry.FullName))
                            continue;
                    }

                    // Skip directories
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    // Extract file
                    var extractPath = Path.Combine(tempDir, entry.FullName);
                    var extractDir = Path.GetDirectoryName(extractPath);
                    if (!string.IsNullOrEmpty(extractDir) && !Directory.Exists(extractDir))
                    {
                        Directory.CreateDirectory(extractDir);
                    }

                    entry.ExtractToFile(extractPath, overwrite: true);
                    extractedFiles.Add(entry.FullName);

                    // Check if it's a Unity asset file
                    if (IsUnityAssetFile(entry.FullName))
                    {
                        assetFiles.Add(extractPath);
                    }
                }
            }

            if (assetFiles.Count == 0)
            {
                // Cleanup
                try { Directory.Delete(tempDir, true); } catch { }
                return $"No Unity asset files found in APK. Extracted {extractedFiles.Count} files total. Try specifying a different subDirectory parameter.";
            }

            // Load the asset files
            var result = _assetManager.LoadFiles(assetFiles.ToArray());

            var response = new
            {
                success = result.Success,
                message = result.Message,
                apkPath,
                extractionDirectory = tempDir,
                totalExtractedFiles = extractedFiles.Count,
                unityAssetFilesFound = assetFiles.Count,
                assetFileNames = assetFiles.Select(f => Path.GetFileName(f)).ToArray(),
                serializedFileCount = result.SerializedFileCount,
                objectCount = result.ObjectCount,
                note = "Temporary extraction directory will be cleaned up on next load or server restart."
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            return $"Error loading APK: {ex.Message}";
        }
    }

    private static bool IsUnityAssetPath(string path)
    {
        // Common Unity asset locations in APK
        var patterns = new[]
        {
            "assets/bin/Data/",
            "assets/bin/Data/",
            "assets/",
        };

        return patterns.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsUnityAssetFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var fileName = Path.GetFileName(path).ToLowerInvariant();

        // Unity asset file extensions
        if (ext is ".bundle" or ".unity3d" or ".assets" or ".resource" or ".resS" or ".sharedassets")
            return true;

        // Files without extension that might be bundles
        if (string.IsNullOrEmpty(ext) && fileName.Contains("data"))
            return true;

        return false;
    }
}
