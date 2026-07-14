using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class GetBundleDependenciesTool
{
    private readonly AssetManagerService _assetManager;

    public GetBundleDependenciesTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Analyze AssetBundle dependencies. Returns external file references, container mappings, and preload table information. Useful for identifying redundant packaging, circular dependencies, and optimizing Bundle structure.")]
    public string GetBundleDependencies()
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var allAssets = _assetManager.GetAllAssets();
        var bundleAssets = allAssets.Where(a => a.Asset is AssetBundle).ToList();

        if (bundleAssets.Count == 0)
            return "No AssetBundle assets found in loaded files.";

        var bundles = new List<object>();

        foreach (var bundleInfo in bundleAssets)
        {
            var bundle = (AssetBundle)bundleInfo.Asset!;

            // External references
            var externals = bundle.assetsFile.m_Externals?.Select(ext => new
            {
                path = ext.pathName,
                guid = ext.guid,
                type = ext.type
            }).ToArray() ?? Array.Empty<object>();

            // Container entries
            var containers = bundle.m_Container?.Select(c => new
            {
                path = c.Key,
                preloadIndex = c.Value.preloadIndex,
                preloadSize = c.Value.preloadSize
            }).ToArray() ?? Array.Empty<object>();

            // Preload table
            var preloadTable = new List<object>();
            if (bundle.m_PreloadTable != null)
            {
                foreach (var ptr in bundle.m_PreloadTable)
                {
                    if (ptr.TryGet(out var obj))
                    {
                        var name = obj switch
                        {
                            NamedObject named => named.m_Name,
                            _ => obj.type.ToString()
                        };
                        preloadTable.Add(new
                        {
                            type = obj.type.ToString(),
                            name,
                            pathId = obj.m_PathID
                        });
                    }
                }
            }

            bundles.Add(new
            {
                name = bundle.m_Name,
                pathId = bundle.m_PathID,
                sourceFile = bundle.assetsFile.fileName,
                containerCount = bundle.m_Container?.Length ?? 0,
                preloadTableSize = bundle.m_PreloadTable?.Length ?? 0,
                externalReferenceCount = externals.Length,
                externals,
                containers,
                preloadTable = preloadTable.Take(50).ToArray()
            });
        }

        var result = new
        {
            totalBundles = bundles.Count,
            bundles
        };

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
