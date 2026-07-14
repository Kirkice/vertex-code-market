using System.ComponentModel;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

/// <summary>
/// MCP Tool: Get detailed information about a specific asset.
/// </summary>
[McpServerToolType]
public class GetAssetInfoTool
{
    private readonly AssetManagerService _assetManager;

    public GetAssetInfoTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Get detailed information about a specific asset identified by pathID and source file name. Returns type, size, container path, and type-specific properties.")]
    public string GetAssetInfo(
        [Description("The pathID of the asset (from list_assets results).")] long pathId,
        [Description("The source file name of the asset (from list_assets results).")] string sourceFile)
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

        var info = new Dictionary<string, object?>
        {
            ["pathId"] = obj.m_PathID,
            ["type"] = obj.type.ToString(),
            ["classId"] = (int)obj.type,
            ["byteSize"] = obj.byteSize,
            ["sourceFile"] = obj.assetsFile.fileName,
            ["unityVersion"] = obj.assetsFile.unityVersion,
            ["platform"] = obj.platform.ToString()
        };

        // Add type-specific info
        switch (obj)
        {
            case Texture2D tex:
                info["name"] = tex.m_Name;
                info["width"] = tex.m_Width;
                info["height"] = tex.m_Height;
                info["format"] = tex.m_TextureFormat.ToString();
                info["mipMap"] = tex.m_MipMap;
                break;
            case AudioClip audio:
                info["name"] = audio.m_Name;
                info["channels"] = audio.m_Channels;
                info["frequency"] = audio.m_Frequency;
                info["bitsPerSample"] = audio.m_BitsPerSample;
                info["source"] = audio.m_Source;
                break;
            case Mesh mesh:
                info["name"] = mesh.m_Name;
                info["vertexCount"] = mesh.m_VertexCount;
                info["subMeshCount"] = mesh.m_SubMeshes?.Length ?? 0;
                break;
            case Shader shader:
                info["name"] = shader.m_ParsedForm?.m_Name ?? shader.m_Name;
                break;
            case AnimationClip clip:
                info["name"] = clip.m_Name;
                info["legacy"] = clip.m_Legacy;
                break;
            case MonoBehaviour mb:
                info["name"] = mb.m_Name;
                info["script"] = mb.m_Script.TryGet(out var script) ? script.m_ClassName : "unknown";
                break;
            case Sprite sprite:
                info["name"] = sprite.m_Name;
                break;
            case Font font:
                info["name"] = font.m_Name;
                info["hasData"] = font.m_FontData != null;
                break;
            case TextAsset textAsset:
                info["name"] = textAsset.m_Name;
                info["scriptLength"] = textAsset.m_Script?.Length ?? 0;
                break;
            case VideoClip videoClip:
                info["name"] = videoClip.m_Name;
                info["originalPath"] = videoClip.m_OriginalPath;
                break;
            case Animator animator:
                info["name"] = animator.m_GameObject.TryGet(out var go) ? go.m_Name : "Animator";
                break;
            case AssetBundle bundle:
                info["name"] = bundle.m_Name;
                info["containerCount"] = bundle.m_Container.Length;
                break;
            case GameObject gameObject:
                info["name"] = gameObject.m_Name;
                break;
            default:
                if (obj is NamedObject named)
                    info["name"] = named.m_Name;
                break;
        }

        return System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
