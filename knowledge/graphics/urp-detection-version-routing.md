# URP 检测与版本路由协议

> 本文是 Unity 图形 Skill 共享的唯一事实检测与 URP API 路由协议。具体版本参考只补充版本差异，不重复本流程。

## 1. 事实优先级

按以下顺序确认事实：

1. `ProjectSettings/ProjectVersion.txt`
2. `Packages/manifest.json` 与 `Packages/packages-lock.json`
3. 已解析 URP 包的 `package.json`
4. 当前项目的 `UniversalRenderPipelineAsset`、Renderer Data 与 Renderer Feature
5. 项目源码中的 `ScriptableRenderPass.Execute`、`RecordRenderGraph`、`AddRenderPasses`、`EnqueuePass`
6. 当前锁定版本的 URP/SRP Core 包源码

项目事实优先于记忆、示例和版本号推断。无法确认时必须标注置信度和未确认项，不生成绑定特定 API 的完整代码。

## 2. 渲染栈检测

- **Built-in RP**：查找 `GraphicsSettings`、`OnRenderImage`、`CommandBuffer`、`GrabPass`、Surface Shader。
- **URP**：查找 `com.unity.render-pipelines.universal`、`UniversalRenderPipelineAsset`、`ScriptableRendererFeature`、`ScriptableRenderPass`。
- **HDRP**：查找 `com.unity.render-pipelines.high-definition`、`HDRenderPipelineAsset`、`CustomPass`、Volume 配置。

如果多个管线并存，以 Graphics Settings、Quality Settings 和场景实际使用的 Pipeline Asset 为准，不能只看依赖声明。

## 3. URP 检查卡

涉及 URP 时，先记录：

```text
Unity 版本：
URP 版本：
Renderer 类型：Universal / 2D / Deferred / 自定义 / 未确认
实际 Renderer Asset：
目标平台：
当前 Pass 路径：Execute / RecordRenderGraph / 混合 / 未确认
资源模式：TemporaryRT / RTHandle / TextureHandle / 未确认
证据来源：
置信度：High / Medium / Low
```

## 4. Execute 与 RenderGraph 路由

- 只有 `Execute` 证据：按 Execute-first 实现，检查 CommandBuffer、临时 RT、目标句柄、`OnCameraCleanup` 和相机栈清理。
- 只有 `RecordRenderGraph` 证据：沿用项目已有的 FrameData、资源句柄、读写声明和 Pass 模式。
- 两者同时存在：确认 Renderer 的实际调用路径，不要把兼容实现误认为当前路径。
- 没有明确证据：先提供检测命令或架构方案，不假设 RenderGraph。

旧式 CommandBuffer/Blit 代码不能机械改名为 RenderGraph 代码；RenderGraph 实现必须重新声明资源读写、依赖和生命周期。

## 5. 版本差异入口

| URP | 默认策略 | 需要确认 |
|---|---|---|
| 7–10 | 传统 ScriptableRenderPass | `Execute`、临时 RT 和 Feature 生命周期 |
| 12 | Execute-first | 是否存在项目级 RenderGraph 扩展 |
| 14 | Execute 与 RenderGraph 过渡并存 | `RecordRenderGraph` 签名、Renderer 调用链、RTHandle/TemporaryRT |
| 15–16 | RenderGraph 能力增强但仍可能保留 Execute | `FrameResources`、实际 Renderer 路径 |
| 17+ | 优先检查 RenderGraph | 当前包 API、兼容路径和项目实际调用链 |

版本表只能用于缩小检查范围，不能替代项目源码和包源码验证。URP 12 与 URP 15 的详细 API 形态分别见 [`urp12-scriptable-render-pass-reference.md`](urp12-scriptable-render-pass-reference.md) 和 [`urp15-rendergraph-transition.md`](urp15-rendergraph-transition.md)。

## 6. 统一输出要求

每个 URP 方案或修改报告必须说明：

- 已确认的 Unity/URP 版本和证据；
- Renderer、渲染路径和实际 Pass 入口；
- 资源生产端、消费端和生命周期；
- 采用的 API 路径及版本范围；
- 平台、相机、XR、MSAA、HDR 和动态分辨率边界；
- 未验证项、降级方案和验证步骤。
