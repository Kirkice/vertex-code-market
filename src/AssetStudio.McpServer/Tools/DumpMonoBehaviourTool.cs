using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class DumpMonoBehaviourTool
{
    private readonly AssetManagerService _assetManager;

    public DumpMonoBehaviourTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Dump a MonoBehaviour asset to JSON. Attempts to deserialize using TypeTree data. If assembly directory is provided, uses Mono.Cecil to resolve types for deeper deserialization. Useful for extracting custom script data, post-processing parameters, and rendering configurations.")]
    public string DumpMonoBehaviour(
        [Description("The pathID of the MonoBehaviour asset.")] long pathId,
        [Description("The source file name of the MonoBehaviour asset.")] string sourceFile,
        [Description("Optional: Directory path containing managed assemblies (e.g., Managed folder) for type resolution.")] string? assemblyDir = null)
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var obj = _assetManager.FindAsset(pathId, sourceFile);
        if (obj is not MonoBehaviour monoBehaviour)
            return $"MonoBehaviour not found: pathID={pathId}, sourceFile={sourceFile}";

        var result = new Dictionary<string, object?>
        {
            ["name"] = monoBehaviour.m_Name,
            ["pathId"] = monoBehaviour.m_PathID,
            ["sourceFile"] = monoBehaviour.assetsFile.fileName
        };

        // Script reference
        if (monoBehaviour.m_Script.TryGet(out var script))
        {
            result["scriptClass"] = script.m_ClassName;
            result["scriptNamespace"] = script.m_Namespace;
            result["scriptAssembly"] = script.m_AssemblyName;
        }

        // Try TypeTree dump first
        var typeTree = monoBehaviour.ToType();
        if (typeTree != null)
        {
            var json = JsonConvert.SerializeObject(typeTree, Formatting.Indented);
            result["data"] = json.Length > 50000 ? json[..50000] + "\n... [truncated]" : json;
            result["method"] = "TypeTree";
        }
        else
        {
            // Try raw dump
            var dump = monoBehaviour.Dump();
            if (dump != null)
            {
                result["data"] = dump.Length > 50000 ? dump[..50000] + "\n... [truncated]" : dump;
                result["method"] = "TypeTree_Dump";
            }
            else
            {
                result["data"] = null;
                result["method"] = "none";
                result["message"] = "Cannot deserialize MonoBehaviour. TypeTree data not available. Provide assembly directory for Mono.Cecil deserialization.";
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
