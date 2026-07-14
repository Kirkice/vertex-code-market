using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class GetMaterialInfoTool
{
    private readonly AssetManagerService _assetManager;

    public GetMaterialInfoTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Get detailed information about a Material asset. Returns shader reference, texture bindings, float/color properties, and keywords. Useful for understanding material configuration and identifying texture references for performance analysis.")]
    public string GetMaterialInfo(
        [Description("The pathID of the Material asset.")] long pathId,
        [Description("The source file name of the Material asset.")] string sourceFile)
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var obj = _assetManager.FindAsset(pathId, sourceFile);
        if (obj is not Material material)
            return $"Material not found: pathID={pathId}, sourceFile={sourceFile}";

        var result = new Dictionary<string, object?>
        {
            ["name"] = material.m_Name,
            ["pathId"] = material.m_PathID,
            ["sourceFile"] = material.assetsFile.fileName
        };

        // Shader reference
        if (material.m_Shader.TryGet(out var shader))
        {
            result["shaderName"] = shader.m_ParsedForm?.m_Name ?? shader.m_Name;
            result["shaderPathId"] = shader.m_PathID;
        }
        else
        {
            result["shaderName"] = "unresolved";
        }

        // Texture properties
        if (material.m_SavedProperties?.m_TexEnvs != null)
        {
            var textures = material.m_SavedProperties.m_TexEnvs.Select(t =>
            {
                var texInfo = new Dictionary<string, object?>
                {
                    ["propertyName"] = t.Key,
                    ["scale"] = new { x = t.Value.m_Scale.X, y = t.Value.m_Scale.Y },
                    ["offset"] = new { x = t.Value.m_Offset.X, y = t.Value.m_Offset.Y }
                };

                if (t.Value.m_Texture.TryGet(out var tex))
                {
                    if (tex is Texture2D tex2d)
                    {
                        texInfo["textureName"] = tex2d.m_Name;
                        texInfo["texturePathId"] = tex2d.m_PathID;
                        texInfo["width"] = tex2d.m_Width;
                        texInfo["height"] = tex2d.m_Height;
                        texInfo["format"] = tex2d.m_TextureFormat.ToString();
                    }
                    else
                    {
                        texInfo["textureName"] = "unknown";
                    }
                }
                else
                {
                    texInfo["textureName"] = "null";
                }

                return texInfo;
            }).ToArray();

            result["textures"] = textures;
        }

        // Float properties
        if (material.m_SavedProperties?.m_Floats != null)
        {
            result["floats"] = material.m_SavedProperties.m_Floats
                .Select(f => new { name = f.Key, value = f.Value }).ToArray();
        }

        // Color properties
        if (material.m_SavedProperties?.m_Colors != null)
        {
            result["colors"] = material.m_SavedProperties.m_Colors
                .Select(c => new { name = c.Key, r = c.Value.R, g = c.Value.G, b = c.Value.B, a = c.Value.A }).ToArray();
        }

        // Int properties
        if (material.m_SavedProperties?.m_Ints != null)
        {
            result["ints"] = material.m_SavedProperties.m_Ints
                .Select(i => new { name = i.Key, value = i.Value }).ToArray();
        }

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
