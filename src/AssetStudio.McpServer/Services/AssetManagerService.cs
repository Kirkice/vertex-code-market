using AssetStudio;
using Microsoft.Extensions.Logging;

namespace AssetStudio.McpServer.Services;

/// <summary>
/// Wraps AssetsManager to provide a stateful asset management service for MCP tools.
/// </summary>
public class AssetManagerService
{
    private readonly ILogger<AssetManagerService> _logger;
    private AssetsManager? _assetsManager;

    public AssetManagerService(ILogger<AssetManagerService> logger)
    {
        _logger = logger;
        // Set up a logger for AssetStudio core library
        Logger.Default = new McpLogger(logger);
    }

    /// <summary>
    /// Gets the current AssetsManager instance, creating one if necessary.
    /// </summary>
    public AssetsManager AssetsManager => _assetsManager ??= new AssetsManager();

    /// <summary>
    /// Whether assets have been loaded.
    /// </summary>
    public bool IsLoaded => _assetsManager != null && _assetsManager.assetsFileList.Count > 0;

    /// <summary>
    /// Load asset files from the given paths.
    /// </summary>
    public LoadResult LoadFiles(string[] paths)
    {
        try
        {
            // Reset manager for fresh load
            _assetsManager = new AssetsManager();
            Logger.Default = new McpLogger(_logger);

            var validPaths = new List<string>();
            var invalidPaths = new List<string>();

            foreach (var path in paths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    validPaths.Add(fullPath);
                }
                else if (Directory.Exists(fullPath))
                {
                    validPaths.Add(fullPath);
                }
                else
                {
                    invalidPaths.Add(path);
                }
            }

            if (validPaths.Count == 0)
            {
                return new LoadResult
                {
                    Success = false,
                    Message = "No valid paths provided.",
                    InvalidPaths = invalidPaths
                };
            }

            // Separate files and folders
            var files = validPaths.Where(File.Exists).ToArray();
            var folders = validPaths.Where(Directory.Exists).ToArray();

            if (files.Length > 0)
            {
                _assetsManager.LoadFiles(files);
            }

            foreach (var folder in folders)
            {
                _assetsManager.LoadFolder(folder);
            }

            var totalObjects = _assetsManager.assetsFileList.Sum(f => f.Objects.Count);
            var totalFiles = _assetsManager.assetsFileList.Count;

            _logger.LogInformation("Loaded {FileCount} serialized files with {ObjectCount} objects", totalFiles, totalObjects);

            return new LoadResult
            {
                Success = true,
                Message = $"Loaded {totalFiles} serialized file(s) with {totalObjects} object(s).",
                SerializedFileCount = totalFiles,
                ObjectCount = totalObjects,
                InvalidPaths = invalidPaths
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load files");
            return new LoadResult
            {
                Success = false,
                Message = $"Error loading files: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get all exportable assets from loaded files.
    /// </summary>
    public List<McpAssetInfo> GetExportableAssets()
    {
        if (!IsLoaded) return new List<McpAssetInfo>();

        var result = new List<McpAssetInfo>();
        var containers = BuildContainerMap();

        foreach (var assetsFile in _assetsManager!.assetsFileList)
        {
            foreach (var obj in assetsFile.Objects)
            {
                var info = CreateAssetInfo(obj, assetsFile, containers);
                if (info != null && info.IsExportable)
                {
                    result.Add(info);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Get all assets (including non-exportable) from loaded files.
    /// </summary>
    public List<McpAssetInfo> GetAllAssets()
    {
        if (!IsLoaded) return new List<McpAssetInfo>();

        var result = new List<McpAssetInfo>();
        var containers = BuildContainerMap();

        foreach (var assetsFile in _assetsManager!.assetsFileList)
        {
            foreach (var obj in assetsFile.Objects)
            {
                var info = CreateAssetInfo(obj, assetsFile, containers);
                if (info != null)
                {
                    result.Add(info);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Find an asset by pathID and source file name.
    /// </summary>
    public Object? FindAsset(long pathId, string fileName)
    {
        if (!IsLoaded) return null;

        foreach (var assetsFile in _assetsManager!.assetsFileList)
        {
            if (string.Equals(assetsFile.fileName, fileName, StringComparison.OrdinalIgnoreCase))
            {
                if (assetsFile.ObjectsDic.TryGetValue(pathId, out var obj))
                {
                    return obj;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Clear all loaded assets.
    /// </summary>
    public void Clear()
    {
        _assetsManager = null;
    }

    private Dictionary<Object, string> BuildContainerMap()
    {
        var containers = new Dictionary<Object, string>();

        foreach (var assetsFile in _assetsManager!.assetsFileList)
        {
            foreach (var obj in assetsFile.Objects)
            {
                if (obj is AssetBundle m_AssetBundle)
                {
                    foreach (var container in m_AssetBundle.m_Container)
                    {
                        var preloadIndex = container.Value.preloadIndex;
                        var preloadSize = container.Value.preloadSize;
                        var preloadEnd = preloadIndex + preloadSize;
                        for (int k = preloadIndex; k < preloadEnd; k++)
                        {
                            if (m_AssetBundle.m_PreloadTable[k].TryGet(out var asset))
                            {
                                containers[asset] = container.Key;
                            }
                        }
                    }
                }
                else if (obj is ResourceManager m_ResourceManager)
                {
                    foreach (var container in m_ResourceManager.m_Container)
                    {
                        if (container.Value.TryGet(out var asset))
                        {
                            containers[asset] = container.Key;
                        }
                    }
                }
            }
        }

        return containers;
    }

    private static McpAssetInfo? CreateAssetInfo(Object obj, SerializedFile assetsFile, Dictionary<Object, string> containers)
    {
        var info = new McpAssetInfo
        {
            PathID = obj.m_PathID,
            SourceFile = assetsFile.fileName,
            Type = obj.type.ToString(),
            ClassID = (int)obj.type,
            ByteSize = obj.byteSize,
            Asset = obj,
            Container = containers.TryGetValue(obj, out var container) ? container : string.Empty
        };

        switch (obj)
        {
            case GameObject m_GameObject:
                info.Name = m_GameObject.m_Name;
                info.IsExportable = false;
                break;
            case Texture2D m_Texture2D:
                info.Name = m_Texture2D.m_Name;
                info.IsExportable = true;
                if (m_Texture2D.m_StreamData?.path != null)
                    info.ByteSize += m_Texture2D.m_StreamData.size;
                break;
            case AudioClip m_AudioClip:
                info.Name = m_AudioClip.m_Name;
                info.IsExportable = true;
                if (!string.IsNullOrEmpty(m_AudioClip.m_Source))
                    info.ByteSize += m_AudioClip.m_Size;
                break;
            case VideoClip m_VideoClip:
                info.Name = m_VideoClip.m_Name;
                info.IsExportable = true;
                if (!string.IsNullOrEmpty(m_VideoClip.m_OriginalPath))
                    info.ByteSize += m_VideoClip.m_ExternalResources.m_Size;
                break;
            case Shader m_Shader:
                info.Name = m_Shader.m_ParsedForm?.m_Name ?? m_Shader.m_Name;
                info.IsExportable = true;
                break;
            case Mesh m_Mesh:
                info.Name = m_Mesh.m_Name;
                info.IsExportable = true;
                break;
            case TextAsset m_TextAsset:
                info.Name = m_TextAsset.m_Name;
                info.IsExportable = true;
                break;
            case AnimationClip m_AnimationClip:
                info.Name = m_AnimationClip.m_Name;
                info.IsExportable = true;
                break;
            case Font m_Font:
                info.Name = m_Font.m_Name;
                info.IsExportable = true;
                break;
            case MovieTexture m_MovieTexture:
                info.Name = m_MovieTexture.m_Name;
                info.IsExportable = true;
                break;
            case Sprite m_Sprite:
                info.Name = m_Sprite.m_Name;
                info.IsExportable = true;
                break;
            case Animator m_Animator:
                info.Name = m_Animator.m_GameObject.TryGet(out var go) ? go.m_Name : "Animator";
                info.IsExportable = true;
                break;
            case MonoBehaviour m_MonoBehaviour:
                if (m_MonoBehaviour.m_Name == "" && m_MonoBehaviour.m_Script.TryGet(out var script))
                    info.Name = script.m_ClassName;
                else
                    info.Name = m_MonoBehaviour.m_Name;
                info.IsExportable = true;
                break;
            case NamedObject m_NamedObject:
                info.Name = m_NamedObject.m_Name;
                info.IsExportable = false;
                break;
            default:
                info.Name = $"{info.Type} #{info.PathID}";
                info.IsExportable = false;
                break;
        }

        if (string.IsNullOrEmpty(info.Name))
        {
            info.Name = $"{info.Type} #{info.PathID}";
        }

        return info;
    }
}

/// <summary>
/// Result of a load operation.
/// </summary>
public class LoadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SerializedFileCount { get; set; }
    public int ObjectCount { get; set; }
    public List<string> InvalidPaths { get; set; } = new();
}

/// <summary>
/// Information about a single asset (renamed to avoid conflict with AssetStudio.AssetInfo).
/// </summary>
public class McpAssetInfo
{
    public long PathID { get; set; }
    public string SourceFile { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int ClassID { get; set; }
    public long ByteSize { get; set; }
    public string Container { get; set; } = string.Empty;
    public bool IsExportable { get; set; }

    /// <summary>
    /// Reference to the underlying AssetStudio Object (not serialized).
    /// </summary>
    public Object? Asset { get; set; }
}

/// <summary>
/// Logger adapter for AssetStudio core library.
/// </summary>
internal class McpLogger : ILogger
{
    private readonly ILogger<AssetManagerService> _logger;

    public McpLogger(ILogger<AssetManagerService> logger)
    {
        _logger = logger;
    }

    public void Log(LoggerEvent loggerEvent, string message)
    {
        switch (loggerEvent)
        {
            case LoggerEvent.Error:
                _logger.LogError(message);
                break;
            case LoggerEvent.Warning:
                _logger.LogWarning(message);
                break;
            case LoggerEvent.Info:
                _logger.LogInformation(message);
                break;
            case LoggerEvent.Debug:
                _logger.LogDebug(message);
                break;
            case LoggerEvent.Verbose:
                _logger.LogTrace(message);
                break;
        }
    }
}
