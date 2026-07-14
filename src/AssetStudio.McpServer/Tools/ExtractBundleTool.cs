using System.ComponentModel;
using AssetStudio;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

/// <summary>
/// MCP Tool: Extract/decompress AssetBundle files to a directory.
/// </summary>
[McpServerToolType]
public class ExtractBundleTool
{
    [McpServerTool, Description("Extract/decompress AssetBundle or WebFile to a directory. This decompresses the bundle and writes the inner files to disk without loading them into memory. Useful for large bundles that you want to inspect or load selectively afterwards.")]
    public string ExtractBundle(
        [Description("Path to the AssetBundle or WebFile to extract.")] string inputPath,
        [Description("Output directory where extracted files will be saved.")] string outputPath)
    {
        if (!File.Exists(inputPath))
        {
            return $"Error: Input file not found: {inputPath}";
        }

        try
        {
            var reader = new FileReader(inputPath);
            int extractedCount = 0;

            if (reader.FileType == FileType.BundleFile)
            {
                var bundleFile = new BundleFile(reader);
                reader.Dispose();

                if (bundleFile.fileList.Length > 0)
                {
                    var extractPath = Path.Combine(outputPath, Path.GetFileName(inputPath) + "_unpacked");
                    extractedCount = ExtractStreamFiles(extractPath, bundleFile.fileList);
                }
                else
                {
                    return "Bundle file contains no inner files.";
                }
            }
            else if (reader.FileType == FileType.WebFile)
            {
                var webFile = new WebFile(reader);
                reader.Dispose();

                if (webFile.fileList.Length > 0)
                {
                    var extractPath = Path.Combine(outputPath, Path.GetFileName(inputPath) + "_unpacked");
                    extractedCount = ExtractStreamFiles(extractPath, webFile.fileList);
                }
                else
                {
                    return "Web file contains no inner files.";
                }
            }
            else
            {
                reader.Dispose();
                return $"Error: File is not a BundleFile or WebFile. Detected type: {reader.FileType}";
            }

            var response = new
            {
                success = true,
                message = $"Extracted {extractedCount} file(s) to {outputPath}",
                extractedCount,
                outputPath
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            return $"Error extracting bundle: {ex.Message}";
        }
    }

    private static int ExtractStreamFiles(string extractPath, StreamFile[] fileList)
    {
        int extractedCount = 0;
        foreach (var file in fileList)
        {
            var filePath = Path.Combine(extractPath, file.path);
            var fileDirectory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDirectory) && !Directory.Exists(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
            }
            if (!File.Exists(filePath))
            {
                using var fileStream = File.Create(filePath);
                file.stream.CopyTo(fileStream);
                extractedCount++;
            }
            file.stream.Dispose();
        }
        return extractedCount;
    }
}
