# Graphics Skill 路由协议

> 统一 Graphics Base 入口 Skill 的任务分类、转交边界和交付状态。各专项 Skill 只保留自己的领域规则。

## 1. 主问题分类

| 用户主要诉求 | 主执行 Skill | 典型产物 |
|---|---|---|
| Unity 图形任务但落点不清楚 | `unity-graphics` | 渲染栈识别、接入点和下游路由 |
| 编写、修改或迁移具体 Shader | `write-shader` | Shader/HLSL/ShaderLab 实现和验证 |
| 新增 Pass、Renderer Feature、RenderGraph 或资源生命周期 | `rendering-pipeline` | Pass 架构、资源读写和管线集成 |
| 黑屏、花屏、闪烁、阴影/颜色/后处理错误 | `graphics-debug` | 对象、Pass、Keyword、输入资源和根因定位 |
| 掉帧、GPU/CPU 时间高、带宽或内存压力 | `graphics-optimization` | 瓶颈证据、优化取舍和前后对比 |

## 2. 路由规则

1. 先检查工作区中的代码、配置、Shader、材质和工具输出，再询问缺失信息。
2. 一个任务只有一个主执行 Skill；其它 Skill 作为明确的二次转交对象。
3. 正确性问题优先于性能问题：先由 `graphics-debug` 定位，再交给 `write-shader` 或 `rendering-pipeline` 修复。
4. 性能问题先由 `graphics-optimization` 建立瓶颈证据，再交给 `write-shader` 或 `rendering-pipeline` 落地。
5. 若问题涉及 Pass 拓扑、资源生产/消费或生命周期，优先 `rendering-pipeline`，不要只在 Shader 中修补。
6. Unity 任务先识别 Built-in、URP 或 HDRP；URP 任务必须遵循 [`urp-detection-version-routing.md`](urp-detection-version-routing.md)。

## 3. 统一事实卡

执行前尽量输出：

```text
任务类型：Shader / Pipeline / Debug / Optimization / Unity routing
引擎与渲染栈：
Unity/URP 版本：
目标平台：
目标对象、Pass 或资源：
已确认事实：
待确认假设：
主执行 Skill：
二次转交：
```

## 4. 统一交付协议

最终报告分开写：

- **Confirmed**：由项目源码、工具输出或可复现结果确认的事实；
- **Inferred**：基于证据的推断，并说明依据；
- **Changed**：修改的文件、接口、资源协议和行为；
- **Validated**：执行过的编译、运行、Frame Debugger、Profiler、GPU Capture 或测试；
- **Blocked**：无法验证的环境、平台、捕获或资源；
- **Follow-up**：仍需执行的最小验证步骤。

不得把未执行的检查写成 PASS，也不得把示例 API 或其它项目的实现当作当前项目事实。
