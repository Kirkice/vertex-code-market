using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class GetMemoryProfileTool
{
    private readonly AssetManagerService _assetManager;

    public GetMemoryProfileTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Estimate runtime memory usage of all loaded assets. Returns per-type memory estimates, top memory consumers, and optimization suggestions. Useful for identifying memory bottlenecks and planning optimization strategies.")]
    public string GetMemoryProfile()
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var allAssets = _assetManager.GetAllAssets();

        // Estimate memory per asset
        var memoryEstimates = new List<object>();
        long totalEstimatedMemory = 0;
        long textureMemory = 0;
        long meshMemory = 0;
        long audioMemory = 0;
        long animationMemory = 0;
        long otherMemory = 0;

        foreach (var assetInfo in allAssets)
        {
            long estimated = 0;
            string category = "other";

            switch (assetInfo.Asset)
            {
                case Texture2D tex:
                    var bpp = GetTextureBPP(tex.m_TextureFormat);
                    estimated = (long)tex.m_Width * tex.m_Height * bpp / 8;
                    if (tex.m_MipMap) estimated = (long)(estimated * 1.33);
                    textureMemory += estimated;
                    category = "texture";
                    break;
                case Mesh mesh:
                    var vertexSize = (long)mesh.m_VertexCount * 48; // ~48 bytes per vertex average
                    var indexSize = (long)mesh.m_Indices.Count * 4;
                    estimated = vertexSize + indexSize;
                    meshMemory += estimated;
                    category = "mesh";
                    break;
                case AudioClip audio:
                    estimated = audio.m_AudioData?.GetData()?.Length ?? 0;
                    if (audio.m_Source != null)
                        estimated += audio.m_Size;
                    audioMemory += estimated;
                    category = "audio";
                    break;
                case AnimationClip clip:
                    estimated = assetInfo.ByteSize;
                    animationMemory += estimated;
                    category = "animation";
                    break;
                default:
                    estimated = assetInfo.ByteSize;
                    otherMemory += estimated;
                    break;
            }

            totalEstimatedMemory += estimated;

            if (estimated > 1024 * 1024) // Only track assets > 1MB
            {
                memoryEstimates.Add(new
                {
                    name = assetInfo.Name,
                    type = assetInfo.Type,
                    category,
                    pathId = assetInfo.PathID,
                    sourceFile = assetInfo.SourceFile,
                    estimatedMemoryBytes = estimated,
                    estimatedMemoryMB = Math.Round(estimated / (1024.0 * 1024.0), 2)
                });
            }
        }

        // Sort by memory usage
        var topConsumers = memoryEstimates
            .OrderByDescending(m => ((dynamic)m).estimatedMemoryBytes)
            .Take(20)
            .ToArray();

        // Optimization suggestions
        var suggestions = new List<string>();
        if (textureMemory > 100 * 1024 * 1024)
            suggestions.Add($"Texture memory is high ({textureMemory / (1024 * 1024)}MB). Consider using compressed formats (ASTC/ETC2/BCn) and reducing texture sizes.");
        if (meshMemory > 50 * 1024 * 1024)
            suggestions.Add($"Mesh memory is high ({meshMemory / (1024 * 1024)}MB). Consider mesh simplification and LOD.");
        if (audioMemory > 50 * 1024 * 1024)
            suggestions.Add($"Audio memory is high ({audioMemory / (1024 * 1024)}MB). Consider using compressed audio formats (Vorbis/ADPCM).");

        var result = new
        {
            summary = new
            {
                totalEstimatedMemoryBytes = totalEstimatedMemory,
                totalEstimatedMemoryMB = Math.Round(totalEstimatedMemory / (1024.0 * 1024.0), 2),
                textureMemoryBytes = textureMemory,
                textureMemoryMB = Math.Round(textureMemory / (1024.0 * 1024.0), 2),
                meshMemoryBytes = meshMemory,
                meshMemoryMB = Math.Round(meshMemory / (1024.0 * 1024.0), 2),
                audioMemoryBytes = audioMemory,
                audioMemoryMB = Math.Round(audioMemory / (1024.0 * 1024.0), 2),
                animationMemoryBytes = animationMemory,
                animationMemoryMB = Math.Round(animationMemory / (1024.0 * 1024.0), 2),
                otherMemoryBytes = otherMemory,
                otherMemoryMB = Math.Round(otherMemory / (1024.0 * 1024.0), 2)
            },
            topMemoryConsumers = topConsumers,
            suggestions,
            suggestionCount = suggestions.Count
        };

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    private static int GetTextureBPP(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.Alpha8 => 8,
            TextureFormat.ARGB4444 or TextureFormat.RGB565 or TextureFormat.RGBA4444 => 16,
            TextureFormat.RGB24 => 24,
            TextureFormat.RGBA32 or TextureFormat.ARGB32 => 32,
            TextureFormat.DXT1 or TextureFormat.BC4 or TextureFormat.ETC_RGB4 or TextureFormat.ETC2_RGB or TextureFormat.ETC2_RGBA1 => 4,
            TextureFormat.DXT5 or TextureFormat.BC5 or TextureFormat.BC6H or TextureFormat.BC7 or TextureFormat.ETC2_RGBA8 => 8,
            TextureFormat.ASTC_RGB_4x4 or TextureFormat.ASTC_RGBA_4x4 => 8,
            TextureFormat.ASTC_RGB_5x5 or TextureFormat.ASTC_RGBA_5x5 => 5,
            TextureFormat.ASTC_RGB_6x6 or TextureFormat.ASTC_RGBA_6x6 => 4,
            TextureFormat.ASTC_RGB_8x8 or TextureFormat.ASTC_RGBA_8x8 => 2,
            TextureFormat.PVRTC_RGB2 or TextureFormat.PVRTC_RGBA2 => 2,
            TextureFormat.PVRTC_RGB4 or TextureFormat.PVRTC_RGBA4 => 4,
            _ => 32
        };
    }
}
