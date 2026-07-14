using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class BatchTextureReportTool
{
    private readonly AssetManagerService _assetManager;

    public BatchTextureReportTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Generate a batch performance report for all Texture2D assets. Returns a summary of texture sizes, formats, MipMap status, and identifies problematic textures. Useful for auditing texture quality across an entire Bundle.")]
    public string BatchTextureReport(
        [Description("Optional: Maximum number of textures to include in detailed report. Default is 100.")] int maxDetails = 100)
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var allAssets = _assetManager.GetAllAssets();
        var textures = allAssets.Where(a => a.Asset is Texture2D).ToList();

        if (textures.Count == 0)
            return "No Texture2D assets found in loaded files.";

        var totalVRAM = 0L;
        var totalDataSize = 0L;
        var oversizedCount = 0;
        var noMipMapCount = 0;
        var uncompressedCount = 0;
        var nonPOTCount = 0;

        var details = new List<object>();

        foreach (var texInfo in textures)
        {
            var tex = (Texture2D)texInfo.Asset!;
            var bpp = GetBPP(tex.m_TextureFormat);
            var vram = (long)tex.m_Width * tex.m_Height * bpp / 8;
            if (tex.m_MipMap) vram = (long)(vram * 1.33);

            totalVRAM += vram;
            totalDataSize += texInfo.ByteSize;

            var issues = new List<string>();
            if (tex.m_Width > 2048 || tex.m_Height > 2048) { oversizedCount++; issues.Add("oversized"); }
            if (!tex.m_MipMap && (tex.m_Width > 64 || tex.m_Height > 64)) { noMipMapCount++; issues.Add("no_mipmap"); }
            if (IsUncompressed(tex.m_TextureFormat)) { uncompressedCount++; issues.Add("uncompressed"); }
            if (!IsPOT(tex.m_Width) || !IsPOT(tex.m_Height)) { nonPOTCount++; issues.Add("non_pot"); }

            if (details.Count < maxDetails)
            {
                details.Add(new
                {
                    name = tex.m_Name,
                    pathId = tex.m_PathID,
                    width = tex.m_Width,
                    height = tex.m_Height,
                    format = tex.m_TextureFormat.ToString(),
                    hasMipMap = tex.m_MipMap,
                    vramMB = Math.Round(vram / (1024.0 * 1024.0), 2),
                    issues
                });
            }
        }

        var result = new
        {
            summary = new
            {
                totalTextures = textures.Count,
                totalVRAMBytes = totalVRAM,
                totalVRAMMB = Math.Round(totalVRAM / (1024.0 * 1024.0), 2),
                totalDataSizeBytes = totalDataSize,
                totalDataSizeMB = Math.Round(totalDataSize / (1024.0 * 1024.0), 2),
                oversizedCount,
                noMipMapCount,
                uncompressedCount,
                nonPOTCount
            },
            details = details.OrderByDescending(d => ((dynamic)d).vramMB).ToArray()
        };

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    private static int GetBPP(TextureFormat format) => format switch
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

    private static bool IsUncompressed(TextureFormat format) => format switch
    {
        TextureFormat.RGB24 or TextureFormat.RGBA32 or TextureFormat.ARGB32 or
        TextureFormat.Alpha8 or TextureFormat.ARGB4444 or TextureFormat.RGBA4444 or
        TextureFormat.RGB565 => true,
        _ => false
    };

    private static bool IsPOT(int value) => value > 0 && (value & (value - 1)) == 0;
}
