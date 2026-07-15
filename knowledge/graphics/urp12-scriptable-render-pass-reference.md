# URP 12.1.7 ScriptableRenderPass 参考

## 适用范围

本文只记录从 URP 12.1.7 包源码确认的通用 API 和判断规则，不包含任何具体项目的业务、算法、资源命名或敏感实现。

## 版本事实

- URP：12.1.7
- Unity：2021.2（由包 `package.json` 的 `unity` 字段声明）
- Core：12.1.7
- ShaderGraph：12.1.7
- 包内未发现 `RecordRenderGraph` 或 RenderGraph 模块引用。

## 执行模型

URP 12 的自定义 Pass 以传统 Scriptable Render Pass 为主，核心执行入口是：

```csharp
public abstract void Execute(
    ScriptableRenderContext context,
    ref RenderingData renderingData);
```

典型注入链路：

```text
ScriptableRendererFeature.Create()
    -> AddRenderPasses(renderer, ref renderingData)
        -> ConfigureInput(...)
        -> renderer.EnqueuePass(pass)
            -> Execute(context, ref renderingData)
```

## URP 12 代码策略

- 默认使用 `Execute`，不要生成 `RecordRenderGraph`。
- 资源管理通常围绕 `CommandBuffer`、`RenderTargetIdentifier`、临时 RT 和 Renderer 的目标句柄展开。
- 新增 Pass 前先确认 `RenderPassEvent`、输入依赖、目标纹理和清理生命周期。
- 需要深度或法线时，检查 `ConfigureInput` 和对应 Renderer/相机设置。
- 需要跨帧保存资源时，单独设计释放和相机栈生命周期，不要把帧内临时资源直接保存。

## 检测清单

1. 从 `manifest.json`、`packages-lock.json` 和本地包 `package.json` 确认 URP 版本。
2. 搜索 `ScriptableRendererFeature`、`ScriptableRenderPass`、`AddRenderPasses`、`EnqueuePass` 和 `Execute`。
3. 确认 Renderer Data 是否实际挂载了 Feature。
4. 检查是否存在项目自定义的兼容宏或本地包覆盖。
5. 检查 `OnCameraCleanup`、`OnFinishCameraStackRendering` 和资源释放逻辑。

## 与其他版本的边界

| 版本 | 自定义 Pass 重点 |
|---|---|
| URP 12 | Execute-first；不要假设 RenderGraph 存在 |
| URP 14 | Execute 与 RenderGraph 过渡路径并存，需要检查实际调用链 |
| URP 15 | RenderGraph 记录入口包含 `FrameResources`，仍保留 Execute |
| URP 17+ | API 形态继续变化，必须重新扫描当前包源码 |

