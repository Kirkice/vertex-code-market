using System.ComponentModel;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

/// <summary>
/// MCP Tool: Export a specific asset to a file.
/// </summary>
[McpServerToolType]
public class ExportAssetTool
{
    private readonly AssetManagerService _assetManager;
    private readonly ExportService _exportService;

    public ExportAssetTool(AssetManagerService assetManager, ExportService exportService)
    {
        _assetManager = assetManager;
        _exportService = exportService;
    }

    [McpServerTool, Description("Export a specific asset to a file. The asset is identified by pathID and source file name (from list_assets or search_assets results). Supports exporting Texture2D (png/jpeg/bmp/tga), AudioClip (wav), Mesh (obj), Shader (.shader), TextAsset (.txt), MonoBehaviour (.json), Font (.ttf/.otf), Sprite, VideoClip, MovieTexture, and Animator (fbx).")]
    public string ExportAsset(
        [Description("The pathID of the asset to export.")] long pathId,
        [Description("The source file name of the asset.")] string sourceFile,
        [Description("The output directory path where the file will be saved.")] string outputPath,
        [Description("Optional: Output format for textures/sprites. Options: 'png', 'jpeg', 'bmp', 'tga'. Default is 'png'.")] string? format = null)
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

        // Get asset name for file naming
        var name = GetAssetName(obj);

        var result = _exportService.ExportAsset(obj, name, outputPath, format);

        var response = new
        {
            success = result.Success,
            message = result.Message,
            outputPath = result.OutputPath
        };

        return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string GetAssetName(AssetStudio.Object obj)
    {
        return obj switch
        {
            Texture2D tex => tex.m_Name,
            AudioClip audio => audio.m_Name,
            Mesh mesh => mesh.m_Name,
            Shader shader => shader.m_ParsedForm?.m_Name ?? shader.m_Name,
            TextAsset text => text.m_Name,
            Font font => font.m_Name,
            Sprite sprite => sprite.m_Name,
            VideoClip video => video.m_Name,
            MovieTexture movie => movie.m_Name,
            AnimationClip clip => clip.m_Name,
            Animator animator => animator.m_GameObject.TryGet(out var go) ? go.m_Name : "Animator",
            MonoBehaviour mb => mb.m_Name,
            NamedObject named => named.m_Name,
            _ => $"asset_{obj.m_PathID}"
        };
    }
}
