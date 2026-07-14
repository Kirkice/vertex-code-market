using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class GetAssetStatsTool
{
    private readonly AssetManagerService _assetManager;

    public GetAssetStatsTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Get statistics of all loaded assets. Returns type distribution, count per type, total size per type, and overall summary. Useful for quickly understanding the content of a Bundle and identifying asset composition.")]
    public string GetAssetStats()
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var allAssets = _assetManager.GetAllAssets();

        // Group by type
        var typeStats = allAssets
            .GroupBy(a => a.Type)
            .Select(g => new
            {
                type = g.Key,
                count = g.Count(),
                totalSizeBytes = g.Sum(a => a.ByteSize),
                totalSizeMB = Math.Round(g.Sum(a => a.ByteSize) / (1024.0 * 1024.0), 2),
                exportableCount = g.Count(a => a.IsExportable),
                avgSizeBytes = (long)g.Average(a => a.ByteSize)
            })
            .OrderByDescending(s => s.count)
            .ToArray();

        // Source file stats
        var fileStats = allAssets
            .GroupBy(a => a.SourceFile)
            .Select(g => new
            {
                sourceFile = g.Key,
                objectCount = g.Count(),
                totalSizeBytes = g.Sum(a => a.ByteSize),
                totalSizeMB = Math.Round(g.Sum(a => a.ByteSize) / (1024.0 * 1024.0), 2),
                types = g.Select(a => a.Type).Distinct().Count()
            })
            .OrderByDescending(f => f.objectCount)
            .ToArray();

        // Container stats
        var containerStats = allAssets
            .Where(a => !string.IsNullOrEmpty(a.Container))
            .GroupBy(a => a.Container)
            .Select(g => new
            {
                container = g.Key,
                count = g.Count(),
                totalSizeBytes = g.Sum(a => a.ByteSize)
            })
            .OrderByDescending(c => c.count)
            .Take(20)
            .ToArray();

        var result = new
        {
            summary = new
            {
                totalObjects = allAssets.Count,
                exportableObjects = allAssets.Count(a => a.IsExportable),
                totalSizeBytes = allAssets.Sum(a => a.ByteSize),
                totalSizeMB = Math.Round(allAssets.Sum(a => a.ByteSize) / (1024.0 * 1024.0), 2),
                typeCount = typeStats.Length,
                sourceFileCount = fileStats.Length
            },
            byType = typeStats,
            bySourceFile = fileStats,
            topContainers = containerStats
        };

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
