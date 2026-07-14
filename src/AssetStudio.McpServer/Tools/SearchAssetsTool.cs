using System.ComponentModel;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

/// <summary>
/// MCP Tool: Search assets by name or type.
/// </summary>
[McpServerToolType]
public class SearchAssetsTool
{
    private readonly AssetManagerService _assetManager;

    public SearchAssetsTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Search loaded assets by name pattern or type. Supports partial name matching (case-insensitive). Returns matching assets with their name, type, pathID, and source file.")]
    public string SearchAssets(
        [Description("Search query string. Matches against asset names (case-insensitive, partial match).")] string query,
        [Description("Optional: Filter by asset type name (e.g., 'Texture2D', 'AudioClip'). Leave empty to search all types.")] string? type = null,
        [Description("Optional: Maximum number of results to return. Default is 50.")] int maxResults = 50)
    {
        if (!_assetManager.IsLoaded)
        {
            return "No assets loaded. Use the 'load_assets' tool first.";
        }

        var allAssets = _assetManager.GetAllAssets();

        // Filter by name
        var filtered = allAssets.Where(a =>
            a.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

        // Filter by type
        if (!string.IsNullOrEmpty(type))
        {
            filtered = filtered.Where(a =>
                a.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        var results = filtered.Take(maxResults).Select(a => new
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
            query,
            typeFilter = type,
            totalMatches = filtered.Count(),
            returnedCount = results.Count,
            results
        };

        return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
