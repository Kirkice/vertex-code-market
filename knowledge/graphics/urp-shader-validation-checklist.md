# URP Shader 验收清单

## 1. 使用规则

本清单适用于新增、修改、迁移或审查 URP Shader、HLSL Include、材质模型及其 C# 绑定协议。

状态只能使用：

| 状态 | 含义 |
|---|---|
| PASS | 已执行，并有证据证明通过 |
| FAIL | 已执行但不满足要求；完成前必须修复 |
| N/A | 经分析确认不适用，并记录原因 |
| BLOCKED | 应执行但缺少环境、资产、平台或信息 |
| REVIEW | 静态审查完成，但仍需要运行或平台验证 |

禁止把“未发现问题”“理论上支持”或“未执行”记录为 PASS。每个 BLOCKED 项都必须进入交付风险；任何强制项为 FAIL 时不得声明完成。

建议记录格式：

```text
检查项：
状态：PASS / FAIL / N/A / BLOCKED / REVIEW
证据：文件、符号、编译日志、截图、Capture 或测试场景
说明：适用范围、异常与剩余风险
```

---

## 2. 事实与范围门禁

- [ ] 已完成 Shader 事实卡，目标 Shader、SubShader、Pass 和材质模型明确。
- [ ] 已确认实际 Renderer、Rendering Mode、注入阶段与目标平台。
- [ ] 已从目标项目锁定包源码确认入口 Include、宏、结构体和函数签名。
- [ ] 已找到最邻近的可工作 Shader 或内置 Pass 作为项目证据。
- [ ] 已列出 C# 设置端、Renderer 资源生产端和 Shader 消费端。
- [ ] 已声明 Forward、Deferred、Renderer2D、自定义 Renderer、XR 等支持或不支持边界。
- [ ] 未把其他项目或其他包代际的实现当作当前项目事实。
- [ ] 所有未知关键协议已标记 BLOCKED，未用猜测补全代码。

证据至少包含目标文件路径、关键符号及其调用或 Include 关系。

---

## 3. Include 与符号检查

- [ ] Include 路径在目标项目中真实存在，大小写与包路径正确。
- [ ] 使用的是面向目标职责的入口，而不是 Deprecated 或禁止直接引用的内部文件。
- [ ] Include 依赖方向符合 Core/Input → Surface/BRDF → Lighting/Pass。
- [ ] 不依赖无关 Include 偶然传递的类型、宏、纹理或 Sampler。
- [ ] Include Guard 唯一，未与管线或项目其他文件冲突。
- [ ] 无重复结构体、全局变量、函数或管线宏声明。
- [ ] 无为了压制错误而无依据地 `#undef` 管线宏。
- [ ] 条件编译的启用与禁用路径都拥有完整声明和合法默认值。

---

## 4. ShaderLab、Pass 与编译

### 4.1 静态结构

- [ ] ShaderLab Property 与 HLSL Material 字段名称、类型和默认值一致。
- [ ] SubShader Tags、Pass Tags、LightMode 与 Renderer 的筛选协议一致。
- [ ] Blend、ZWrite、ZTest、Cull、ColorMask、Stencil 和 Conservative 等状态符合目标 Pass。
- [ ] Shader Target、Renderer 限制和平台排除有明确依据。
- [ ] Vertex/Fragment 入口、Attributes、Varyings 和语义匹配。
- [ ] Instance、Stereo、DOTS 或适用的 GPU-driven 宏位于正确位置。

### 4.2 编译

- [ ] 编辑器导入无 Shader Error 和 HLSL Warning。
- [ ] 目标图形 API 的 Shader 编译通过。
- [ ] 每个适用 Pass 均已编译，而非只验证默认 Forward Pass。
- [ ] 关键 Keyword 组合编译通过。
- [ ] 禁用功能的最小 Variant 编译通过。
- [ ] 构建或等价剔除流程后，运行时所需 Variant 仍然存在。
- [ ] Fallback、Error Shader 和平台不支持行为符合设计。

---

## 5. Material CBUFFER、Buffer 与绑定协议

- [ ] 所有材质 Pass 使用相同 Material Constant Buffer 字段、顺序和类型。
- [ ] Material CBUFFER 布局不因 Keyword 改变。
- [ ] 纹理和 Sampler 未错误放入 Constant Buffer。
- [ ] 未修改引擎每 Draw Buffer 来承载项目材质属性。
- [ ] C# Property ID 与 HLSL 名称逐项一致。
- [ ] C# 上传类型与 HLSL 标量、向量、矩阵和数组类型一致。
- [ ] Structured/Raw/ByteAddress Buffer 的 Stride、对齐、容量和索引语义一致。
- [ ] Buffer 的创建、更新、绑定和释放时机明确。
- [ ] 全局属性不会在多相机、Camera Stack 或并行录制中残留错误状态。
- [ ] GPU Instancing、MaterialPropertyBlock 和普通材质路径结果一致或差异有设计依据。
- [ ] SRP Batcher 兼容状态已在目标项目中验证。

---

## 6. 数据初始化与材质语义

### 6.1 Surface 数据

- [ ] 表面结构体先完整零初始化或逐字段赋值。
- [ ] Albedo、Alpha、Metallic/Specular、Smoothness、Normal、Emission 和 Occlusion 来源明确。
- [ ] 未启用 Normal Map 时使用语义正确的切线空间默认法线。
- [ ] 未启用可选材质层时使用安全默认值。
- [ ] Alpha Clip、Premultiply、Modulate 和 Alpha-to-Coverage 的顺序正确。
- [ ] 颜色纹理与数据纹理的 sRGB/Linear 采样设置正确。

### 6.2 几何与管线输入

- [ ] Position、Normal、Tangent、View Direction 坐标空间明确。
- [ ] 插值法线在 Fragment 阶段按需要归一化。
- [ ] TBN、切线符号、双面法线和非均匀缩放处理正确。
- [ ] Shadow Coord 的 Vertex/Fragment 计算方式符合本地协议。
- [ ] Baked GI、Shadow Mask、Fog、Vertex Lighting 和 Screen UV 在适用 Variant 中完成初始化。
- [ ] Debug 所需字段在启用 Debug 时完整初始化。
- [ ] 新增结构体字段已同步所有构造、重载、Pack、Unpack 和 Debug 路径。

---

## 7. 坐标、深度与屏幕空间资源

- [ ] 使用管线变换 Helper 和矩阵宏，不直接绕过 Instancing/XR 覆盖。
- [ ] Object、World、View、Clip、NDC、Screen 和 Tangent Space 未混用。
- [ ] 屏幕 UV 通过目标项目 Helper 生成。
- [ ] UV 原点、Y Flip、预旋转、Viewport 和动态分辨率已处理。
- [ ] XR 纹理维度、Array Slice 和 Stereo Transform 使用管线抽象。
- [ ] Reversed Z、Clip Space 深度范围和线性化公式来自目标项目协议。
- [ ] Depth 的 Load/Sample、MSAA 和过滤方式符合资源类型。
- [ ] Camera Depth、Normals、Opaque Color、AO 或 Rendering Layers 在消费前确实由 Renderer 生产并绑定。
- [ ] 资源生产 Pass 在消费 Pass 之前完成。
- [ ] 未从当前写入的 Attachment 进行未经声明的反馈采样。
- [ ] Copy、Input Attachment、Framebuffer Fetch 或 Texture 回退路径与 Renderer 选择一致。
- [ ] Base/Overlay、Scene、Preview、Reflection、Target Texture 相机行为已验证或明确排除。

---

## 8. 光照、阴影与 GI

### 8.1 BRDF 与光循环

- [ ] BRDF 数据在进入光循环前完整初始化。
- [ ] Metallic/Specular 工作流、能量分配、粗糙度下限和 Fresnel 语义一致。
- [ ] 主光与附加光使用目标项目 Light 获取和 Loop 抽象。
- [ ] 不假定光列表固定来自数组、Structured Buffer 或 Cluster。
- [ ] 材质不变量没有在逐光循环内重复计算。
- [ ] Distance/Angle Attenuation、Cookie、Light Layer、AO 和 Shadow 条件正确。
- [ ] 顶点光照与逐像素附加光不会重复累计。
- [ ] 高光、Clear Coat 或自定义材质层同时定义了直接光和间接光行为。

### 8.2 阴影

- [ ] 主光与附加光 Shadow Keyword、资源和索引协议一致。
- [ ] Shadow Bias 在正确坐标空间和正确 Pass 应用。
- [ ] 级联选择、Fade、Frustum 外行为和无阴影回退正确。
- [ ] Point Light Face/Slice 与 Atlas 索引合法。
- [ ] 软阴影质量、过滤 Tap 和采样器由管线协议控制。
- [ ] 实时阴影、烘焙 Shadow Mask 与 Mixed Lighting 混合正确。
- [ ] ShadowCaster 的 Alpha Clip、Vertex Deformation 和 Cull 行为与可见 Pass 一致。

### 8.3 间接光

- [ ] Lightmap、Directional Lightmap、SH 或 Probe Volume 路径按项目 Keyword 正确选择。
- [ ] Dynamic/Static Lightmap UV 与变换正确。
- [ ] Reflection Probe 的 Box Projection、Rotation、Blending 与 Roughness Mip 处理正确。
- [ ] Occlusion 同时遵守直接和间接光语义。
- [ ] Meta Pass 的 Albedo、Emission、Alpha Clip 和材质参数与烘焙意图一致。

---

## 9. Forward、Deferred 与多 Pass 一致性

为每项修改记录 PASS、N/A 或 BLOCKED：

| 检查对象 | Forward | GBuffer Write | Deferred Read | Depth/Normals | ShadowCaster | Meta | Motion | Debug |
|---|---|---|---|---|---|---|---|---|
| Property 与纹理 |  |  |  |  |  |  |  |  |
| Alpha 与裁剪 |  |  |  |  |  |  |  |  |
| Normal 与位移 |  |  |  |  |  |  |  |  |
| BRDF 参数 |  |  |  |  |  |  |  |  |
| Emission 与 GI |  |  |  |  |  |  |  |  |
| Keyword 与实例化 |  |  |  |  |  |  |  |  |

强制检查：

- [ ] Forward 与 Deferred 的可见结果差异在设计容差内。
- [ ] DepthOnly/DepthNormals 与可见几何的 Alpha Clip 和 Vertex Deformation 一致。
- [ ] ShadowCaster 轮廓与可见几何一致。
- [ ] Motion Vector 使用当前/前帧和抖动/非抖动矩阵的本地协议。
- [ ] Debug View 能解释新增材质字段或明确标为 N/A。
- [ ] 不支持的 Pass 已通过 Tags、Fallback 或 Renderer 行为明确排除。

---

## 10. GBuffer、DBuffer 与 Attachment 协议

若任务不涉及 Attachment 协议，可整节标为 N/A 并说明原因；否则全部检查：

- [ ] C# Attachment 数量、索引、格式、MSAA 和创建条件正确。
- [ ] Fragment Output Target 索引、类型和平台 Format Hint 对应 C# 格式。
- [ ] 写端 Packing 与读端 Unpacking 的通道、范围、量化和颜色空间一致。
- [ ] Material Flag 的 Bit 定义在所有写端和读端一致。
- [ ] Framebuffer Fetch/Input Attachment 与普通 Texture 回退读取相同语义。
- [ ] Deferred Lighting、Decal/DBuffer、Rendering Layer、Shadow Mask 和 AO 组合正确。
- [ ] Attachment 增量已评估显存、Tile Store/Load 和带宽成本。
- [ ] 不支持 MRT 数量或格式的平台拥有合法回退或明确不支持。
- [ ] GPU Capture 中 Attachment 格式、写入值、Load/Store 和读取时序符合设计。

---

## 11. Keyword 与 Variant

- [ ] 每个新增或修改的 Keyword 都记录设置端和清理端。
- [ ] 已确认 Global/Local 作用域及 Material/Camera/Renderer/Pipeline 生命周期。
- [ ] 已确认编译阶段范围：Vertex、Fragment 或全部阶段。
- [ ] 互斥 Keyword 使用了正确的声明方式。
- [ ] 禁用路径拥有语义正确的默认值。
- [ ] Keyword 不用于掩盖未绑定资源或错误 Pass 时序。
- [ ] 已计算理论 Variant 增量并检查实际构建数量。
- [ ] Build Stripper、Shader Variant Collection 或 Warmup 策略已同步。
- [ ] 运行时切换不会访问已被剔除的 Variant。
- [ ] 平台 Workaround 的条件、原因和移除标准有注释与证据。

---

## 12. 精度、稳定性与画质

- [ ] 世界位置、矩阵、深度、距离和 Motion 使用足够精度。
- [ ] HDR 多光累加在目标平台无溢出、NaN、Inf 或明显 Banding。
- [ ] Normal、Direction、Roughness 和 Fresnel 在降精度后仍稳定。
- [ ] 大图集、长距离 UV 和高频法线无抖动或接缝。
- [ ] 除零、负数开方、非法对数和未归一化输入拥有保护。
- [ ] 极端材质参数、极近/极远距离和高强度灯光已测试。
- [ ] HDR 与 SDR 输出均未发生无依据的提前 Saturate 或颜色空间错误。
- [ ] MSAA、Alpha-to-Coverage、透明混合和后处理组合结果正确。

---

## 13. 平台与相机矩阵

按项目实际目标填写：

| 维度 | 配置 | 状态 | 证据/说明 |
|---|---|---|---|
| Renderer | Universal / 2D / Custom |  |  |
| Rendering Mode | Forward / Deferred / Other |  |  |
| Camera | Base / Overlay / Scene / Preview / Reflection |  |  |
| Output | Backbuffer / Target Texture |  |  |
| Resolution | Native / Dynamic / Scaled |  |  |
| MSAA | Off / Enabled |  |  |
| Color | SDR / HDR |  |  |
| XR | Multi Pass / Single Pass / Other |  |  |
| API | 项目目标图形 API |  |  |
| Quality | 目标质量等级 |  |  |

- [ ] 至少验证默认配置和一组关键边界配置。
- [ ] 未验证的平台、XR 模式或 Renderer 未声明为支持。
- [ ] 平台特定回退不会静默产生错误画面。

---

## 14. 性能与资源成本

### 14.1 Shader 成本

- [ ] 记录修改前后指令、纹理采样、寄存器、Varying 和占用率变化。
- [ ] 逐光循环新增成本与最大可见光数量共同评估。
- [ ] 无重复纹理采样或可安全复用却重复计算的结果。
- [ ] 动态分支的一致性与 Keyword Variant 成本经过比较。
- [ ] Vertex/Fragment 分工符合精度和 Varying 预算。

### 14.2 带宽与资源

- [ ] 新增纹理的尺寸、格式、Mip、MSAA、Array Slice 和生命周期明确。
- [ ] 新增全屏资源或 Attachment 的显存与帧带宽已估算。
- [ ] Load/Store、Resolve、Copy 和中间纹理等间接成本已检查。
- [ ] 不存在无消费者的 Renderer 输入请求或全局纹理绑定。

### 14.3 运行验证

- [ ] 使用 Profiler、Frame Debugger 或 GPU Capture 验证实际 Pass 拓扑。
- [ ] 目标硬件帧时间与瓶颈变化已记录。
- [ ] Shader 编译、Warmup、首帧卡顿和包体变化已评估。

无法获得目标硬件时，相关项标为 BLOCKED 或 REVIEW，不得标为 PASS。

---

## 15. 回归、禁用与故障行为

- [ ] 功能启用和禁用均通过，禁用时无残留 Keyword、Global 或资源。
- [ ] 缺少可选纹理、Buffer、Lightmap、Shadow 或 Probe 时回退正确。
- [ ] Material 默认值、旧材质和序列化升级行为正确。
- [ ] Domain Reload、Scene 切换、Renderer 切换和质量等级切换无错误状态。
- [ ] Shader 重新导入和构建后结果稳定。
- [ ] 无粉色材质、黑屏、闪烁、NaN 扩散或平台专属编译错误。
- [ ] 受影响的现有材质、Renderer Feature 和后处理路径已做回归。

---

## 16. 交付门禁

只有满足以下条件才能声明完成：

- [ ] 所有事实门禁均为 PASS，或非关键未知项已明确 BLOCKED。
- [ ] 所有适用编译与协议项为 PASS。
- [ ] 没有遗留 FAIL。
- [ ] 所有 N/A 都有具体原因，而不是为了跳过测试。
- [ ] BLOCKED、REVIEW、未验证平台和兼容性边界已进入最终报告。
- [ ] 修改文件、关键符号、C#/HLSL 协议、Variant 与性能变化已列出。
- [ ] 已记录使用的工具、场景、配置和可复现证据。
- [ ] 未使用 Deprecated 符号作为新实现入口。
- [ ] 未把目标项目特例提升为跨项目通用事实。

最终摘要模板：

```text
目标 Shader / Pass：
Renderer / Rendering Mode：
修改范围：
本地源码证据：
C#/HLSL 协议变化：
Variant 变化：
性能与资源变化：
PASS：
FAIL：
N/A：
BLOCKED：
REVIEW：
未支持或未验证范围：
剩余风险：
完成判定：通过 / 不通过
```
