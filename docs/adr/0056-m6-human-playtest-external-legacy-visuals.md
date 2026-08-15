# ADR 0056: M6 Human Playtest External Legacy Visuals

## Status

Accepted for configured project-enhancement delivery.

## Decision

The M6 Unity bootstrap may prefer a read-only `YR1001_ProjectBaseline` source
when the local external-content configuration explicitly enables it. The
Unity-only `ExternalLegacyVisualProvider` reuses the existing bounded MIX,
SHP(TS), PAL, VXL/HVA, and managed RawLzo1X contracts. It probes only a fixed,
bounded candidate-name set, keeps source status separate from Simulation state,
and exposes no payload, decoded bytes, pixels, filenames, or absolute paths.

If the source is absent, unavailable, or does not yield a safe indexed-frame /
palette pair, the bootstrap uses an explicit synthetic Sprite fallback. Terrain
continues to use the existing synthetic isometric chunk because TMP/theater
binding and map-specific terrain semantics are outside this package.

## Consequences

Local interactive launches can show configured legacy indexed visuals without
embedding original assets in the repository. Automated headless scene smoke
tests start paused so asynchronous scene integration cannot advance the bounded
synthetic simulation before assertions drive it. No new LZO algorithm or writer
is introduced, no map packed section is loaded, and no original-runtime or
visual-parity claim is made.
