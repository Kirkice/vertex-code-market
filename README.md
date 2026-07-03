﻿# Vertex Skills

Graphics skills packaged for marketplace-style installation.

## Repo Layout

```text
graphics/
  write-shader/
    SKILL.md
  rendering-pipeline/
    SKILL.md
  graphics-debug/
    SKILL.md
    references/
      debug-playbook.md
  graphics-optimization/
    SKILL.md
  unity-graphics/
    SKILL.md
renderdoc-for-vscode/
  renderdoc-buffer-inspector/
    SKILL.md
    agents/
      openai.yaml
  renderdoc-current-selection-explainer/
    SKILL.md
    agents/
      openai.yaml
  renderdoc-frame-overview/
    SKILL.md
    agents/
      openai.yaml
  renderdoc-pass-to-project-mapping/
    SKILL.md
    agents/
      openai.yaml
  renderdoc-performance-investigation/
    SKILL.md
    agents/
      openai.yaml
  renderdoc-replay-recovery-helper/
    SKILL.md
    agents/
      openai.yaml
  renderdoc-shader-review/
    SKILL.md
    agents/
      openai.yaml
  renderdoc-texture-trace/
    SKILL.md
    agents/
      openai.yaml
```

## Notes

- Each skill lives in its own folder and includes a `SKILL.md`.
- Extra references stay beside the skill under `references/`.
- Some skills also include agent-specific metadata under `agents/`.
- This repo is ready to be referenced by marketplace `skills.yml` entries using `sourcePath` values like `graphics/write-shader` or `renderdoc-for-vscode/renderdoc-frame-overview`.
