# URP 版本适配矩阵

这是一份决策矩阵，不是完整 API 文档。具体项目始终优先使用本地包源码和已有代码。

## 主要版本族

| 版本族 | 常见 Unity | 默认判断 | 代码策略 |
|---|---|---|---|
| URP 7-10 | Unity 2019-2020 | 传统 ScriptableRenderPass 为主 | 优先检查 `Execute`、旧式临时 RT 和 Renderer Feature 生命周期 |
| URP 12 | Unity 2021 | 传统路径成熟，部分新资源接口出现 | 复制项目现有 Pass；不要假设 RenderGraph 可用 |
| URP 14 | Unity 2022 | `Execute` 仍是自定义 Pass 必需路径，RenderGraph 过渡代码并存 | 默认 Execute-first；只有确认 Renderer 与 Pass 支持时才使用 URP 14 RenderGraph |
| URP 15-16 | Unity 2023 | RenderGraph 相关能力逐步增强 | 同时检查 `Execute` 与 `RecordRenderGraph`，以工程配置为准 |
| URP 17+ | Unity 6 | RenderGraph 是重点路径 | 优先使用 `RecordRenderGraph`，但仍需检查项目是否保留兼容路径 |

## 能力判断矩阵

| 能力 | 不能从版本号直接推断 | 必须检查的项目事实 |
|---|---|---|
| RenderGraph | 包版本之外还受项目设置和 API 可用性影响 | `RecordRenderGraph`、`RenderGraphModule`、URP 设置和现有 Pass |
| Camera Color 采样 | 受注入时机和中间纹理策略影响 | 当前目标是否 BackBuffer、`requiresIntermediateTexture`、Renderer 配置 |
| 深度/法线输入 | 受 Renderer 和 Feature 配置影响 | `ConfigureInput`、Depth/DepthNormal Pass、实际资源绑定 |
| Forward/Deferred | 由 Renderer Data 和 URP Asset 决定 | `UniversalRendererData` 的 rendering mode 和实际 Renderer |
| Native Render Pass/Tile | 受平台、Renderer 设置和 Pass 资源声明影响 | 平台、Tile Only 设置、Pass 是否使用 Unsafe/显式 RT |
| XR/Camera Stacking | 不是所有 Pass 自动兼容 | 相机类型、XR 配置、Base/Overlay 关系和资源生命周期 |

## API 选择规则

1. 若工程已有 `RecordRenderGraph`，优先沿用其 FrameData、资源句柄和 Pass 模式。
2. 若工程只有 `Execute`，不要凭版本号强行迁移；先按当前模式实现，再单独规划迁移。
3. 若两种方法都存在，查看 Renderer 实际调用链、编译宏和包源码，确认哪个路径生效。
4. `RenderPassEvent`、输入依赖和资源声明必须以目标版本的枚举及方法签名为准。
5. 发现 API 不存在时，停止生成该 API 的代码，并回退到工程已验证的相邻模式。

## 迁移风险

- 旧式 `Execute` 代码不能机械替换为 `RecordRenderGraph`。
- 临时 RT、RTHandle、TextureHandle 的所有权和生命周期不同。
- 旧式 CommandBuffer/Blit 可能绕过 RenderGraph 的依赖分析。
- Pass 的注入事件名称相同，不代表资源可用时机相同。
- Shader Pass Tag、关键字和变体策略也可能随版本及 Renderer 路径变化。

## URP 14 特别规则

- `ScriptableRenderPass` 仍要求实现 `Execute`；`RecordRenderGraph` 是另一路径，不是 `Execute` 的自动适配器。
- URP 14 使用 `UnityEngine.Experimental.Rendering.RenderGraphModule`，不要套用 URP 17 的命名空间。
- URP 14 的 RenderGraph 记录方法使用 `ref RenderingData`，不要直接替换为 URP 17 的 `ContextContainer frameData`。
- URP 14 工程可以使用 RTHandle，但仍可能保留 `GetTemporaryRT`、`BuiltinRenderTextureType` 和条件编译分支。
- URP 14 的具体项目实现仍必须以当前工程源码和包源码为准，不应从版本号推断 Renderer 或资源策略。

## URP 12 特别规则

- URP 12.1.7 的自定义 Pass 以 `Execute(ScriptableRenderContext, ref RenderingData)` 为主。
- 对 URP 12 项目不要生成 `RecordRenderGraph`，除非项目提供了明确的自定义扩展或包覆盖证据。
- 资源生命周期重点检查 CommandBuffer、临时 RT、Renderer 目标句柄、`OnCameraCleanup` 和相机栈清理。
- 包内没有 RenderGraph 证据时，应优先采用 Execute-first 降级方案。
- 通用参考见 `graphics/urp12-scriptable-render-pass-reference.md`。

