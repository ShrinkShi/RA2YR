# ADR 0056: M6 Human Playtest External Legacy Visuals

## Status

Accepted for configured project-enhancement delivery.

## Decision

The M6 Unity bootstrap may prefer a read-only `YR1001_ProjectBaseline` source
when the local external-content configuration explicitly enables it. The
Unity-only `ExternalLegacyVisualProvider` reuses the existing bounded MIX,
SHP(TS), PAL, VXL/HVA, and managed RawLzo1X contracts. Rules registry type ids
are resolved through typed Art records and the bounded Content/MIX VFS; no
physical visual filename catalog is embedded in the provider. Each stable role
has an independent asset/cache identity, and SHP buildings are never rendered
through a shared VXL mesh. Source status remains separate from Simulation state,
and no payload, decoded bytes, pixels, filenames, or absolute paths are exposed.

If the source is absent, unavailable, or does not yield a safe indexed-frame /
palette pair, the bootstrap uses an explicit synthetic Sprite fallback. Terrain
continues to use the existing synthetic isometric chunk because TMP/theater
binding and map-specific terrain semantics are outside this package.

## Consequences

Local interactive launches can show configured legacy indexed visuals without
embedding original assets in the repository. Palette handling defaults to the
explicit `SourcePaletteOnly` profile; implementation-specific remap offsets are
opt-in and are not a YR-authenticity claim. Automated headless scene smoke tests
start paused so asynchronous scene integration cannot advance the bounded
synthetic simulation before assertions drive it. M6-C5 reuses the M3-C4 managed
RawLzo1X backend and introduces no additional codec or writer, no map packed
section is loaded, and no original-runtime or visual-parity claim is made.
