# URP Shader 工程规范

## 1. 适用范围

本规范适用于：

- 新增或修改 URP Shader、HLSL Include 和自定义材质模型。
- 使用 Depth、Normals、Opaque Color、AO、Shadow 或 GBuffer 等管线资源。
- 修改光照、阴影、GI、Deferred、Motion Vector 或 Debug 协议。
- 审查 Shader 正确性、兼容性、Variant、精度和性能。

具体 API 和符号必须先按 [`urp-shader-library.md`](urp-shader-library.md) 完成项目事实检测。

---

## 2. 推荐代码结构

每个手写 Shader 应尽量按以下职责组织：

```text
ShaderLab Properties
SubShader / Tags / Render State
  Pass
    Pragmas and Keywords
    Minimal Includes
    Material CBUFFER and Texture Declarations
    Attributes / Varyings
    InitializeSurfaceData
    InitializeInputData
    Vertex Entry
    Fragment Entry
```

项目公共代码建议拆分为：

```text
ProjectShaderLibrary/
  MaterialInput.hlsl       // Property、纹理、Surface 初始化
  Common.hlsl              // 项目稳定 Helper，不包含业务 Pass
  LightingExtension.hlsl   // 对管线光照入口的薄扩展
  Passes/
    ForwardPass.hlsl
    GBufferPass.hlsl
    DepthNormalsPass.hlsl
    ShadowCasterPass.hlsl
    MetaPass.hlsl
```

不要把所有 Pass、平台宏、材质采样和光照算法放进单一 Include。

---

## 3. Include 规范

### 必须

- 使用目标项目中存在的包路径和入口。
- Include Guard 唯一且与文件职责匹配。
- 直接 Include 自己明确依赖的稳定叶子模块；面向完整光照时使用聚合入口。
- 注释说明非显然的 Include 顺序约束。

### 禁止

- 直接 Include 标记为“不可直接使用”的内部公共文件。
- 新代码 Include Deprecated 兼容层。
- 依赖另一个无关 Include 偶然传递进来的类型或宏。
- 为解决重定义而无条件 `#undef` 管线宏。
- 复制管线核心 Include 到项目目录后长期分叉，却不记录上游来源和同步策略。

---

## 4. Material Property 与 SRP Batcher

### 4.1 Material CBUFFER

- 每材质标量和向量放入统一 Material Constant Buffer。
- 所有使用该 Shader 的 Pass 保持相同字段、顺序和类型。
- 不在不同 Pass 中根据 Keyword 改变 Material CBUFFER 布局。
- 纹理和 Sampler 按目标项目宏声明，不放入 Constant Buffer。
- C#、ShaderLab Property 和 HLSL 字段名称、类型、默认值保持一致。

### 4.2 每 Draw 与全局数据

- 不修改引擎每 Draw Buffer 布局来存放项目材质属性。
- 全局数据只用于真正跨材质共享且具有明确更新时机的内容。
- 高频每 Draw 数据优先复用已有引擎协议或实例化属性。
- 新增 Structured Buffer 时记录 Stride、容量、索引和生命周期。

### 4.3 实例化

- Vertex 输入、输出和入口保持目标项目要求的 Instance/Stereo 宏。
- 不直接访问对象矩阵变量来绕过 Instancing 或 DOTS 覆盖。
- 验证普通 Renderer、GPU Instancing 和适用的 GPU-driven 路径。

---

## 5. 数据初始化规范

### 5.1 SurfaceData

提供单一表面初始化函数，并明确：

- Base Color 与 Alpha 的来源。
- Metallic/Specular 工作流。
- Smoothness 通道。
- Tangent Space Normal 默认值。
- Emission、Occlusion 和可选材质层默认值。
- Alpha Clip、Premultiply 或 Modulate 顺序。

新增字段时，所有构造点必须更新；未使用字段也应给出语义正确的默认值。

### 5.2 InputData

Fragment 阶段统一构建 InputData：

- Position、Normal、View Direction 的空间必须明确。
- 插值法线逐像素归一化。
- Normal Map 使用正确 TBN 和切线符号。
- Shadow Coord 根据目标项目要求由 Vertex 插值或 Fragment 计算。
- Baked GI、Shadow Mask、Fog 和 Screen UV 按 Keyword 条件初始化。
- Debug 字段仅在对应条件下填写，但启用时必须完整。

### 5.3 输出

- Opaque 与 Transparent Alpha 语义分开处理。
- Alpha Clip Helper 的返回值若参与 Alpha-to-Coverage，应传到最终输出。
- HDR 颜色不要在无协议要求时提前 Saturate。
- 半精度累加结果存在溢出风险时，采用目标项目已有保护策略。

---

## 6. 坐标空间与矩阵

### 必须

- 变量名使用空间后缀，例如 OS、WS、VS、CS、NDC、SS、TS。
- 使用管线 Transform Helper 和矩阵宏。
- 区分 Position、Direction 和 Normal 的变换。
- 非均匀缩放下使用正确法线变换。
- Motion Vector 区分当前、前帧、抖动和非抖动矩阵。

### 禁止

- 把 Clip Position 的 XY 直接当归一化 UV。
- 默认 View Space 或 Clip Space 手性和深度范围固定。
- 直接访问某个矩阵变量并假定 XR、Shadow Pass 和 VFX 下语义相同。
- 在 Fragment 内重复进行可安全放在 Vertex 的高成本变换，却没有精度或插值理由。

---

## 7. 屏幕空间资源

### 7.1 生产先于消费

采样 Camera Texture 前必须确认：

- Renderer 是否支持该资源。
- Pass 是否请求或触发资源生产。
- 资源在注入点前已写完。
- Camera Stack、Scene/Preview、Target Texture 和 XR 下是否有效。

### 7.2 采样

- 使用管线的 X 纹理声明和 Sample/Load 宏。
- UV 使用归一化屏幕 Helper，再按资源协议应用 Stereo/预旋转转换。
- Pixel Coordinate 与 Normalized UV 不得混用。
- Point、Linear、Clamp 和 Repeat Sampler 必须符合数据语义。
- Depth 使用正确类型、Load/Sample 路径和线性化函数。

### 7.3 自采样风险

读取当前正在写入的颜色或 Attachment 前，必须由 Renderer 选择 Copy、Input Attachment、Framebuffer Fetch 或其他合法路径。禁止只在 Shader 中声明同名纹理并假定反馈读取安全。

---

## 8. 光照与阴影

### 8.1 光循环

- 使用目标项目提供的 Light 获取和 Loop 抽象。
- 不假定附加光一定来自数组、Structured Buffer 或 Cluster。
- 逐光循环内避免重复计算材质不变量。
- Light Layer、Cookie、Shadow 和 AO 条件应与内置路径一致。
- 新增逐光分支前评估分支一致性、寄存器和 Variant 成本。

### 8.2 BRDF

- 在进入光循环前初始化 BRDFData。
- 保持能量分配、粗糙度下限和 Fresnel 语义一致。
- 新材质层必须同时定义直接光、间接光和混合方式。
- 不只修改 Forward 直接光而遗漏 GI、Deferred 或 Debug。

### 8.3 阴影

- 使用目标项目 Shadow Coord 和采样 Helper。
- Shadow Bias 在正确空间和正确 Pass 应用。
- 软阴影质量和过滤 Tap 由项目协议控制。
- 混合光照必须组合实时阴影、Shadow Mask 和 Fade，而不是简单相乘。
- Point Light Face/Slice、级联索引和超出 Frustum 行为必须保持合法。

---

## 9. Forward、Deferred 与多 Pass 一致性

材质功能变更必须建立 Pass 矩阵：

| 功能 | Forward | GBuffer | Deferred Read | DepthNormals | ShadowCaster | Meta | Motion | Debug |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Property/纹理语义 | 必查 | 必查 | 视情况 | 视情况 | Alpha 相关 | 必查 | Alpha 相关 | 必查 |
| Normal | 必查 | 必查 | 必查 | 必查 | 通常 N/A | 视烘焙 | 视位移 | 必查 |
| Alpha Clip | 必查 | 必查 | N/A | 必查 | 必查 | 必查 | 必查 | 必查 |
| 新 BRDF 参数 | 必查 | 必查 | 必查 | 通常 N/A | N/A | 视语义 | N/A | 必查 |
| Emission/GI | 必查 | 必查 | 必查 | N/A | N/A | 必查 | N/A | 必查 |

若项目不支持某路径，明确标为 N/A，不要默认为已兼容。

---

## 10. GBuffer 与 DBuffer 协议

修改 Attachment 协议时必须同时更新：

1. C# Attachment 数量、索引、格式和创建条件。
2. Shader Fragment Output 的 Target 索引和类型。
3. Packing 的通道和量化方式。
4. Framebuffer Fetch/Input Attachment 声明。
5. 普通 Texture 回退声明和绑定槽。
6. Unpacking 与 Deferred Lighting。
7. Shader Format Hint 或等价编译信息。
8. Debug View、Decal、Rendering Layer 和 Shadow Mask 条件组合。

Material Flag 应使用稳定 Bit 定义，写端和读端完全一致。不得复用一个 Bit 表达互斥性未被证明的两个功能。

---

## 11. Keyword 与 Variant

### 11.1 新增前回答

- 谁设置 Keyword？
- 局部还是全局？
- Material、Camera、Renderer 还是 Pipeline 级？
- 运行时是否切换？
- 是否需要 Vertex/Fragment 两阶段？
- 与哪些 Keyword 互斥？
- Build Stripper 如何保留或删除？
- Variant 增量是多少？

### 11.2 选择原则

- 材质静态功能优先 Local Keyword。
- 管线功能必须与 Renderer 设置和剔除策略同步。
- 能由已有 Keyword 推导的状态不再增加 Keyword。
- 低成本、运行时频繁变化且分支一致的逻辑可考虑动态分支。
- 不用 Keyword 掩盖资源未绑定或 Pass 时序错误。

### 11.3 条件编译

- 条件范围尽量小。
- 为禁用路径提供语义正确默认值。
- 不允许某组合下结构体字段未赋值。
- 平台 Workaround 必须有原因、范围和移除条件。

---

## 12. 精度规范

| 数据 | 默认建议 | 原因 |
|---|---|---|
| Object/World Position | `float` | 大范围和矩阵运算误差 |
| Clip/NDC/Depth | `float` | 深度精度和投影稳定性 |
| Distance/Attenuation 中间量 | `float` | 近距离倒数和范围平滑 |
| Motion Vector | `float` | 小运动与历史误差敏感 |
| Normal/Direction | `half` 或 `real`，验证后 | 通常归一化范围，但累积误差需关注 |
| Base Color/材质参数 | `half` 或 `real` | 通常受控范围 |
| HDR 光照累加 | 按平台评估，必要时 `float` | 多光和高强度可能溢出 |
| UV | 普通材质可降精度；大图集/长距离用 `float` | 精度不足会抖动或接缝 |

所有降精度都需要图像和目标平台验证。

---

## 13. 性能规范

### 13.1 ALU 与寄存器

- 把逐材质/逐像素不变量移出逐光循环。
- 避免同时保留多个大型结构体副本。
- 使用已有打包和 Fast Math Helper，但不牺牲协议正确性。
- 分支、Unroll 和 Loop 属性按目标平台实测，不凭经验绝对化。

### 13.2 纹理与带宽

- 合并通道前明确色彩空间、压缩和精度。
- 不重复采样同一纹理和 UV；复用结果。
- 屏幕空间 Load 与 Sample 根据过滤需求选择。
- 新增 GBuffer、全屏纹理或高精度格式必须计算显存和带宽。

### 13.3 Variant 与构建

- 记录新增 Variant 理论组合和实际构建数量。
- 检查 Shader Warmup、首帧卡顿和包体影响。
- 对互斥功能使用正确声明，避免笛卡尔积。

### 13.4 顶点与片元分工

- 可线性插值且精度允许的计算优先 Vertex。
- 法线、视线和非线性项在 Fragment 按需要重建或归一化。
- 移动平台重点关注 Varying 数量；桌面平台同样关注寄存器和带宽。

---

## 14. 命名、注释与代码风格

- 类型和公共函数使用清晰语义名；局部变量简洁但不丢失空间/单位。
- 坐标变量带空间后缀。
- `Initialize*` 负责完整初始化；`Sample*` 负责资源读取；`Evaluate*`/`Lighting*` 负责计算；`Pack*`/`Unpack*` 成对出现。
- 魔法通道、Bit、Slice、单位和范围使用命名常量或注释。
- 注释解释“为什么”和协议约束，不逐行复述代码。
- Workaround 注明平台条件、症状和验证依据。
- 保持与邻近管线源码风格一致，避免无关格式化。

---

## 15. 反模式

- 从其他项目复制完整 Lit Shader，仅修改颜色后宣称跨项目兼容。
- 直接读取内部全局变量，而不确认 C# 设置时机。
- 手写 Screen UV、Depth Linearization 或 Stereo Slice。
- 新增 Keyword 但不修改设置端和 Stripper。
- 只更新 Forward Pass，遗漏 GBuffer、ShadowCaster 或 Meta。
- 修改 GBuffer 通道却不更新 Deferred Reader 和 C# Format。
- 把所有变量改为 `half` 作为“移动优化”。
- 直接 Include Deprecated 文件或调用兼容重载。
- 为规避编译错误复制同名结构体/函数，造成双重协议。
- 未运行目标平台测试就声明 XR、移动端或 Deferred 支持。

---

## 16. 交付报告要求

Shader 修改交付时至少报告：

- 目标 Shader、Pass、Renderer 和 Rendering Mode。
- 使用的本地 Include 与关键符号证据。
- Material 到 Surface、Input、BRDF 和输出的数据流。
- C#/HLSL 协议变化。
- Keyword/Variant 变化。
- 精度、纹理、Varying、光循环和 Attachment 成本。
- Forward/Deferred/Shadow/Depth/Meta 等路径覆盖。
- XR、动态分辨率、MSAA、HDR 和平台验证范围。
- PASS、FAIL、N/A、BLOCKED 与剩余风险。
