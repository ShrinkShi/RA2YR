# Legacy visual import readiness (M6-C2)

M6-C2 adds provider-neutral Core contracts for PAL binding, indexed images,
SHP reuse, and raw VXL/HVA import. The readers preserve file-order bytes and
bounded provenance; they do not create Unity objects or choose renderer
semantics.

## PAL and indexed images

`WestwoodPaletteReader` remains the single PAL parser. `PaletteBindingDescriptor`
and `PaletteConversionProfile` carry an explicit logical palette identity and
conversion profile. `IndexedImageDescriptor` keeps an immutable index buffer,
palette binding, transparency candidate, and team-remap candidate separate.
The Core does not default to RGBA or silently choose a six-bit conversion.

## SHP

M6-C2 reuses the existing strict SHP(TS) reader and decoder. Supported flags
and unresolved row-width/flag boundaries are unchanged; an unsupported frame
must remain a diagnostic rather than a compatibility success.

## VXL raw reader

The reader accepts the 802-byte header, 28-byte section headers, bounded shared
body, 92-byte section tailers, sparse column directories, and span commands.
It preserves palette bytes, remap bytes, raw float bit patterns, dimensions,
raw color/normal indices, duplicate counts, and source order. Span ranges are
inclusive and strict: no clamping, padding, modulo normal lookup, or partial
voxel success. Memory, bounded Stream, and `ReadOnlyDataWindow` routes share
the same parser after a budget-checked snapshot.

The embedded palette is raw evidence. External theater/palette selection,
normal tables, axis conversion, transforms, lighting, mesh generation, and
Unity presentation are later explicit stages.

## HVA raw reader and binding

The HVA reader preserves the 24-byte header label, 16-byte section names, and
raw 3x4 Float32 bit records. Frame-major and section-major flattening are both
exposed as explicit candidates; `Unresolved` never guesses. `VxlHvaBinder`
matches unique exact names only, retains unbound sections, and fails closed on
duplicate-name ambiguity. It does not compose matrices or infer Art.ini roles.

## Compatibility boundary

This is synthetic/configured import readiness. It is not proof of original
runtime behavior, palette parity, VXL lighting parity, HVA ordering, SHP full
compatibility, or a playable renderer. No ProjectBaseline packed visual data
was read for this work package.
