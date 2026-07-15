# 跨版本 URP 图形工程方法

## 目标

本知识适用于 Unity URP 项目，不把任何单一 URP 版本当作默认实现。Agent 必须先识别项目事实，再选择 API、模板和验证方式。

## 三层模型

### 第一层：跨版本原则

这些原则通常跨 URP 版本成立：

- 先确认渲染栈、Unity 版本、URP 版本、Renderer 类型和目标平台。
- 把功能拆成 Renderer 配置、Feature/Pass、资源依赖和 Shader 四层。
- 先定义 Pass 的输入、输出和执行时机，再选择 API。
- 区分瞬时资源与跨帧资源；不要把一帧内的资源句柄当作持久资源保存。
- 修改前先检查现有工程中的实现和包源码，项目事实优先于记忆和示例。
- 验证顺序为：Pass 是否注入、资源是否正确、Shader 是否匹配、结果是否正确、性能是否可接受。

### 第二层：版本适配

API、Pass 生命周期、资源访问方式和 RenderGraph 支持程度随 URP 版本变化。知识条目必须声明适用版本；示例代码必须声明是旧式 Execute 路径还是 RenderGraph 路径。

### 第三层：项目事实校验

生成或修改代码前，优先扫描：

1. `Packages/manifest.json` 和 `Packages/packages-lock.json`
2. `ProjectSettings/ProjectVersion.txt`
3. `Packages/com.unity.render-pipelines.universal*/package.json`
4. `UniversalRenderPipelineAsset`、`UniversalRendererData` 和 Renderer Feature 资源
5. 工程中已有的 `RecordRenderGraph`、`ScriptableRenderPass.Execute` 和 `EnqueuePass`

如果项目代码和文档冲突，以项目代码和当前包源码为准，并在结论中标明证据来源。

## 版本未知时的安全策略

- 不直接生成特定版本的完整代码。
- 先输出需要确认的 API 事实和扫描结果。
- 优先使用项目中已存在的扩展模式进行复制和最小修改。
- 无法确认时，提供旧式路径和 RenderGraph 路径的差异，而不是混用两套 API。
- 对内部 API、包内 API 和公开 API 分级标注：`public`、`package-internal`、`project-local`。

## 任务路由

| 用户目标 | 首先识别 | 主要落点 |
|---|---|---|
| 新增全屏效果 | URP 版本、Renderer 类型、颜色输入是否可采样 | Renderer Feature + Pass + Shader |
| 读取深度/法线 | 当前 Renderer 是否生成对应输入 | Pass 输入配置 + Shader |
| 修改后处理 | 注入点、相机堆叠、HDR 和动态分辨率 | Post-process Pass |
| 性能优化 | RenderGraph 是否启用、Pass 合并、临时纹理和带宽 | Pass 资源声明 + Renderer 配置 |
| 排查 Feature 不生效 | Feature 是否挂载、是否激活、条件是否提前返回 | Renderer Data + Feature |

## 必须输出的兼容性信息

每次给出 URP 实现方案时，说明：

- 已确认的 Unity/URP 版本
- 已确认的 Renderer 类型和渲染路径
- 使用的 API 路径
- 不适用的旧版本或新版本路径
- 需要在工程中验证的假设
- 验证步骤和失败时的降级方案

