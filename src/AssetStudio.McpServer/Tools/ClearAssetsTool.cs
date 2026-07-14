using System.ComponentModel;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class ClearAssetsTool
{
    private readonly AssetManagerService _assetManager;

    public ClearAssetsTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Clear all loaded assets and free memory. Use this before loading a different set of assets to avoid memory accumulation.")]
    public string ClearAssets()
    {
        var wasLoaded = _assetManager.IsLoaded;
        _assetManager.Clear();

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var result = new
        {
            success = true,
            message = wasLoaded ? "All assets cleared and memory freed." : "No assets were loaded.",
            gcCollected = true
        };

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
