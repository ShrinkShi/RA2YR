# PreviewPack raw component foundation

This document records the M3-C5 Core boundary for PreviewPack. It is a
bounded, read-only foundation for synthetic/configured inputs. It is not a
claim that the project can load a complete map, render a preview, or match
the original runtime.

## Scope

The implemented path is:

```text
lossless Preview/PreviewPack occurrences
  -> explicit metadata selection and four-field Size parse
  -> M3-C1 packed fragment/Base64/chunk/RawLzo1X pipeline
  -> exact decoded byte stream
  -> raw component layout view
```

`PreviewMetadataReader` preserves all four signed `Size` fields. The current
configured interpretation uses fields 2 and 3 as positive width and height
candidates. Fields 0 and 1 remain raw and are not inferred as origin,
offset, or payload metadata. Duplicate sections and duplicate `Size`
occurrences require explicit selection; no first/last-wins rule is applied.

`PreviewPackSectionReader` requires a selected `PreviewPack` occurrence, an
explicit `RawLzo1X` packed policy, and an injected backend. It validates the
policy, backend, cancellation state, packed result, and checked decoded
length before creating a layout view. Upstream failure never invokes a
downstream metadata or layout interpretation.

## Raw and derived views

Decoded bytes are retained as an immutable defensive copy and are exposed
only through `GetBytesCopy()`. The optional layout view exposes raw component
triples and requires explicit profiles for channel order (`RGB`, `BGR`, or
`RawUnknown`) and row order (top-down or bottom-up). `RawUnknown` prevents
semantic pixel access. No palette, RGBA conversion, Texture2D, Sprite,
theater binding, TMP reference, or renderer is created.

The default length contract is exactly `width * height * 3`, with checked
arithmetic and a decoded-byte budget. Short and long streams fail closed;
bytes are never padded or silently truncated.

## Limits and provenance

Metadata sections, `Size` occurrences, fragments, decoded bytes, dimensions,
pixels, and diagnostics are bounded by `PreviewReadLimits`. Lazy occurrence
sources stop at the configured budget and null occurrences are fatal. Every
stage retains source context and INI provenance. Execution state is separate
from the diagnostic list, so a zero diagnostic budget cannot turn a failure
into success; suppressed diagnostic counts are retained.

## Compatibility state

- Preview metadata and raw component reader: Synthetic/configured.
- Channel order and row order: ExplicitProfileOnly.
- `Size` fields 0/1 meaning: Unresolved.
- Original runtime comparison: NotConfirmed.
- ProjectBaseline packed PreviewPack audit: `CompleteWithFailures` on the
  configured patched development source. The sanitized run observed 184
  candidate entries, 184 exact decoded streams, zero section failures, and
  one MIX mount-level failure. No payload, filename, path, pixel, or per-entry
  value was published.
- LZO: M3-C5 reuses the existing M3-C4 managed `RawLzo1X` backend; no new
  codec, writer, or recompressor is part of this work package.
- Palette, rendering, TMP, theater, gameplay, and M3-C6: NotImplemented.

The ProjectBaseline audit is read-only and aggregate-only. The source is a
patched development corpus, not a clean YR 1.001 installation, and the audit
does not establish original-runtime compatibility. The synthetic tests remain
independent of ProjectBaseline payload bytes.

The controlled entry point is
`Tools/Content/Invoke-PreviewPackProjectBaselineAudit.ps1`. It requires an
explicit external-content configuration whose only enabled source is
`YR1001_ProjectBaseline`; it refuses repository-local caches and writes a
sanitized JSON summary below `TestResults`.
