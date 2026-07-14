using System.ComponentModel;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

/// <summary>
/// MCP Tool: Load Unity asset files or folders into the asset manager.
/// </summary>
[McpServerToolType]
public class LoadAssetsTool
{
    private readonly AssetManagerService _assetManager;

    public LoadAssetsTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Load Unity asset files or folders. Supports .assets, .bundle, .unity3d, .web, .zip, .gz, .br files and folders containing them. Returns the number of serialized files and objects loaded.")]
    public string LoadAssets(
        [Description("Array of file paths or folder paths to load. Can be absolute or relative paths.")] string[] paths)
    {
        if (paths == null || paths.Length == 0)
        {
            return "Error: No paths provided. Please provide at least one file or folder path.";
        }

        var result = _assetManager.LoadFiles(paths);

        var response = new
        {
            success = result.Success,
            message = result.Message,
            serializedFileCount = result.SerializedFileCount,
            objectCount = result.ObjectCount,
            invalidPaths = result.InvalidPaths
        };

        return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
