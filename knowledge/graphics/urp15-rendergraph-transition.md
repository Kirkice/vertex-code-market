# URP 15.0.6 RenderGraph 过渡参考

## 适用范围

本条目只记录从 URP 15.0.6 包源码确认的公开架构事实和 API 形态，不包含任何项目名称、项目算法、业务资源或敏感实现。

## 版本事实

- URP：15.0.6
- Unity：2023.1（由包 `package.json` 的 `unity` 字段声明）
- Core：15.0.6
- ShaderGraph：15.0.6
- RenderGraph 命名空间：`UnityEngine.Experimental.Rendering.RenderGraphModule`

## Pass API 形态

URP 15 仍保留传统执行入口：

```csharp
public abstract void Execute(
    ScriptableRenderContext context,
    ref RenderingData renderingData);
```

同时提供 RenderGraph 记录入口：

```csharp
public virtual void RecordRenderGraph(
    RenderGraph renderGraph,
    FrameResources frameResources,
    ref RenderingData renderingData);
```

与 URP 14 相比，URP 15 的重要适配点是 `FrameResources` 参数。不要将 URP 14 的双参数签名或 URP 17 的 `ContextContainer` 签名混用。

## 选择规则

1. 先确认目标 Renderer 是否调用 RenderGraph 路径。
2. 传统兼容 Pass 实现 `Execute`；RenderGraph Pass 实现 URP 15 的三参数 `RecordRenderGraph`。
3. 读取资源时沿用项目和当前包源码中的 `FrameResources` 访问方式。
4. 不将旧式 CommandBuffer 操作直接包装成 RenderGraph 代码；需要声明资源读写和生命周期。
5. 版本未知时，以工程中现有 Pass 的签名为准，并报告未确认的 API 假设。

## 检测清单

- 搜索 `RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)`。
- 确认命名空间是否为 `UnityEngine.Experimental.Rendering.RenderGraphModule`。
- 检查 `ScriptableRenderer.RecordRenderGraph` 的调用链。
- 检查目标 Feature 是否仍通过 `renderer.EnqueuePass` 注入。
- 检查 `Execute` 与 `RecordRenderGraph` 是否同时存在，以及当前 Renderer 的实际路径。

## 版本边界

| 版本 | 记录入口特征 |
|---|---|
| URP 14 | `RecordRenderGraph(RenderGraph, ref RenderingData)` |
| URP 15 | `RecordRenderGraph(RenderGraph, FrameResources, ref RenderingData)` |
| URP 17+ | API 形态进一步变化，必须重新扫描项目和包源码 |

