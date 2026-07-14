using System.ComponentModel;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

/// <summary>
/// MCP Tool: List all loaded assets with optional filtering.
/// </summary>
[McpServerToolType]
public class ListAssetsTool
{
    private readonly AssetManagerService _assetManager;

    public ListAssetsTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("List all loaded assets. Optionally filter by asset type (e.g., Texture2D, AudioClip, Mesh, Shader, MonoBehaviour, etc.). Returns a summary list with name, type, pathID, source file, and size.")]
    public string ListAssets(
        [Description("Optional: Filter by asset type name (e.g., 'Texture2D', 'AudioClip', 'Mesh', 'Shader', 'MonoBehaviour', 'Sprite', 'Font', 'AnimationClip', 'Animator'). Leave empty to list all exportable assets.")] string? type = null,
        [Description("Optional: If true, include non-exportable assets (like GameObject, Transform, etc.). Default is false.")] bool includeAll = false)
    {
        if (!_assetManager.IsLoaded)
        {
            return "No assets loaded. Use the 'load_assets' tool first to load Unity asset files.";
        }

        var assets = includeAll ? _assetManager.GetAllAssets() : _assetManager.GetExportableAssets();

        // Apply type filter
        if (!string.IsNullOrEmpty(type))
        {
            assets = assets.Where(a => a.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var summary = assets.Select(a => new
        {
            name = a.Name,
            type = a.Type,
            pathId = a.PathID,
            sourceFile = a.SourceFile,
            container = a.Container,
            size = a.ByteSize,
            isExportable = a.IsExportable
        }).ToList();

        var response = new
        {
            totalCount = summary.Count,
            assets = summary
        };

        return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
