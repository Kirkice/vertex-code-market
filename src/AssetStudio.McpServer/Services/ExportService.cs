using System.Text;
using AssetStudio;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AssetStudio.McpServer.Services;

/// <summary>
/// Service for exporting assets to various formats.
/// </summary>
public class ExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Export an asset to the specified output path with automatic format detection.
    /// </summary>
    public ExportResult ExportAsset(Object asset, string name, string outputPath, string? format = null)
    {
        try
        {
            Directory.CreateDirectory(outputPath);
            var type = asset.type;

            switch (type)
            {
                case ClassIDType.Texture2D:
                    return ExportTexture2D((Texture2D)asset, name, outputPath, format);
                case ClassIDType.AudioClip:
                    return ExportAudioClip((AudioClip)asset, name, outputPath);
                case ClassIDType.Shader:
                    return ExportShader((Shader)asset, name, outputPath);
                case ClassIDType.TextAsset:
                    return ExportTextAsset((TextAsset)asset, name, outputPath);
                case ClassIDType.MonoBehaviour:
                    return ExportMonoBehaviour((MonoBehaviour)asset, name, outputPath);
                case ClassIDType.Font:
                    return ExportFont((Font)asset, name, outputPath);
                case ClassIDType.Mesh:
                    return ExportMesh((Mesh)asset, name, outputPath);
                case ClassIDType.VideoClip:
                    return ExportVideoClip((VideoClip)asset, name, outputPath);
                case ClassIDType.MovieTexture:
                    return ExportMovieTexture((MovieTexture)asset, name, outputPath);
                case ClassIDType.Sprite:
                    return ExportSprite((Sprite)asset, name, outputPath, format);
                case ClassIDType.Animator:
                    return ExportAnimator((Animator)asset, name, outputPath);
                default:
                    return ExportRaw(asset, name, outputPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export asset {Name}", name);
            return new ExportResult
            {
                Success = false,
                Message = $"Export failed: {ex.Message}"
            };
        }
    }

    private ExportResult ExportTexture2D(Texture2D texture, string name, string outputPath, string? format)
    {
        var image = texture.ConvertToImage(true);
        if (image == null)
        {
            return new ExportResult { Success = false, Message = "Failed to convert texture to image." };
        }

        using (image)
        {
            var ext = format?.ToLower() ?? "png";
            var filePath = Path.Combine(outputPath, $"{FixFileName(name)}.{ext}");
            using var file = File.OpenWrite(filePath);

            var imageFormat = ext switch
            {
                "png" => ImageFormat.Png,
                "jpeg" or "jpg" => ImageFormat.Jpeg,
                "bmp" => ImageFormat.Bmp,
                "tga" => ImageFormat.Tga,
                _ => ImageFormat.Png
            };

            image.WriteToStream(file, imageFormat);
            return new ExportResult
            {
                Success = true,
                Message = $"Texture exported to {filePath}",
                OutputPath = filePath
            };
        }
    }

    private ExportResult ExportAudioClip(AudioClip audioClip, string name, string outputPath)
    {
        var audioData = audioClip.m_AudioData.GetData();
        if (audioData == null || audioData.Length == 0)
        {
            return new ExportResult { Success = false, Message = "Audio clip has no data." };
        }

        var converter = new AudioClipConverter(audioClip);
        if (converter.IsSupport)
        {
            var filePath = Path.Combine(outputPath, $"{FixFileName(name)}.wav");
            var buffer = converter.ConvertToWav();
            if (buffer != null)
            {
                File.WriteAllBytes(filePath, buffer);
                return new ExportResult { Success = true, Message = $"Audio exported to {filePath}", OutputPath = filePath };
            }
        }

        // Fallback to raw export
        var ext = converter.GetExtensionName();
        var rawPath = Path.Combine(outputPath, $"{FixFileName(name)}{ext}");
        File.WriteAllBytes(rawPath, audioData);
        return new ExportResult { Success = true, Message = $"Audio raw data exported to {rawPath}", OutputPath = rawPath };
    }

    private ExportResult ExportShader(Shader shader, string name, string outputPath)
    {
        var filePath = Path.Combine(outputPath, $"{FixFileName(name)}.shader");
        var str = shader.Convert();
        File.WriteAllText(filePath, str);
        return new ExportResult { Success = true, Message = $"Shader exported to {filePath}", OutputPath = filePath };
    }

    private ExportResult ExportTextAsset(TextAsset textAsset, string name, string outputPath)
    {
        var filePath = Path.Combine(outputPath, $"{FixFileName(name)}.txt");
        File.WriteAllBytes(filePath, textAsset.m_Script);
        return new ExportResult { Success = true, Message = $"TextAsset exported to {filePath}", OutputPath = filePath };
    }

    private ExportResult ExportMonoBehaviour(MonoBehaviour monoBehaviour, string name, string outputPath)
    {
        var filePath = Path.Combine(outputPath, $"{FixFileName(name)}.json");
        var type = monoBehaviour.ToType();
        if (type == null)
        {
            // Try dump as raw text
            var dump = monoBehaviour.Dump();
            if (dump != null)
            {
                File.WriteAllText(filePath, dump);
                return new ExportResult { Success = true, Message = $"MonoBehaviour dumped to {filePath}", OutputPath = filePath };
            }
            return new ExportResult { Success = false, Message = "Cannot deserialize MonoBehaviour without assembly." };
        }

        var json = JsonConvert.SerializeObject(type, Formatting.Indented);
        File.WriteAllText(filePath, json);
        return new ExportResult { Success = true, Message = $"MonoBehaviour exported to {filePath}", OutputPath = filePath };
    }

    private ExportResult ExportFont(Font font, string name, string outputPath)
    {
        if (font.m_FontData == null)
        {
            return new ExportResult { Success = false, Message = "Font has no data." };
        }

        var extension = ".ttf";
        if (font.m_FontData.Length >= 4 &&
            font.m_FontData[0] == 79 && font.m_FontData[1] == 84 &&
            font.m_FontData[2] == 84 && font.m_FontData[3] == 79)
        {
            extension = ".otf";
        }

        var filePath = Path.Combine(outputPath, $"{FixFileName(name)}{extension}");
        File.WriteAllBytes(filePath, font.m_FontData);
        return new ExportResult { Success = true, Message = $"Font exported to {filePath}", OutputPath = filePath };
    }

    private ExportResult ExportMesh(Mesh mesh, string name, string outputPath)
    {
        if (mesh.m_VertexCount <= 0)
        {
            return new ExportResult { Success = false, Message = "Mesh has no vertices." };
        }

        var filePath = Path.Combine(outputPath, $"{FixFileName(name)}.obj");
        var sb = new StringBuilder();
        sb.AppendLine("g " + mesh.m_Name);

        // Vertices
        if (mesh.m_Vertices == null || mesh.m_Vertices.Length == 0)
        {
            return new ExportResult { Success = false, Message = "Mesh has no vertex data." };
        }

        int c = mesh.m_Vertices.Length == mesh.m_VertexCount * 4 ? 4 : 3;
        for (int v = 0; v < mesh.m_VertexCount; v++)
        {
            sb.AppendFormat("v {0} {1} {2}\r\n", -mesh.m_Vertices[v * c], mesh.m_Vertices[v * c + 1], mesh.m_Vertices[v * c + 2]);
        }

        // UV
        if (mesh.m_UV0?.Length > 0)
        {
            c = mesh.m_UV0.Length == mesh.m_VertexCount * 2 ? 2 :
                mesh.m_UV0.Length == mesh.m_VertexCount * 3 ? 3 : 4;
            for (int v = 0; v < mesh.m_VertexCount; v++)
            {
                sb.AppendFormat("vt {0} {1}\r\n", mesh.m_UV0[v * c], mesh.m_UV0[v * c + 1]);
            }
        }

        // Normals
        if (mesh.m_Normals?.Length > 0)
        {
            c = mesh.m_Normals.Length == mesh.m_VertexCount * 3 ? 3 : 4;
            for (int v = 0; v < mesh.m_VertexCount; v++)
            {
                sb.AppendFormat("vn {0} {1} {2}\r\n", -mesh.m_Normals[v * c], mesh.m_Normals[v * c + 1], mesh.m_Normals[v * c + 2]);
            }
        }

        // Faces
        int sum = 0;
        for (var i = 0; i < mesh.m_SubMeshes.Length; i++)
        {
            sb.AppendLine($"g {mesh.m_Name}_{i}");
            int indexCount = (int)mesh.m_SubMeshes[i].indexCount;
            var end = sum + indexCount / 3;
            for (int f = sum; f < end; f++)
            {
                sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\r\n",
                    mesh.m_Indices[f * 3 + 2] + 1, mesh.m_Indices[f * 3 + 1] + 1, mesh.m_Indices[f * 3] + 1);
            }
            sum = end;
        }

        sb.Replace("NaN", "0");
        File.WriteAllText(filePath, sb.ToString());
        return new ExportResult { Success = true, Message = $"Mesh exported to {filePath}", OutputPath = filePath };
    }

    private ExportResult ExportVideoClip(VideoClip videoClip, string name, string outputPath)
    {
        if (videoClip.m_ExternalResources.m_Size <= 0)
        {
            return new ExportResult { Success = false, Message = "Video clip has no external resources." };
        }

        var ext = Path.GetExtension(videoClip.m_OriginalPath);
        var filePath = Path.Combine(outputPath, $"{FixFileName(name)}{ext}");
        videoClip.m_VideoData.WriteData(filePath);
        return new ExportResult { Success = true, Message = $"Video exported to {filePath}", OutputPath = filePath };
    }

    private ExportResult ExportMovieTexture(MovieTexture movieTexture, string name, string outputPath)
    {
        var filePath = Path.Combine(outputPath, $"{FixFileName(name)}.ogv");
        File.WriteAllBytes(filePath, movieTexture.m_MovieData);
        return new ExportResult { Success = true, Message = $"MovieTexture exported to {filePath}", OutputPath = filePath };
    }

    private ExportResult ExportSprite(Sprite sprite, string name, string outputPath, string? format)
    {
        var image = sprite.GetImage();
        if (image == null)
        {
            return new ExportResult { Success = false, Message = "Failed to get sprite image." };
        }

        using (image)
        {
            var ext = format?.ToLower() ?? "png";
            var filePath = Path.Combine(outputPath, $"{FixFileName(name)}.{ext}");
            using var file = File.OpenWrite(filePath);

            var imageFormat = ext switch
            {
                "png" => ImageFormat.Png,
                "jpeg" or "jpg" => ImageFormat.Jpeg,
                "bmp" => ImageFormat.Bmp,
                "tga" => ImageFormat.Tga,
                _ => ImageFormat.Png
            };

            image.WriteToStream(file, imageFormat);
            return new ExportResult { Success = true, Message = $"Sprite exported to {filePath}", OutputPath = filePath };
        }
    }

    private ExportResult ExportAnimator(Animator animator, string name, string outputPath)
    {
        try
        {
            var filePath = Path.Combine(outputPath, FixFileName(name), $"{FixFileName(name)}.fbx");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var convert = new ModelConverter(animator, ImageFormat.Png);
            // Default FBX export settings
            ModelExporter.ExportFbx(
                filePath, convert,
                eulerFilter: true,
                filterPrecision: 0.5f,
                allNodes: false,
                skins: true,
                animation: true,
                blendShape: true,
                castToBone: false,
                boneSize: 10,
                exportAllUvsAsDiffuseMaps: false,
                scaleFactor: 1.0f,
                versionIndex: 0,
                isAscii: false
            );
            return new ExportResult { Success = true, Message = $"Animator exported to {filePath}", OutputPath = filePath };
        }
        catch (Exception ex)
        {
            return new ExportResult { Success = false, Message = $"FBX export failed: {ex.Message}. FBX SDK native library may not be available." };
        }
    }

    private ExportResult ExportRaw(Object asset, string name, string outputPath)
    {
        var filePath = Path.Combine(outputPath, $"{FixFileName(name)}.dat");
        File.WriteAllBytes(filePath, asset.GetRawData());
        return new ExportResult { Success = true, Message = $"Raw data exported to {filePath}", OutputPath = filePath };
    }

    private static string FixFileName(string str)
    {
        if (string.IsNullOrEmpty(str)) return "unnamed";
        if (str.Length >= 260) return Path.GetRandomFileName();
        return Path.GetInvalidFileNameChars().Aggregate(str, (current, c) => current.Replace(c, '_'));
    }
}

/// <summary>
/// Result of an export operation.
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? OutputPath { get; set; }
}
