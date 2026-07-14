using System.ComponentModel;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

/// <summary>
/// MCP Tool: Get the TypeTree structure of a specific asset.
/// </summary>
[McpServerToolType]
public class GetTypeTreeTool
{
    private readonly AssetManagerService _assetManager;

    public GetTypeTreeTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Get the TypeTree dump of a specific asset. The TypeTree shows the serialized field structure of the asset, which is useful for understanding its data layout. Returns the TypeTree as a formatted string.")]
    public string GetTypeTree(
        [Description("The pathID of the asset.")] long pathId,
        [Description("The source file name of the asset.")] string sourceFile)
    {
        if (!_assetManager.IsLoaded)
        {
            return "No assets loaded. Use the 'load_assets' tool first.";
        }

        var obj = _assetManager.FindAsset(pathId, sourceFile);
        if (obj == null)
        {
            return $"Asset not found: pathID={pathId}, sourceFile={sourceFile}";
        }

        var dump = obj.Dump();
        if (dump == null)
        {
            return $"TypeTree is not available for this asset (type: {obj.type}). The asset may not have TypeTree data in this serialized file.";
        }

        var response = new
        {
            pathId = obj.m_PathID,
            type = obj.type.ToString(),
            sourceFile = obj.assetsFile.fileName,
            typeTree = dump
        };

        return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
