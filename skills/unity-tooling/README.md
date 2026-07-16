# Unity Tooling Skills

This group contains Unity project-operation skills. It is intentionally separate from `skills/graphics-base/`:

Both child directories are first-class marketplace skills. Their marketplace source paths are `skills/unity-tooling/unity-cli` and `skills/unity-tooling/unity-package-management`; `third-party/unity-technologies-skills/` only stores attribution and upstream licensing records.

- `unity-cli`: Editor, project, build, test, log, and environment operations.
- `unity-package-management`: UPM package discovery and package changes through Unity's Package Manager API.

Use these skills to establish project facts and run validation; use the graphics skills to reason about URP, shaders, RenderGraph, and GPU behavior.

## Routing

```text
Unity Editor / build / test / logs -> unity-cli
UPM package / version / dependency -> unity-package-management
Built-in / URP / HDRP identification -> ../graphics-base/unity-graphics
RendererFeature / Pass / RenderGraph -> ../graphics-base/rendering-pipeline
ShaderLab / HLSL -> ../graphics-base/write-shader
GPU correctness / performance -> ../graphics-base/graphics-debug or graphics-optimization
```

