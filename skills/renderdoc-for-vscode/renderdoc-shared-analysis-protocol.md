# RenderDoc Shared Analysis Protocol

> Shared workflow for RenderDoc inspection skills. Specialized skills own the target-specific branch; this document owns common state handling, evidence discipline, and reporting.

## 1. Capture and replay preflight

1. Resolve the capture before deeper inspection. Use `renderdoc_openCapture` when the active capture is unknown.
2. Resolve selection context with `renderdoc_getSelectionContext` when the user says “this”, “current”, or “selected”.
3. Check whether replay/native inspection is available before relying on pipeline, shader, mesh, buffer, or texture details.
4. If state is missing, use the recovery path rather than guessing. Report what is unavailable and what analysis remains possible.

## 2. Evidence ladder

Use the smallest sufficient tool sequence:

1. Capture/session state
2. Frame, event, selection, or resource identity
3. Target-specific detail
4. Supporting context only when it changes the conclusion
5. Project mapping or source inspection only when requested or materially useful

Prefer structured summaries and narrow resource reads. Do not dump all events, raw shader source, or full buffers unless requested.

## 3. Facts versus inference

Every conclusion should be classified as:

- **Confirmed**: directly returned by RenderDoc or verified in project files;
- **Inferred**: a likely interpretation supported by evidence;
- **Blocked**: unavailable because capture, replay, selection, source, or timing data is missing;
- **Next step**: the smallest follow-up inspection that can distinguish competing explanations.

Do not assume a resource is a shadow map, G-buffer, post-process target, or material texture without producer/consumer or binding evidence. Do not make performance claims without timing data.

## 4. Common output shape

```text
Target:
Capture/replay state:
Confirmed:
Likely interpretation:
Missing evidence:
Recommended next inspection:
```

For project mapping, keep capture facts separate from source-code candidates. For optimization, separate measured cost from hypotheses. For resource tracing, group events by producer, consumer, and pass role instead of listing every EID.

## 5. Specialist branches

- Frame overview: `renderdoc_getFrameSummary`, then timings only when cost matters.
- Current selection: selection context, then event/resource details according to selected type.
- Buffer inspection: resource detail, then a narrow `renderdoc_getBufferContents` slice.
- Texture trace: texture identity, producer/consumer search, then representative event context.
- Shader review: shader info first, pipeline state when bindings or render state matter, source only when needed.
- Performance investigation: frame summary, capture info, action timings, then drill into hot events.
- Pass-to-project mapping: event/frame context, shader anchors, then project implementation search.
- Replay recovery: restore enough capture/replay state and report remaining limitations.
- Application launch/capture: validate platform, target, device and injection readiness before launch.
