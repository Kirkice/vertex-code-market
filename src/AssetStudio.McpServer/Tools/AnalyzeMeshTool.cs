using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class AnalyzeMeshTool
{
    private readonly AssetManagerService _assetManager;

    public AnalyzeMeshTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Analyze a Mesh asset for GPU performance. Returns vertex count, triangle count, sub-mesh count, UV channel count, bone weight info, BlendShape count, and estimated GPU cost. Identifies high-poly meshes that may impact draw call performance.")]
    public string AnalyzeMesh(
        [Description("The pathID of the Mesh asset.")] long pathId,
        [Description("The source file name of the Mesh asset.")] string sourceFile)
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var obj = _assetManager.FindAsset(pathId, sourceFile);
        if (obj is not Mesh mesh)
            return $"Mesh not found: pathID={pathId}, sourceFile={sourceFile}";

        var triangleCount = mesh.m_Indices.Count / 3;
        var hasSkin = mesh.m_Skin != null && mesh.m_Skin.Length > 0;
        var hasBlendShapes = mesh.m_Shapes?.channels != null && mesh.m_Shapes.channels.Length > 0;
        var blendShapeCount = hasBlendShapes ? mesh.m_Shapes!.channels.Length : 0;

        // Count UV channels
        var uvChannels = 0;
        if (mesh.m_UV0?.Length > 0) uvChannels++;
        if (mesh.m_UV1?.Length > 0) uvChannels++;
        if (mesh.m_UV2?.Length > 0) uvChannels++;
        if (mesh.m_UV3?.Length > 0) uvChannels++;
        if (mesh.m_UV4?.Length > 0) uvChannels++;
        if (mesh.m_UV5?.Length > 0) uvChannels++;
        if (mesh.m_UV6?.Length > 0) uvChannels++;
        if (mesh.m_UV7?.Length > 0) uvChannels++;

        // Estimate vertex data size
        var vertexStride = 12; // position (3 floats)
        if (mesh.m_Normals?.Length > 0) vertexStride += 12;
        if (mesh.m_Tangents?.Length > 0) vertexStride += 16;
        if (mesh.m_Colors?.Length > 0) vertexStride += 4;
        vertexStride += uvChannels * 8; // 2 floats per UV
        if (hasSkin) vertexStride += 32; // 4 weights + 4 indices

        var estimatedVertexMemory = (long)mesh.m_VertexCount * vertexStride;
        var estimatedIndexMemory = (long)mesh.m_Indices.Count * 4; // 32-bit indices

        // Performance analysis
        var issues = new List<string>();
        if (mesh.m_VertexCount > 65535)
            issues.Add($"High vertex count ({mesh.m_VertexCount}). Requires 32-bit indices, increasing memory.");
        if (triangleCount > 100000)
            issues.Add($"Very high triangle count ({triangleCount}). Consider LOD or mesh simplification.");
        if (triangleCount > 50000)
            issues.Add($"High triangle count ({triangleCount}). May impact GPU vertex processing.");
        if (uvChannels > 2)
            issues.Add($"Multiple UV channels ({uvChannels}). Each channel adds vertex data size.");
        if (hasBlendShapes && blendShapeCount > 10)
            issues.Add($"Many BlendShapes ({blendShapeCount}). Each BlendShape adds vertex data and CPU cost.");
        if (hasSkin)
            issues.Add("Skinned mesh. Requires bone matrix computation on CPU/GPU.");

        // SubMesh analysis
        var subMeshes = mesh.m_SubMeshes?.Select((sm, i) => new
        {
            index = i,
            indexCount = sm.indexCount,
            triangleCount = (int)(sm.indexCount / 3),
            topology = sm.topology.ToString(),
            vertexCount = sm.vertexCount
        }).ToArray();

        var result = new
        {
            name = mesh.m_Name,
            pathId = mesh.m_PathID,
            sourceFile = mesh.assetsFile.fileName,
            vertexCount = mesh.m_VertexCount,
            triangleCount,
            subMeshCount = mesh.m_SubMeshes?.Length ?? 0,
            subMeshes,
            uvChannels,
            hasNormals = mesh.m_Normals?.Length > 0,
            hasTangents = mesh.m_Tangents?.Length > 0,
            hasColors = mesh.m_Colors?.Length > 0,
            hasSkin,
            boneCount = mesh.m_BindPose?.Length ?? 0,
            hasBlendShapes,
            blendShapeCount,
            blendShapeNames = hasBlendShapes ? mesh.m_Shapes!.channels.Select(c => c.name).ToArray() : Array.Empty<string>(),
            estimatedVertexMemoryBytes = estimatedVertexMemory,
            estimatedVertexMemoryKB = Math.Round(estimatedVertexMemory / 1024.0, 2),
            estimatedIndexMemoryBytes = estimatedIndexMemory,
            estimatedIndexMemoryKB = Math.Round(estimatedIndexMemory / 1024.0, 2),
            totalEstimatedMemoryKB = Math.Round((estimatedVertexMemory + estimatedIndexMemory) / 1024.0, 2),
            vertexStride,
            issues,
            issueCount = issues.Count
        };

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
