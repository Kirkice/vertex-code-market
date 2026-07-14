using System.ComponentModel;
using AssetStudio;
using AssetStudio.McpServer.Services;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer.Tools;

[McpServerToolType]
public class GetHierarchyTool
{
    private readonly AssetManagerService _assetManager;

    public GetHierarchyTool(AssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    [McpServerTool, Description("Get the GameObject scene hierarchy tree from loaded assets. Returns a tree structure showing parent-child relationships, with Transform position/rotation/scale data. Useful for understanding scene structure and analyzing rendering batch possibilities.")]
    public string GetHierarchy(
        [Description("Optional: Maximum depth of the hierarchy tree to return. Default is 10.")] int maxDepth = 10,
        [Description("Optional: Filter by source file name to get hierarchy from a specific file.")] string? sourceFile = null)
    {
        if (!_assetManager.IsLoaded)
            return "No assets loaded. Use the 'load_assets' tool first.";

        var allAssets = _assetManager.GetAllAssets();
        var gameObjects = new List<GameObject>();
        var transforms = new Dictionary<long, Transform>();

        foreach (var assetInfo in allAssets)
        {
            if (sourceFile != null && !string.Equals(assetInfo.SourceFile, sourceFile, StringComparison.OrdinalIgnoreCase))
                continue;

            if (assetInfo.Asset is GameObject go)
                gameObjects.Add(go);
            else if (assetInfo.Asset is Transform t)
                transforms[t.m_PathID] = t;
        }

        // Build hierarchy: find root GameObjects (those whose Transform has no father)
        var roots = new List<GameObject>();
        var goTransformMap = new Dictionary<long, Transform>();

        foreach (var go in gameObjects)
        {
            if (go.m_Transform != null)
            {
                goTransformMap[go.m_Transform.m_PathID] = go.m_Transform;
            }
        }

        foreach (var go in gameObjects)
        {
            if (go.m_Transform != null)
            {
                var father = go.m_Transform.m_Father;
                if (!father.TryGet(out _))
                {
                    roots.Add(go);
                }
            }
        }

        // Build tree
        var tree = new List<object>();
        foreach (var root in roots.OrderBy(r => r.m_Name))
        {
            tree.Add(BuildNode(root, 0, maxDepth, goTransformMap));
        }

        var result = new
        {
            totalGameObjects = gameObjects.Count,
            rootCount = roots.Count,
            maxDepth,
            hierarchy = tree
        };

        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    private static object BuildNode(GameObject go, int depth, int maxDepth, Dictionary<long, Transform> transformMap)
    {
        var node = new Dictionary<string, object?>
        {
            ["name"] = go.m_Name,
            ["pathId"] = go.m_PathID,
            ["depth"] = depth
        };

        // Transform data
        if (go.m_Transform != null)
        {
            var t = go.m_Transform;
            node["position"] = new { x = t.m_LocalPosition.X, y = t.m_LocalPosition.Y, z = t.m_LocalPosition.Z };
            node["rotation"] = new { x = t.m_LocalRotation.X, y = t.m_LocalRotation.Y, z = t.m_LocalRotation.Z, w = t.m_LocalRotation.W };
            node["scale"] = new { x = t.m_LocalScale.X, y = t.m_LocalScale.Y, z = t.m_LocalScale.Z };
        }

        // Components summary
        var componentTypes = new List<string>();
        if (go.m_MeshRenderer != null) componentTypes.Add("MeshRenderer");
        if (go.m_SkinnedMeshRenderer != null) componentTypes.Add("SkinnedMeshRenderer");
        if (go.m_Animator != null) componentTypes.Add("Animator");
        if (go.m_Animation != null) componentTypes.Add("Animation");
        if (componentTypes.Count > 0)
            node["components"] = componentTypes;

        // Children
        if (depth < maxDepth && go.m_Transform?.m_Children != null)
        {
            var children = new List<object>();
            foreach (var childPtr in go.m_Transform.m_Children)
            {
                if (childPtr.TryGet(out var childTransform))
                {
                    if (childTransform.m_GameObject.TryGet(out var childGo))
                    {
                        children.Add(BuildNode(childGo, depth + 1, maxDepth, transformMap));
                    }
                }
            }
            if (children.Count > 0)
                node["children"] = children;
        }
        else if (go.m_Transform?.m_Children != null && go.m_Transform.m_Children.Length > 0)
        {
            node["childCount"] = go.m_Transform.m_Children.Length;
            node["truncated"] = true;
        }

        return node;
    }
}
