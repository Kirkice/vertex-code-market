﻿# Vertex Code Market

> **A production-grade skill and knowledge layer for [Vertex-Code](https://github.com/Kirkice/Vertex-Code).**
>
> Turn Vertex-Code from a capable coding agent into a graphics-aware engineering partner for Unity rendering, shader authoring, RenderDoc investigation, pipeline design, and performance optimization.

<div align="center">

[![Companion Repository](https://img.shields.io/badge/companion-Vertex--Code-7c3aed?style=for-the-badge)](https://github.com/Kirkice/Vertex-Code)
[![Graphics](https://img.shields.io/badge/domain-Graphics-0ea5e9?style=for-the-badge)](#graphics-engineering)
[![Unity](https://img.shields.io/badge/Unity-Tooling-222c37?style=for-the-badge&logo=unity)](#unity-engineering)
[![RenderDoc](https://img.shields.io/badge/RenderDoc-Analysis-334155?style=for-the-badge)](#renderdoc-analysis)

</div>

## What this repository is

Vertex-Code Market is the domain extension layer for [Vertex-Code](https://github.com/Kirkice/Vertex-Code). It packages specialized **Skills**, durable **Knowledge**, and marketplace metadata so Vertex-Code can reason about graphics tasks with a repeatable engineering workflow instead of relying on generic code generation.

This repository provides:

- **Task routing** — identify whether a request belongs to shader authoring, rendering-pipeline design, debugging, optimization, Unity integration, or RenderDoc analysis.
- **Evidence-driven analysis** — separate project facts, tool output, inferred conclusions, blocked checks, and validated results.
- **Unity-aware workflows** — detect Built-in Render Pipeline, URP, and HDRP from project evidence before selecting APIs or generating code.
- **Cross-version URP guidance** — route between `Execute` and RenderGraph paths using the actual Unity package, Renderer, and source-code evidence.
- **RenderDoc investigation workflows** — inspect captures incrementally, preserve replay state, trace resources, review shaders, and map GPU evidence back to project code.
- **Engineering-quality delivery** — require explicit validation status, compatibility boundaries, performance evidence, and remaining risks.

## How it fits with Vertex-Code

The architecture is intentionally split into two repositories:

```text
┌──────────────────────────────────────────────────────────────┐
│ Vertex-Code                                                   │
│ Agent runtime · modes · orchestration · tool execution        │
└───────────────────────────────┬──────────────────────────────┘
                                │ loads and routes
┌───────────────────────────────▼──────────────────────────────┐
│ Vertex Code Market                                            │
│ Skills · knowledge · references · marketplace registration    │
└───────────────────────────────┬──────────────────────────────┘
                                │ operates on
┌───────────────────────────────▼──────────────────────────────┐
│ Your workspace                                                 │
│ Unity project · shaders · captures · build and profiling data │
└──────────────────────────────────────────────────────────────┘
```

Use [Vertex-Code](https://github.com/Kirkice/Vertex-Code) as the agent platform, and use this repository as its graphics and Unity engineering capability pack.

## Capability map

### Graphics engineering

The Graphics Base layer provides a clear separation of concerns:

| Capability | Responsibility | Typical output |
|---|---|---|
| [`unity-graphics`](skills/graphics-base/unity-graphics/SKILL.md) | Unity graphics entry point and render-stack routing | Pipeline identification, constraints, downstream Skill selection |
| [`write-shader`](skills/graphics-base/write-shader/SKILL.md) | HLSL, GLSL, WGSL, MSL, ShaderLab, URP ShaderLibrary, and shader migration | Shader implementation, review, integration contract, variant and precision analysis |
| [`rendering-pipeline`](skills/graphics-base/rendering-pipeline/SKILL.md) | Render Pass, Renderer Feature, RenderGraph, attachments, barriers, and resource lifetime | Pipeline design, pass integration, resource read/write contract |
| [`graphics-debug`](skills/graphics-base/graphics-debug/SKILL.md) | Black screens, incorrect lighting, shadows, colors, post-processing, and frame correctness | Root-cause isolation, evidence chain, targeted fix |
| [`graphics-optimization`](skills/graphics-base/graphics-optimization/SKILL.md) | GPU/CPU cost, overdraw, bandwidth, memory, draw calls, and shader variants | Bottleneck evidence, optimization trade-offs, before/after validation |
| [`shader-to-desmos`](skills/graphics-base/shader-to-desmos/SKILL.md) | Convert shader mathematics into interactive plots | Desmos-compatible equations and visual mathematical inspection |

The shared routing rules live in [`graphics-skill-routing-protocol.md`](knowledge/graphics/graphics-skill-routing-protocol.md). This prevents every Skill from inventing its own classification and handoff behavior.

### Unity engineering

Unity-specific workflows complement the graphics Skills:

| Capability | Responsibility |
|---|---|
| [`unity-cli`](skills/unity-tooling/unity-cli/SKILL.md) | Unity Editor discovery, project operations, builds, tests, logs, and diagnostics |
| [`unity-package-management`](skills/unity-tooling/unity-package-management/SKILL.md) | Package discovery, version inspection, dependency analysis, and package changes |
| [`urp-detection-version-routing.md`](knowledge/graphics/urp-detection-version-routing.md) | Project-fact-first URP detection, version routing, Renderer inspection, and Execute/RenderGraph selection |
| [`urp-shader-validation-checklist.md`](knowledge/graphics/urp-shader-validation-checklist.md) | Structured acceptance checks for URP ShaderLab, HLSL, bindings, passes, variants, platforms, and runtime behavior |

### RenderDoc analysis

The RenderDoc suite is organized around focused investigations rather than one oversized diagnostic Skill:

| Capability | Responsibility |
|---|---|
| [`renderdoc-frame-overview`](skills/renderdoc-for-vscode/renderdoc-frame-overview/SKILL.md) | Establish a reliable frame and event map |
| [`renderdoc-current-selection-explainer`](skills/renderdoc-for-vscode/renderdoc-current-selection-explainer/SKILL.md) | Explain the current selection in grounded, user-friendly terms |
| [`renderdoc-buffer-inspector`](skills/renderdoc-for-vscode/renderdoc-buffer-inspector/SKILL.md) | Inspect structured and raw buffer data incrementally |
| [`renderdoc-texture-trace`](skills/renderdoc-for-vscode/renderdoc-texture-trace/SKILL.md) | Trace texture producers, consumers, formats, and event usage |
| [`renderdoc-shader-review`](skills/renderdoc-for-vscode/renderdoc-shader-review/SKILL.md) | Review shader behavior in pipeline and resource context |
| [`renderdoc-performance-investigation`](skills/renderdoc-for-vscode/renderdoc-performance-investigation/SKILL.md) | Start from timing evidence and drill into GPU hotspots |
| [`renderdoc-pass-to-project-mapping`](skills/renderdoc-for-vscode/renderdoc-pass-to-project-mapping/SKILL.md) | Connect capture evidence to the implementation in the workspace |
| [`renderdoc-replay-recovery-helper`](skills/renderdoc-for-vscode/renderdoc-replay-recovery-helper/SKILL.md) | Recover missing capture or replay state without guessing |
| [`renderdoc-app-launch-capture`](skills/renderdoc-for-vscode/renderdoc-app-launch-capture/SKILL.md) | Prepare application launch and capture workflows |

All RenderDoc investigation Skills share [`renderdoc-shared-analysis-protocol.md`](skills/renderdoc-for-vscode/renderdoc-shared-analysis-protocol.md), which defines capture preflight, evidence levels, replay recovery, and reporting conventions.

## Engineering principles

### Facts before assumptions

The agent should inspect the workspace, project configuration, package manifests, source code, captures, and tool output before producing implementation-specific conclusions. Examples, memory, and version-number assumptions never outrank project evidence.

### One primary route per task

Every request gets one primary execution Skill. Related Skills are explicit handoff targets, not competing workflows running simultaneously. Correctness issues are isolated before performance work begins.

### Capture facts are not source-code facts

RenderDoc observations and project implementation candidates are reported separately. A resource is not called a shadow map, G-buffer, post-process target, or material texture without producer/consumer or binding evidence.

### Validation is part of delivery

A completed answer distinguishes:

- **Confirmed** — directly verified from source, configuration, tool output, or reproducible behavior.
- **Inferred** — a reasoned interpretation supported by evidence.
- **Changed** — files, interfaces, resources, or behavior modified.
- **Validated** — checks actually executed, such as compilation, tests, Frame Debugger, Profiler, or GPU Capture.
- **Blocked** — checks that could not be performed because required evidence or environment was unavailable.
- **Follow-up** — the smallest remaining verification step.

## Shared protocols and knowledge

Shared methodology is kept in Knowledge rather than duplicated across every Skill:

- [`graphics-skill-routing-protocol.md`](knowledge/graphics/graphics-skill-routing-protocol.md) — task classification, Skill ownership, handoff boundaries, and delivery states.
- [`urp-detection-version-routing.md`](knowledge/graphics/urp-detection-version-routing.md) — Unity render-stack detection and URP API routing.
- [`renderdoc-shared-analysis-protocol.md`](skills/renderdoc-for-vscode/renderdoc-shared-analysis-protocol.md) — capture/replay preflight, evidence discipline, and RenderDoc reporting.
- [`urp-shader-validation-checklist.md`](knowledge/graphics/urp-shader-validation-checklist.md) — shader-specific acceptance criteria.
- [`knowledge/index.json`](knowledge/index.json) — searchable knowledge metadata used by the marketplace integration.

## Repository structure

```text
skills/
├─ graphics-base/
│  ├─ unity-graphics/
│  ├─ write-shader/
│  ├─ rendering-pipeline/
│  ├─ graphics-debug/
│  ├─ graphics-optimization/
│  └─ shader-to-desmos/
├─ renderdoc-for-vscode/
│  ├─ renderdoc-*/
│  └─ renderdoc-shared-analysis-protocol.md
└─ unity-tooling/
   ├─ unity-cli/
   └─ unity-package-management/
knowledge/
├─ graphics/
├─ general/
└─ index.json
marketplace.yml
```

Each Skill is self-contained with a [`SKILL.md`](skills/graphics-base/unity-graphics/SKILL.md). Optional references live beside their Skill, and agent-specific metadata is stored in [`agents/`](skills/renderdoc-for-vscode/renderdoc-frame-overview/agents/).

## Installation and integration

This repository is published through [`marketplace.yml`](marketplace.yml). In a Vertex-Code setup, register or point the marketplace configuration at this repository, then enable the Skills and Knowledge entries required by your workspace.

Recommended first activation set:

1. [`unity-graphics`](skills/graphics-base/unity-graphics/SKILL.md) for Unity graphics routing.
2. [`write-shader`](skills/graphics-base/write-shader/SKILL.md) for shader implementation and review.
3. [`rendering-pipeline`](skills/graphics-base/rendering-pipeline/SKILL.md) for pass and resource architecture.
4. [`graphics-debug`](skills/graphics-base/graphics-debug/SKILL.md) for correctness investigations.
5. [`graphics-optimization`](skills/graphics-base/graphics-optimization/SKILL.md) for measured performance work.
6. The RenderDoc Skills under [`skills/renderdoc-for-vscode/`](skills/renderdoc-for-vscode/).

The exact installation command is controlled by the [Vertex-Code](https://github.com/Kirkice/Vertex-Code) marketplace/runtime workflow; this repository intentionally keeps runtime orchestration in the companion project rather than duplicating it here.

## Typical workflows

### Build or migrate a URP shader

1. Detect Unity and URP from project files.
2. Record the shared URP fact card.
3. Inspect the target Renderer, Pass path, Include graph, material contract, and C# bindings.
4. Implement the smallest compatible shader change.
5. Validate relevant passes, keywords, platforms, and runtime behavior.
6. Report confirmed facts, changes, validation, blocked checks, and residual risk.

Start with [`write-shader/SKILL.md`](skills/graphics-base/write-shader/SKILL.md) and [`urp-detection-version-routing.md`](knowledge/graphics/urp-detection-version-routing.md).

### Investigate a RenderDoc capture

1. Resolve the capture and replay state.
2. Resolve the current selection or target event.
3. Inspect only the resource, pipeline, shader, timing, or buffer context needed for the question.
4. Separate capture facts from source-code mapping candidates.
5. State what is confirmed, inferred, blocked, and worth inspecting next.

Start with [`renderdoc-frame-overview/SKILL.md`](skills/renderdoc-for-vscode/renderdoc-frame-overview/SKILL.md) and [`renderdoc-shared-analysis-protocol.md`](skills/renderdoc-for-vscode/renderdoc-shared-analysis-protocol.md).

### Diagnose a rendering bug before optimizing

1. Classify the visible failure.
2. Isolate the object, pass, keyword, input resource, and pipeline state.
3. Confirm correctness with a reproducible check.
4. Only then hand off to [`graphics-optimization/SKILL.md`](skills/graphics-base/graphics-optimization/SKILL.md) if measured cost remains.

## Repository status and scope

This repository contains capability definitions, methodology, references, and marketplace metadata. It does not replace the Vertex-Code runtime, a Unity project, or RenderDoc itself. Runtime validation depends on the target project, Unity version, graphics API, platform, capture availability, and installed tools.

Unity tooling skills adapted from Unity Technologies are kept under [`skills/unity-tooling/`](skills/unity-tooling/). Their attribution, license, and upstream records live beside the adapted skills:

- [`NOTICE.md`](skills/unity-tooling/NOTICE.md) — third-party attribution and scope boundary.
- [`UNITY-COMPANION-LICENSE.md`](skills/unity-tooling/UNITY-COMPANION-LICENSE.md) — applicable Unity Companion License notice.
- [`UPSTREAM.md`](skills/unity-tooling/UPSTREAM.md) — source repository, commit, imported skills, and provenance record.

## License and attribution

See the repository license and the Unity tooling attribution records under [`skills/unity-tooling/`](skills/unity-tooling/) before redistributing or modifying adapted Unity material.
