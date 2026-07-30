# URP ShaderLibrary 通用架构

## 1. 架构目标

ShaderLibrary 不是零散函数集合，而是连接引擎输入、Renderer 协议、材质模型和 GPU 平台的分层接口。理解它时应区分：

1. **稳定概念**：坐标空间、表面数据、BRDF、光、阴影、GI、屏幕空间资源和输出协议。
2. **项目实现**：具体文件、宏、字段、函数签名、Keyword 和 Buffer 布局。
3. **兼容实现**：旧名称、旧重载和平台特殊分支。

通用知识只描述第一层及调查方法；后两层必须从目标项目确认。

---

## 2. 分层模型

### 2.1 平台与 SRP Core 基础层

职责：

- HLSL 基础类型和数学函数。
- 纹理、Sampler、Load、Sample、Gather 和 Shadow Compare 抽象。
- 平台能力、深度范围、UV 原点、Framebuffer Fetch 和 Native Render Pass 差异。
- 打包、材质数学、空间变换和全局 Sampler。

规则：

- 管线层应复用 SRP Core 抽象，不重复实现平台宏。
- 不直接把某后端的纹理类型或坐标约定写进通用材质函数。

### 2.2 管线核心入口层

职责：

- 聚合必要的 Core Include。
- 定义管线级纹理抽象，尤其是普通二维纹理与 XR Texture Array 的统一访问。
- 提供全局 Mip Bias、Framebuffer Input 和通用顶点输入结构。
- 引入管线 Input、变换辅助和必要兼容层。

该层是平台差异进入 URP Shader 的主要边界。业务 Shader 不应绕过它自行重建 XR 或采样逻辑。

### 2.3 引擎与管线输入层

可进一步分为：

- **引擎输入**：时间、相机、投影、屏幕、对象矩阵、每 Draw 数据、Lightmap、SH、Reflection Probe 和 Stereo 数据。
- **管线输入**：主光、附加光、Cluster 参数、AO 参数、全局 Mip Bias、Rendering Layer 和管线资源。
- **实例化输入**：传统 Instancing、DOTS/GPU-driven 和 VFX 矩阵覆盖。

关键约束：

- Include 顺序可能是协议的一部分，尤其是 Constant Buffer 与实例化宏。
- 每 Draw Constant Buffer 的字段顺序和布局不可随意调整。
- 矩阵应通过管线矩阵宏和变换 Helper 访问，避免绕过 Stereo 或实例化覆盖。

### 2.4 语义辅助层

职责：

- Object、World、View、Clip、NDC 和 Screen 空间转换。
- Camera Position、View Direction 和投影类型。
- 法线/TBN 初始化和归一化。
- Alpha Clip、Alpha Premultiply、Fog、Depth 转换与 Rendering Layer 编码。

该层将平台和管线细节封装为语义函数。上层代码应表达“获取归一化屏幕 UV”而不是手写屏幕参数计算。

### 2.5 材质与表面层

职责：

- 声明材质纹理和 Sampler。
- 采样 Base、Normal、Emission 等输入。
- 应用 Alpha、Cutoff、Normal Scale 和材质 Keyword。
- 构建统一 Surface 描述。

边界：

- 此层不应自行遍历光源。
- 表面结构表达材质结果，而不是 Renderer Attachment 布局。
- Shader Graph 或其他生成系统可能依赖结构体字段；字段变化属于协议变更。

### 2.6 BRDF 层

职责：

- 将 Surface 参数转换为预计算 BRDF 数据。
- 统一 Metallic 与 Specular 工作流。
- 保存光循环不变量，减少逐光重复计算。
- 计算直接 BRDF、环境 BRDF和可选材质层。

典型流向：

```text
SurfaceData
  -> Initialize BRDF
  -> BRDFData
  -> Direct BRDF / Environment BRDF
```

BRDF 层不负责取得光列表或阴影资源。

### 2.7 光照子系统层

#### 实时光

抽象一个 Light 通常包含方向、颜色、距离衰减、阴影衰减和 Layer Mask。其来源可能是：

- 主光常量。
- 每对象附加光索引。
- Structured Buffer。
- Cluster/Tile 光列表。

光照调用方应使用目标项目提供的 Light 获取与 Loop 抽象，而不是直接索引内部数组。

#### 阴影

阴影层负责：

- 世界位置到 Shadow Coord。
- 主光级联选择。
- 附加光 Slice 选择。
- 硬阴影和软阴影过滤。
- Shadow Bias 和 Near Plane Clamp。
- 实时阴影、Shadow Mask 与距离 Fade 混合。

阴影纹理、矩阵数组、Slice 索引和质量参数均由 C# 对端生产。

#### 间接光

GI 层负责：

- 静态和动态 Lightmap。
- SH/Ambient Probe。
- Probe Volume。
- Reflection Probe、Box Projection、Rotation 和 Blending。
- 环境 BRDF 与混合光照修正。

同一语义可能有多个数据源，通常由 Keyword、Renderer 配置和资源有效性选择。

#### AO 与 Decal

- AO 把屏幕空间遮蔽和材质 Occlusion 组合为直接/间接因子。
- Decal/DBuffer 在光照前修改 Surface 或材质属性。
- 两者都依赖上游 Renderer Pass，不能仅靠 Shader Keyword 保证资源有效。

### 2.8 光照聚合层

聚合层连接 Surface、Input、BRDF、Light、GI、Shadow、AO 和 Debug，并输出 LightingData 或最终颜色。

典型 PBR 路径：

```text
Initialize BRDFData
  -> Resolve Shadow Mask and AO
  -> Get Main Light
  -> Mix Realtime and Baked GI
  -> Evaluate Environment/GI
  -> Evaluate Main Light
  -> Iterate Additional Lights
  -> Add Vertex Lighting and Emission
  -> Debug filtering / Fog / Final output
```

高层入口适合材质 Pass 调用；其内部文件不应被任意拆开重组。

### 2.9 屏幕空间声明层

独立声明层通常暴露：

- Camera Depth。
- Camera Normals。
- Opaque Color。
- Rendering Layers。
- Screen Space AO、Shadow 或 Irradiance。

调用者必须同时满足：

1. Renderer 在正确时机生产资源。
2. Pass 声明或请求了该输入。
3. Shader 使用正确的纹理维度和坐标 Helper。
4. 当前相机、Renderer、MSAA 和平台路径支持该资源。

### 2.10 Deferred/GBuffer 层

Deferred 是强协议区域，包含：

- 静态和条件 Attachment 索引。
- 每通道材质语义。
- Material Flags 位布局。
- Normal 编码。
- Surface/BRDF 打包和解包。
- Framebuffer Fetch 与普通纹理读取双路径。
- Attachment 格式提示和 C# 实际格式。

修改原则：

```text
C# Attachment Allocation
  == HLSL Target Index
  == HLSL Channel Encoding
  == Deferred Reader Decode
  == Shader Compiler Format Hint
```

任一端不一致都可能产生无编译错误的视觉损坏。

### 2.11 适配与工具层

包括 Shader Graph、VFX、Particle、Meta、Motion Vector、Debug 和 Rendering Layers 等适配入口。这些层应把上游系统的数据转换成 ShaderLibrary 稳定语义，不应让特殊系统逻辑渗透到所有材质。

---

## 3. Include 图阅读方法

### 3.1 从入口向下

1. 找到 Shader Pass 的首个管线 Include。
2. 记录其直接 Include，不把传递依赖误认为调用方的显式契约。
3. 找到结构体与高层函数的定义文件。
4. 识别哪些符号来自 SRP Core、URP、生成文件或项目自定义库。
5. 检查条件 Include 和 Keyword 是否改变依赖图。

### 3.2 从符号向上

对每个关键符号追踪：

```text
定义位置
  -> 条件编译守卫
  -> 直接调用者
  -> Shader Pass
  -> C# 数据上传或资源生产者
```

只阅读 HLSL 不能证明全局变量何时有效；必须继续追踪 C# 的 Property ID、SetGlobal/SetBuffer、Attachment 创建和 Keyword 设置。

### 3.3 识别聚合入口和叶子模块

- 聚合入口：面向 Shader Pass，Include 多个子系统。
- 叶子模块：提供单一职责函数或声明。
- 内部公共模块：只供成对入口共享，通常不应直接 Include。
- 兼容模块：旧符号转发，不应成为新依赖。

---

## 4. 数据协议边界

### 4.1 Material Properties 到 SurfaceData

Shader 应有显式初始化函数，将纹理和 Material CBUFFER 转换为 SurfaceData。这样可以：

- 集中 Alpha 和 Normal 规则。
- 让 Forward、GBuffer、DepthNormals 和 Meta 共享材质语义。
- 防止不同 Pass 对同一 Property 采用不同解释。

### 4.2 Vertex/Varyings 到 InputData

InputData 应在 Fragment 边界统一构建：

- 插值并归一化法线/TBN。
- 计算 View Direction。
- 选择 Shadow Coord 来源。
- 采样或组合 Baked GI 与 Shadow Mask。
- 计算归一化屏幕 UV。
- 填充 Debug 所需数据。

### 4.3 C# 到全局输入

任何新增全局字段都需要确认：

- Property ID 唯一且名称一致。
- 每帧、每相机、每 Draw 或每材质更新频率正确。
- CommandBuffer 设置发生在消费 Pass 之前。
- Stereo 每眼数据和 Camera Stack 不被后续相机污染。
- Buffer 容量、Stride、元素类型和索引范围一致。

### 4.4 Renderer 到屏幕空间纹理

资源有效性由 Renderer 拓扑决定。Shader 声明只表示“可绑定”，不表示“已生产”。新增采样需求时，应先修改或配置生产端，再实现消费端。

---

## 5. Forward 与 Deferred 的共享和差异

### 5.1 应共享

- Material Property 和纹理语义。
- SurfaceData 初始化。
- Alpha Clip 和 Normal 处理。
- BRDF 参数转换。
- Lightmap/Probe 输入准备。
- Debug 材质数据。

### 5.2 不应强行共享

- 最终 Fragment 输出类型。
- 光循环发生的位置。
- GBuffer Packing/Unpacking。
- Deferred Material Flags。
- Attachment 和 Framebuffer Fetch 细节。

### 5.3 修改检查

更改材质模型时至少检查：

- Forward Lit 输出。
- GBuffer 写入。
- Deferred 读取与光照。
- DepthNormals。
- ShadowCaster 的 Alpha Clip。
- Meta/Lightmapping。
- Motion Vector 的 Alpha Clip。
- Debug Display 和 Shader Graph 适配。

---

## 6. 平台抽象边界

### 6.1 XR

XR 可能把二维纹理变为 Texture Array，并使用 Eye Index 选择 Slice。所有 Camera Texture、Framebuffer Input 和屏幕空间采样应走管线的 X 纹理抽象与 Stereo Helper。

### 6.2 动态分辨率与 RTHandle

屏幕尺寸、纹理有效区域和历史帧 Scale 可能不同。使用管线提供的 Scaled Screen Params、RTHandle Scale 和归一化坐标转换，不能默认纹理全尺寸等于当前 Viewport。

### 6.3 UV 原点与预旋转

平台可能存在 Y Flip 或显示方向预旋转。坐标变换应集中在屏幕空间 Helper 中，避免各效果重复且顺序不一致地修正 UV。

### 6.4 深度

深度可能为 Reversed Z，Clip 范围也随后端变化。使用管线深度宏和线性化 Helper，不手写固定公式。

### 6.5 Framebuffer Fetch

读取 Attachment 可能走 Native Render Pass Input，也可能回退为纹理。调用者应依赖统一 Load 抽象，并在 Renderer 侧建立正确 Attachment/Input Attachment 协议。

---

## 7. 源码定制边界

优先在项目 Shader Library 中增加薄适配层，而不是复制整份管线库。只有当修改以下核心协议时才考虑 Local/Embedded 包定制：

- 全局 Input 或每 Draw Buffer 布局。
- Light/Shadow Buffer 协议。
- SurfaceData 或公共 BRDF 契约。
- GBuffer/DBuffer Attachment 和编码。
- XR/Framebuffer Fetch/平台抽象。
- Shader Graph 或内置材质必须共同使用的新能力。

定制时保留上游文件结构和最小差异，并记录所有 C#、HLSL、生成代码和测试对端。
