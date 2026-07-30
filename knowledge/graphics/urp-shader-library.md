# Unity URP ShaderLibrary 工程知识库

> 目标：为开发者和 LLM 提供不绑定具体 Unity 或 URP 发布版本的 ShaderLibrary 使用、扩展、审查与验收依据。
>
> 核心模型：**稳定分层 + 目标项目事实检测 + C#/HLSL 协议校验**。
>
> 本知识库不替代目标项目源码。宏、结构体字段、函数签名、Keyword、Buffer 布局和 Pass 协议必须以目标项目锁定包源码、编译结果和运行验证为准。

---

## 1. 文档结构

| 文档 | 职责 |
|---|---|
| [`urp-shader-library.md`](urp-shader-library.md) | 权威性、事实门禁、功能地图与标准工作流 |
| [`urp-shader-library-architecture.md`](urp-shader-library-architecture.md) | Include 分层、数据流、资源边界与协议关系 |
| [`urp-shader-engineering.md`](urp-shader-engineering.md) | Shader 代码结构、精度、Variant、平台和 C#/HLSL 工程规范 |
| [`urp-shader-validation-checklist.md`](urp-shader-validation-checklist.md) | 编译、视觉、平台、性能和协议验收门禁 |

自动执行工作流由 [`../../skills/graphics-base/write-shader/SKILL.md`](../../skills/graphics-base/write-shader/SKILL.md) 定义。

---

## 2. 权威性顺序

遇到资料冲突时按以下顺序判断：

1. 目标项目实际使用的 Shader、Material、Renderer、Renderer Feature 和 C# 绑定代码。
2. 锁定的 URP、SRP Core 及相关包源码。
3. Shader 编译输出、Unity Console、Frame Debugger、Profiler 和 GPU Capture。
4. 本知识库中的稳定架构和工程规范。
5. 与目标项目匹配的官方文档和 Samples。
6. 其他项目、其他包代际的示例、教程和经验。

禁止仅凭版本记忆或最新模板推断符号存在。当前包中存在的函数也不能自动视为其他目标项目的公共 API。

---

## 3. 强制 Shader 事实门禁

生成或修改 Shader 前，必须输出以下事实卡：

```text
渲染管线：URP / 未确认
目标 Shader 与 Pass：
Renderer 与 Rendering Mode：
目标平台与 Shader Target：
入口 Include：
本地可用结构体与函数签名：
需要的纹理、Sampler、Buffer 与 CBUFFER：
Keyword 与 Variant 来源：
C# 属性、Keyword、Buffer、Attachment 或 GBuffer 对端：
XR / 动态分辨率 / MSAA / HDR / 屏幕方向相关性：
证据：
未知项与假设：
```

若关键事实未确认：

- 不生成绑定未确认字段或签名的完整 Shader。
- 不复制其他包代际的 Include 路径、宏或结构体布局。
- 不自行声明与管线同名的全局资源来掩盖协议缺失。
- 优先查找目标项目中同一 Pass、同一材质模型和同一 Renderer 的可工作实现。
- 将未知项标为 BLOCKED，而不是猜测。

---

## 4. ShaderLibrary 功能地图

ShaderLibrary 通常可按职责理解为以下层次。具体文件名和可用符号仍需从目标项目确认。

| 层次 | 主要职责 | 使用边界 |
|---|---|---|
| 平台与核心入口 | 基础类型、平台宏、纹理采样、XR 纹理抽象、坐标与全局采样策略 | 作为多数管线 Shader 的底座；避免重复声明同名宏 |
| 管线与引擎输入 | 相机、矩阵、时间、光照、反射探针、Lightmap、实例化和每 Draw 数据 | 与 C# 上传和引擎内建布局强耦合 |
| 变换与屏幕空间 | Object/World/View/Clip 变换、视线、法线、深度、Fog、屏幕 UV | 必须处理 Y Flip、动态缩放、XR 和预旋转 |
| 材质表面层 | 材质纹理采样、Alpha、法线、Emission 和统一表面描述 | 将材质表示与光照实现解耦 |
| BRDF 层 | Metallic/Specular 工作流、粗糙度、直接与环境 BRDF、Clear Coat | 输入应来自已初始化的表面数据 |
| 实时光照层 | 主光、附加光、衰减、Cookie、Light Layer 和光循环 | 光列表存储和循环模型由项目事实决定 |
| 阴影层 | 阴影坐标、级联、过滤、Bias、Shadow Mask 和实时/烘焙混合 | 与 C# 阴影 Atlas、矩阵和参数布局同步 |
| 间接光层 | Lightmap、SH、Probe Volume、Reflection Probe 和环境反射 | 采样路径由 Keyword 与 Renderer 配置共同决定 |
| 屏幕空间输入 | Depth、Normals、Opaque Color、Rendering Layers、AO 等声明与采样 | 只有上游 Pass 确实生产并绑定资源时才有效 |
| Deferred/GBuffer | Attachment 索引、格式、材质标志、打包、读取和重建 | 写端、读端、C# Attachment 格式必须同时修改 |
| Decal/DBuffer | Decal 数据编码、MRT 选择和 Surface 应用 | 与 Renderer Feature、Keyword 和 Attachment 数同步 |
| Motion Vector | 当前/前帧变换、NDC Motion、对象与相机运动 | 需验证抖动、XR、Foveated 和历史矩阵协议 |
| Debug 与工具适配 | Debug Display、Shader Graph、VFX、Meta、粒子和实例化适配 | 只按目标工作流引入，避免污染普通材质 Variant |
| Deprecated 兼容层 | 旧名称和旧签名转发 | 仅用于迁移；新代码不得以此为模板 |

---

## 5. 稳定数据流

典型 Lit Shader 的概念数据流为：

```text
Material Properties / Textures
  -> SurfaceData
  -> BRDFData
  -> InputData + Light + Baked GI + Shadow + AO
  -> LightingData
  -> Final Color / GBuffer Output
```

各阶段职责：

1. **Material Inputs**：读取材质属性和纹理，只表达材质自身。
2. **SurfaceData**：统一 Albedo、Metallic/Specular、Smoothness、Normal、Emission、Occlusion、Alpha 等表面语义。
3. **InputData**：统一世界空间位置、法线、视线、阴影坐标、GI、Fog 和屏幕空间坐标等几何/管线语义。
4. **BRDFData**：把材质参数转换为适合光循环复用的 BRDF 不变量。
5. **Light/GI Evaluation**：处理主光、附加光、Shadow、Cookie、Light Layer、Lightmap、Probe 和 Reflection。
6. **LightingData**：分离 GI、主光、附加光、顶点光照和 Emission，支持 Debug 与最终组合。
7. **Output**：前向路径输出最终颜色；Deferred 路径输出约定的 GBuffer 和必要的附加 Attachment。

不得把某个包中的字段全集视为永恒协议。扩展数据结构前必须确认所有构造、写入、读取和 Debug 路径。

---

## 6. Include 选择原则

### 6.1 最小入口

- 只需要变换、平台和基础输入时，使用目标项目证明可用的核心入口。
- 需要完整 Lit 光照时，使用聚合光照入口，而不是手工拼接其内部依赖。
- 只需要相机 Depth、Normals 或 Opaque Color 时，优先使用对应声明层，不要引入完整光照库。
- 写 GBuffer 与读 GBuffer 应使用各自入口，不直接引用标记为内部公共实现的中间文件。

### 6.2 依赖方向

推荐依赖方向：

```text
平台/Core
  -> 全局 Input 与 Transform
  -> Surface / BRDF
  -> Shadow / Realtime Light / GI / AO
  -> Lighting 聚合
  -> Pass Shader 或适配层
```

禁止：

- 在基础层反向依赖业务 Pass。
- 通过 Include 顺序偶然获得未声明依赖。
- 重复 Include 多个聚合入口并依赖重复定义保护来维持编译。
- 新代码直接依赖 Deprecated 文件。

---

## 7. 跨项目不变量

### 7.1 C#/HLSL 是同一协议

以下内容必须成对审查：

- Property 名称、类型和默认值。
- Global/Material Keyword 名称、作用域和剔除策略。
- Constant Buffer 字段顺序、类型、对齐和更新频率。
- Structured Buffer 元素布局和索引语义。
- Texture 维度、格式、MSAA、Array Slice 和 Sampler。
- ShaderTag、Pass 名称、Render State、Stencil 和 Rendering Layer。
- GBuffer/DBuffer 索引、通道、格式和编码。
- 当前/前帧矩阵、Motion Vector 与 History 资源。

### 7.2 屏幕空间必须走管线抽象

- 使用管线提供的归一化屏幕 UV、缩放和纹理抽象。
- 不手写假定左下原点、固定分辨率或普通二维纹理的采样代码。
- 读取屏幕空间资源前确认生产时机、绑定名、有效相机和坐标域。
- XR、动态分辨率、预旋转和 Foveated Rendering 相关路径必须单独验证。

### 7.3 数据初始化必须完整

- 结构体先零初始化或为每个字段赋值。
- 新增字段时同步所有构造函数、兼容重载、Debug、Forward、Deferred、Meta 和适用适配层。
- Alpha、Normal、Occlusion 和 Clear Coat 等默认值必须符合材质语义，不得依赖未初始化值。

### 7.4 Variant 必须可控

- 每个 Keyword 都要说明由谁设置、是全局还是局部、是否运行时切换、是否可剔除。
- 不为可以统一计算或低成本动态分支的逻辑无条件增加组合 Keyword。
- 不在 HLSL 中偷偷定义 C# 不知情的功能状态。
- 构建剔除后仍要验证运行时需要的 Variant 存在。

### 7.5 精度按语义选择

- 世界位置、大范围距离、深度、矩阵、运动和易溢出的中间量优先使用全精度。
- 颜色、单位向量和受控范围材质参数可在验证后使用半精度。
- `real` 表示由平台策略决定的精度时，必须考虑其在半精度平台上的范围。
- 不为追求表面上的性能统一把所有变量降为半精度。

### 7.6 废弃层只用于迁移

- Deprecated 名称和兼容重载不能作为新代码入口。
- 迁移时先找到当前目标项目推荐符号，再替换调用点。
- 不把兼容层中的发布说明、临时宏或旧协议写入通用设计。

---

## 8. 标准工作流

1. **检测**：完成 Shader 事实卡，确认本地 Include、符号、Pass 和 C# 对端。
2. **分类**：判断任务属于材质表面、光照、阴影、屏幕空间、Deferred、后处理、Motion 或适配层。
3. **溯源**：沿 Include 和调用链找到入口、数据生产者、消费者及 C# 上传点。
4. **设计**：定义数据流、Keyword、资源、坐标空间、精度、兼容范围和性能预算。
5. **实现**：遵守 [`urp-shader-engineering.md`](urp-shader-engineering.md)，保持最小 Include 和最小修改面。
6. **验收**：执行 [`urp-shader-validation-checklist.md`](urp-shader-validation-checklist.md) 的通用项和任务相关项。
7. **报告**：列出证据、修改文件、协议变化、Variant 影响、验证结果、未验证平台和剩余风险。

---

## 9. 完成定义

只有同时满足以下条件才能标记完成：

1. 使用的 Include、宏、结构体和函数已由目标项目源码确认。
2. Shader Pass 的生产/消费时序和资源绑定已确认。
3. C#/HLSL Property、Keyword、Buffer 和 Attachment 协议一致。
4. 数据结构初始化完整，Forward/Deferred/Debug 等适用路径已同步。
5. XR、动态分辨率、MSAA、HDR 和平台差异已评估并按适用范围验证。
6. Variant 增量、精度选择、纹理带宽和光循环成本有明确说明。
7. 编译与适用视觉测试通过；未执行项标为 N/A 或 BLOCKED。
8. 未把当前包特例、Deprecated 符号或具体签名提升为通用事实。
