using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class AnalyzeAnimationTool
{
    private readonly AssetManagerService _assetManager;

    public AnalyzeAnimationTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Analyze an AnimationClip asset. Returns duration, frame count, sample rate, curve count, compression type, and estimated CPU sampling cost. Useful for evaluating animation data size and identifying expensive animations.")]
    public string AnalyzeAnimation(
        [Description("The pathID of the AnimationClip asset.")] long pathId,
        [Description("The source file name of the AnimationClip asset.")] string sourceFile)
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var obj = _assetManager.FindAsset(pathId, sourceFile);
        if (obj is not AnimationClip clip)
            return $"AnimationClip not found: pathID={pathId}, sourceFile={sourceFile}";

        var result = new Dictionary<string, object?>
        {
            ["name"] = clip.m_Name,
            ["pathId"] = clip.m_PathID,
            ["sourceFile"] = clip.assetsFile.fileName,
            ["legacy"] = clip.m_Legacy,
            ["compressed"] = clip.m_Compressed,
            ["duration"] = clip.m_MuscleClip.m_StopTime - clip.m_MuscleClip.m_StartTime,
            ["startTime"] = clip.m_MuscleClip.m_StartTime,
            ["stopTime"] = clip.m_MuscleClip.m_StopTime,
            ["sampleRate"] = clip.m_SampleRate,
            ["wrapMode"] = clip.m_WrapMode
        };

        // Estimate frame count
        var duration = clip.m_MuscleClip.m_StopTime - clip.m_MuscleClip.m_StartTime;
        var estimatedFrames = (int)(duration * clip.m_SampleRate);
        result["estimatedFrames"] = estimatedFrames;

        // Clip data analysis
        if (clip.m_MuscleClip.m_Clip != null)
        {
            var clipData = clip.m_MuscleClip.m_Clip;

            // Streamed clip
            if (clipData.m_StreamedClip != null)
            {
                result["streamedCurveCount"] = clipData.m_StreamedClip.curveCount;
                result["streamedDataSize"] = clipData.m_StreamedClip.data?.Length * 4 ?? 0;
            }

            // Dense clip
            if (clipData.m_DenseClip != null)
            {
                result["denseFrameCount"] = clipData.m_DenseClip.m_FrameCount;
                result["denseCurveCount"] = clipData.m_DenseClip.m_CurveCount;
                result["denseSampleRate"] = clipData.m_DenseClip.m_SampleRate;
                result["denseDataSize"] = clipData.m_DenseClip.m_SampleArray?.Length * 4 ?? 0;
            }

            // Constant clip
            if (clipData.m_ConstantClip != null)
            {
                result["constantCurveCount"] = clipData.m_ConstantClip.data?.Length ?? 0;
            }
        }

        // Events
        if (clip.m_Events != null)
        {
            result["eventCount"] = clip.m_Events.Length;
            result["events"] = clip.m_Events.Select(e => new
            {
                time = e.time,
                name = e.functionName
            }).ToArray();
        }

        // Bounds
        if (clip.m_MuscleClip.m_Clip != null)
        {
            result["hasMuscleClip"] = true;
        }

        // Performance analysis
        var issues = new List<string>();
        if (duration > 10)
            issues.Add($"Long animation ({duration:F2}s). Consider splitting into shorter clips.");
        if (estimatedFrames > 1000)
            issues.Add($"High frame count ({estimatedFrames}). May increase memory usage.");
        if (clip.m_SampleRate > 60)
            issues.Add($"High sample rate ({clip.m_SampleRate}Hz). Consider reducing to 30Hz for non-critical animations.");

        result["issues"] = issues;
        result["issueCount"] = issues.Count;

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
