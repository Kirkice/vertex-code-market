using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class DecompileShaderTool
{
    private readonly AssetManagerService _assetManager;

    public DecompileShaderTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Decompile a Shader asset to readable HLSL/GLSL source code. Uses AssetStudio's ShaderConverter to reconstruct the shader source. Useful for deep analysis of rendering logic and understanding custom effects.")]
    public string DecompileShader(
        [Description("The pathID of the Shader asset.")] long pathId,
        [Description("The source file name of the Shader asset.")] string sourceFile)
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var obj = _assetManager.FindAsset(pathId, sourceFile);
        if (obj is not Shader shader)
            return $"Shader not found: pathID={pathId}, sourceFile={sourceFile}";

        try
        {
            var decompiled = shader.Convert();
            if (string.IsNullOrEmpty(decompiled))
                return "Shader decompilation returned empty result.";

            var result = new
            {
                name = shader.m_ParsedForm?.m_Name ?? shader.m_Name,
                pathId = shader.m_PathID,
                sourceFile = shader.assetsFile.fileName,
                decompiledLength = decompiled.Length,
                decompiledSource = decompiled.Length > 50000 ? decompiled[..50000] + "\n... [truncated]" : decompiled
            };

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"Shader decompilation failed: {ex.Message}";
        }
    }
}
