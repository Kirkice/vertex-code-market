# URP 项目检测与降级规则

## 检测顺序

### 1. 确认 Unity 项目

查找 `ProjectSettings/ProjectVersion.txt`、`Assets/`、`Packages/manifest.json`。若没有 Unity 项目结构，不使用 Unity/URP 专用模板。

### 2. 确认管线

- Built-in：查找 `GraphicsSettings`、`OnRenderImage`、`CommandBuffer` 等旧入口。
- URP：查找 `com.unity.render-pipelines.universal`、`UniversalRenderPipelineAsset`、`ScriptableRendererFeature`。
- HDRP：查找 `com.unity.render-pipelines.high-definition`、`CustomPass`、`HDRenderPipelineAsset`。

如果多个管线并存，按当前 Graphics Settings、Quality Settings 和场景实际使用的 Pipeline Asset 判断，不能只看依赖声明。

### 3. 确认版本

按可靠性排序读取：

1. 当前项目 `Packages/manifest.json`
2. `packages-lock.json` 中解析后的版本
3. 本地 URP 包 `package.json`
4. 工程代码中的 API 证据
5. 文件夹名称或用户描述

版本冲突时报告冲突，不静默选择一个版本。

### 4. 确认扩展路径

搜索以下符号并记录文件位置：

- `ScriptableRendererFeature`
- `ScriptableRenderPass`
- `AddRenderPasses`
- `RecordRenderGraph`
- `Execute`
- `renderer.EnqueuePass`
- `ConfigureInput`

同时查找 Renderer Data 资产，确认 Feature 是否真的被挂载。

### 5. 确认 URP 14 过渡状态

如果发现 Unity 2022.3 / URP 14.x：

- 分别统计 `Execute` 和 `RecordRenderGraph` 的实现数量。
- 检查 RenderGraph 命名空间是否为 `UnityEngine.Experimental.Rendering.RenderGraphModule`。
- 检查 `RecordRenderGraph` 是否接收 `ref RenderingData`，不要误套 URP 17 的 `ContextContainer`。
- 如果目标 Feature 只实现 `Execute`，默认按 Execute-first 生成方案。
- 同时记录 RTHandle、TemporaryRT 和 BuiltinRenderTextureType 的使用情况。

### 6. 确认 URP 12 传统路径

如果发现 URP 12.x：

- 搜索 `RecordRenderGraph` 和 `RenderGraphModule`；没有结果时按 Execute-first 处理。
- 确认 `ScriptableRenderPass.Execute` 是目标 Pass 的实际入口。
- 检查 CommandBuffer、临时 RT、`OnCameraCleanup` 和相机栈清理。
- 不要因为用户提出 RenderGraph 需求就把 URP 15/17 API 回填到 URP 12 工程；应先说明版本边界并提供传统路径。

## 置信度

| 级别 | 条件 |
|---|---|
| High | manifest、package.json 和项目 API 证据一致 |
| Medium | 版本文件明确，但 Renderer/调用路径未完全确认 |
| Low | 只有用户描述、文件夹名称或通用经验 |

## 降级规则

- **版本未知**：只给架构方案和检测命令，不给不可验证的完整 API 代码。
- **管线未知**：先执行 Unity 渲染栈识别，不直接套 URP 模板。
- **Renderer 未知**：分别列出 Universal Renderer、2D Renderer、Deferred 的差异。
- **RenderGraph 未确认**：优先复用项目已有 Pass；没有证据时不要假设 `RecordRenderGraph`。
- **输入资源未确认**：先增加资源可视化/验证步骤，不直接采样 Camera Color、Depth 或 Normal。
- **跨版本迁移**：先保留原实现作为基线，再逐 Pass 迁移并比较 Frame Debugger/RenderDoc 结果。
- **源码不可用**：明确标注“未从项目源码确认”，并降低结论置信度。
- **URP 14 混合路径**：包内存在 RenderGraph 文件不代表自定义 Feature 已走 RenderGraph；必须检查 Feature/Pass 的实际方法实现和 Renderer 调用路径。

## 输出模板

```text
渲染栈：URP / Built-in / HDRP（证据：...）
Unity 版本：...（置信度：...）
URP 版本：...（置信度：...）
Renderer：...（证据：...）
当前扩展路径：Execute / RecordRenderGraph / 未确认
可直接使用的 API：...
需要项目确认的假设：...
降级实现：...
验证步骤：...
```
