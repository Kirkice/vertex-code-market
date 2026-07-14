using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class AnalyzeShaderTool
{
    private readonly AssetManagerService _assetManager;

    public AnalyzeShaderTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Analyze a Shader asset in detail. Extracts SubShader count, Pass count, Tags, render queue, LOD, blend modes, GPU program types, and shader properties. Useful for understanding rendering techniques, identifying URP/HDRP/Built-in pipeline usage, and diagnosing shader complexity.")]
    public string AnalyzeShader(
        [Description("The pathID of the Shader asset.")] long pathId,
        [Description("The source file name of the Shader asset.")] string sourceFile)
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var obj = _assetManager.FindAsset(pathId, sourceFile);
        if (obj is not Shader shader)
            return $"Shader not found: pathID={pathId}, sourceFile={sourceFile}";

        var result = new Dictionary<string, object?>
        {
            ["name"] = shader.m_ParsedForm?.m_Name ?? shader.m_Name,
            ["pathId"] = shader.m_PathID,
            ["sourceFile"] = shader.assetsFile.fileName
        };

        if (shader.m_ParsedForm != null)
        {
            var parsed = shader.m_ParsedForm;

            // Properties
            var props = parsed.m_PropInfo?.m_Props?.Select(p => new
            {
                name = p.m_Name,
                description = p.m_Description,
                type = p.m_Type.ToString(),
                attributes = p.m_Attributes
            }).ToArray();
            result["properties"] = props ?? Array.Empty<object>();

            // SubShaders
            var subShaders = new List<object>();
            if (parsed.m_SubShaders != null)
            {
                foreach (var sub in parsed.m_SubShaders)
                {
                    var passes = new List<object>();
                    if (sub.m_Passes != null)
                    {
                        foreach (var pass in sub.m_Passes)
                        {
                            var passInfo = new Dictionary<string, object?>
                            {
                                ["name"] = pass.m_State?.m_Name,
                                ["tags"] = pass.m_State?.m_Tags?.tags?.ToDictionary(t => t.Key, t => t.Value),
                                ["lod"] = pass.m_State?.m_LOD,
                                ["lighting"] = pass.m_State?.lighting
                            };

                            // Blend state
                            if (pass.m_State?.rtBlend != null)
                            {
                                var blend = pass.m_State.rtBlend[0];
                                passInfo["srcBlend"] = blend.srcBlend.val;
                                passInfo["destBlend"] = blend.destBlend.val;
                            }

                            // GPU programs
                            if (pass.progVertex?.m_SubPrograms != null)
                            {
                                passInfo["vertexProgramTypes"] = pass.progVertex.m_SubPrograms
                                    .Select(sp => sp.m_GpuProgramType.ToString()).Distinct().ToArray();
                            }
                            if (pass.progFragment?.m_SubPrograms != null)
                            {
                                passInfo["fragmentProgramTypes"] = pass.progFragment.m_SubPrograms
                                    .Select(sp => sp.m_GpuProgramType.ToString()).Distinct().ToArray();
                            }

                            passes.Add(passInfo);
                        }
                    }

                    subShaders.Add(new
                    {
                        lod = sub.m_LOD,
                        tags = sub.m_Tags?.tags?.ToDictionary(t => t.Key, t => t.Value),
                        passCount = sub.m_Passes?.Length ?? 0,
                        passes
                    });
                }
            }
            result["subShaderCount"] = parsed.m_SubShaders?.Length ?? 0;
            result["subShaders"] = subShaders;

            // Fallback
            result["fallback"] = parsed.m_FallbackName;
            result["customEditor"] = parsed.m_CustomEditorName;
        }

        // Platforms
        if (shader.platforms != null)
        {
            result["platforms"] = shader.platforms.Select(p => p.ToString()).ToArray();
        }

        // Script (raw shader source if available)
        if (shader.m_Script != null && shader.m_Script.Length > 0)
        {
            var scriptText = System.Text.Encoding.UTF8.GetString(shader.m_Script);
            result["hasSourceCode"] = true;
            result["sourceCodeLength"] = scriptText.Length;
            // Include first 2000 chars as preview
            result["sourceCodePreview"] = scriptText.Length > 2000 ? scriptText[..2000] + "..." : scriptText;
        }
        else
        {
            result["hasSourceCode"] = false;
        }

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
