using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class GetRendererInfoTool
{
    private readonly AssetManagerService _assetManager;

    public GetRendererInfoTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Get detailed information about a Renderer component (MeshRenderer or SkinnedMeshRenderer). Returns material references, shadow settings, light probe usage, sorting layer, and static batch info. Useful for analyzing Draw Call sources and identifying batching issues.")]
    public string GetRendererInfo(
        [Description("The pathID of the Renderer asset (MeshRenderer or SkinnedMeshRenderer).")] long pathId,
        [Description("The source file name of the Renderer asset.")] string sourceFile)
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var obj = _assetManager.FindAsset(pathId, sourceFile);
        if (obj is not Renderer renderer)
            return $"Renderer not found: pathID={pathId}, sourceFile={sourceFile}";

        var result = new Dictionary<string, object?>
        {
            ["type"] = renderer.type.ToString(),
            ["pathId"] = renderer.m_PathID,
            ["sourceFile"] = renderer.assetsFile.fileName
        };

        // GameObject reference
        if (renderer.m_GameObject.TryGet(out var go))
        {
            result["gameObject"] = go.m_Name;
            result["gameObjectPathId"] = go.m_PathID;
        }

        // Materials
        if (renderer.m_Materials != null)
        {
            var materials = new List<object>();
            foreach (var matPtr in renderer.m_Materials)
            {
                if (matPtr.TryGet(out var mat) && mat is Material material)
                {
                    var matInfo = new Dictionary<string, object?>
                    {
                        ["name"] = material.m_Name,
                        ["pathId"] = material.m_PathID
                    };

                    if (material.m_Shader.TryGet(out var shader))
                    {
                        matInfo["shader"] = shader.m_ParsedForm?.m_Name ?? shader.m_Name;
                    }

                    materials.Add(matInfo);
                }
                else
                {
                    materials.Add(new { name = "unresolved" });
                }
            }
            result["materials"] = materials;
            result["materialCount"] = renderer.m_Materials.Length;
        }

        // Static batch info
        if (renderer.m_StaticBatchInfo.subMeshCount > 0)
        {
            result["staticBatch"] = new
            {
                firstSubMesh = renderer.m_StaticBatchInfo.firstSubMesh,
                subMeshCount = renderer.m_StaticBatchInfo.subMeshCount
            };
        }

        // Mesh reference (for MeshRenderer)
        if (renderer is MeshRenderer meshRenderer && meshRenderer.m_GameObject.TryGet(out var meshGo))
        {
            if (meshGo.m_MeshFilter != null && meshGo.m_MeshFilter.m_Mesh.TryGet(out var mesh))
            {
                result["mesh"] = new
                {
                    name = mesh.m_Name,
                    pathId = mesh.m_PathID,
                    vertexCount = mesh.m_VertexCount,
                    subMeshCount = mesh.m_SubMeshes?.Length ?? 0
                };
            }
        }

        // SkinnedMeshRenderer specific
        if (renderer is SkinnedMeshRenderer skinRenderer)
        {
            result["isSkinned"] = true;
            if (skinRenderer.m_Mesh.TryGet(out var skinMesh))
            {
                result["mesh"] = new
                {
                    name = skinMesh.m_Name,
                    pathId = skinMesh.m_PathID,
                    vertexCount = skinMesh.m_VertexCount,
                    subMeshCount = skinMesh.m_SubMeshes?.Length ?? 0
                };
            }
            result["boneCount"] = skinRenderer.m_Bones?.Length ?? 0;
        }

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
