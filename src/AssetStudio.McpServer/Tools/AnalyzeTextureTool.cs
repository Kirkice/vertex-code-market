using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class AnalyzeTextureTool
{
    private readonly AssetManagerService _assetManager;

    public AnalyzeTextureTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Analyze a Texture2D asset for GPU performance. Returns format, dimensions, MipMap status, compression ratio, estimated VRAM usage, and identifies potential performance issues (oversized textures, missing MipMaps, uncompressed formats).")]
    public string AnalyzeTexture(
        [Description("The pathID of the Texture2D asset.")] long pathId,
        [Description("The source file name of the Texture2D asset.")] string sourceFile)
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var obj = _assetManager.FindAsset(pathId, sourceFile);
        if (obj is not Texture2D texture)
            return $"Texture2D not found: pathID={pathId}, sourceFile={sourceFile}";

        var format = texture.m_TextureFormat;
        var width = texture.m_Width;
        var height = texture.m_Height;
        var hasMipMap = texture.m_MipMap;
        var dataSize = texture.image_data?.GetData()?.Length ?? 0;
        var streamSize = texture.m_StreamData?.size ?? 0;
        var totalDataSize = dataSize + (int)streamSize;

        // Estimate VRAM usage
        var estimatedVram = EstimateVRAM(width, height, format, hasMipMap);

        // Performance analysis
        var issues = new List<string>();
        if (width > 2048 || height > 2048)
            issues.Add($"Oversized texture: {width}x{height}. Consider using 2048 or smaller for mobile.");
        if (width > 4096 || height > 4096)
            issues.Add($"Very large texture: {width}x{height}. This will consume significant VRAM.");
        if (!hasMipMap && (width > 64 || height > 64))
            issues.Add("Missing MipMap on large texture. This causes aliasing and cache thrashing.");
        if (IsUncompressedFormat(format))
            issues.Add($"Uncompressed format '{format}' uses more VRAM. Consider compressed formats (DXT/ASTC/ETC2).");
        if (!IsPowerOfTwo(width) || !IsPowerOfTwo(height))
            issues.Add("Non-power-of-two dimensions may cause alignment waste on some GPUs.");

        var result = new
        {
            name = texture.m_Name,
            pathId = texture.m_PathID,
            sourceFile = texture.assetsFile.fileName,
            width,
            height,
            format = format.ToString(),
            formatCategory = GetFormatCategory(format),
            hasMipMap,
            mipMapCount = hasMipMap ? (int)Math.Floor(Math.Log2(Math.Max(width, height))) + 1 : 0,
            dataSizeBytes = totalDataSize,
            dataSizeMB = Math.Round(totalDataSize / (1024.0 * 1024.0), 2),
            estimatedVramBytes = estimatedVram,
            estimatedVramMB = Math.Round(estimatedVram / (1024.0 * 1024.0), 2),
            compressionRatio = totalDataSize > 0 ? Math.Round((double)estimatedVram / totalDataSize, 2) : 0,
            issues,
            issueCount = issues.Count
        };

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    private static long EstimateVRAM(int width, int height, TextureFormat format, bool hasMipMap)
    {
        var bpp = GetBitsPerPixel(format);
        var baseSize = (long)width * height * bpp / 8;
        if (hasMipMap)
            baseSize = (long)(baseSize * 1.33); // Mip chain adds ~33%
        return baseSize;
    }

    private static int GetBitsPerPixel(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.Alpha8 => 8,
            TextureFormat.ARGB4444 => 16,
            TextureFormat.RGB24 => 24,
            TextureFormat.RGBA32 => 32,
            TextureFormat.ARGB32 => 32,
            TextureFormat.RGB565 => 16,
            TextureFormat.DXT1 => 4,
            TextureFormat.DXT5 => 8,
            TextureFormat.RGBA4444 => 16,
            TextureFormat.BC4 => 4,
            TextureFormat.BC5 => 8,
            TextureFormat.BC6H => 8,
            TextureFormat.BC7 => 8,
            TextureFormat.ETC_RGB4 => 4,
            TextureFormat.ETC2_RGB => 4,
            TextureFormat.ETC2_RGBA8 => 8,
            TextureFormat.ETC2_RGBA1 => 4,
            TextureFormat.ASTC_RGB_4x4 or TextureFormat.ASTC_RGBA_4x4 => 8,
            TextureFormat.ASTC_RGB_5x5 or TextureFormat.ASTC_RGBA_5x5 => 5,
            TextureFormat.ASTC_RGB_6x6 or TextureFormat.ASTC_RGBA_6x6 => 4,
            TextureFormat.ASTC_RGB_8x8 or TextureFormat.ASTC_RGBA_8x8 => 2,
            TextureFormat.ASTC_RGB_10x10 or TextureFormat.ASTC_RGBA_10x10 => 1,
            TextureFormat.ASTC_RGB_12x12 or TextureFormat.ASTC_RGBA_12x12 => 1,
            TextureFormat.PVRTC_RGB2 => 2,
            TextureFormat.PVRTC_RGBA2 => 2,
            TextureFormat.PVRTC_RGB4 => 4,
            TextureFormat.PVRTC_RGBA4 => 4,
            TextureFormat.R16 => 16,
            TextureFormat.RHalf => 16,
            TextureFormat.RGHalf => 32,
            TextureFormat.RGBAHalf => 64,
            TextureFormat.RFloat => 32,
            TextureFormat.RGFloat => 64,
            TextureFormat.RGBAFloat => 128,
            _ => 32 // Default assumption
        };
    }

    private static bool IsUncompressedFormat(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.RGB24 or TextureFormat.RGBA32 or TextureFormat.ARGB32 or
            TextureFormat.Alpha8 or TextureFormat.ARGB4444 or TextureFormat.RGBA4444 or
            TextureFormat.RGB565 or TextureFormat.R16 or TextureFormat.RHalf or
            TextureFormat.RGHalf or TextureFormat.RGBAHalf or TextureFormat.RFloat or
            TextureFormat.RGFloat or TextureFormat.RGBAFloat => true,
            _ => false
        };
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

    private static string GetFormatCategory(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.DXT1 or TextureFormat.DXT5 or TextureFormat.BC4 or TextureFormat.BC5 or
            TextureFormat.BC6H or TextureFormat.BC7 => "BCn (Desktop)",
            TextureFormat.ETC_RGB4 or TextureFormat.ETC2_RGB or TextureFormat.ETC2_RGBA8 or
            TextureFormat.ETC2_RGBA1 => "ETC (Android/Universal)",
            TextureFormat.ASTC_RGB_4x4 or TextureFormat.ASTC_RGB_5x5 or TextureFormat.ASTC_RGB_6x6 or
            TextureFormat.ASTC_RGB_8x8 or TextureFormat.ASTC_RGB_10x10 or TextureFormat.ASTC_RGB_12x12 or
            TextureFormat.ASTC_RGBA_4x4 or TextureFormat.ASTC_RGBA_5x5 or TextureFormat.ASTC_RGBA_6x6 or
            TextureFormat.ASTC_RGBA_8x8 or TextureFormat.ASTC_RGBA_10x10 or TextureFormat.ASTC_RGBA_12x12 => "ASTC (Mobile)",
            TextureFormat.PVRTC_RGB2 or TextureFormat.PVRTC_RGBA2 or
            TextureFormat.PVRTC_RGB4 or TextureFormat.PVRTC_RGBA4 => "PVRTC (iOS)",
            TextureFormat.RHalf or TextureFormat.RGHalf or TextureFormat.RGBAHalf or
            TextureFormat.RFloat or TextureFormat.RGFloat or TextureFormat.RGBAFloat => "HDR",
            _ => "Uncompressed"
        };
    }
}
