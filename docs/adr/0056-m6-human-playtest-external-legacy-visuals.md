# ADR 0056: M6 Human Playtest External Legacy Visuals

## Status

Accepted for configured project-enhancement delivery.

## Decision

The M6 Unity bootstrap may prefer a read-only `YR1001_ProjectBaseline` source
when the local external-content configuration explicitly enables it. The
Unity-only `ExternalLegacyVisualProvider` reuses the existing bounded MIX,
SHP(TS), PAL, VXL/HVA, and managed RawLzo1X contracts. VXL presentation uses
an explicit raw-axis basis, bounds-center pivot, bounded normalization, and a
section hierarchy rather than a fixed scale constant. Rules registry type ids
are resolved through typed Art records and the bounded Content/MIX VFS; no
physical visual filename catalog is embedded in the provider. Each stable role
has an independent asset/cache identity, and SHP buildings are never rendered
through a shared VXL mesh. Source status remains separate from Simulation state,
and no payload, decoded bytes, pixels, filenames, or absolute paths are exposed.

Archive topology belongs to a bounded Core Content profile rather than the
Unity provider. Art visual identity is an explicit policy: `ExplicitOnly`
requires `Image=`, while `ExplicitOrSectionIdentifier` permits the resolved Art
section identity when `Image=` is absent. The latter is a configured project
policy based on a community convention, not original-runtime source proof.
`Voxel=yes` remains mandatory before the section identity can route to VXL.
HVA frame 0 is applied to each bound section, and source palette indices are
carried to mesh vertex colors. The PAL format model remains raw 6-bit channels
(0..63); `PaletteDisplayProfileConversion` delegates to the authoritative PAL
display conversion and is applied exactly once for both SHP texture colors and
VXL mesh colors. Human/enemy ownership is represented by a presentation marker
ring, not by replacing source palette colors.

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

The configured patched-development corpus currently resolves a sanitized
aggregate of two VXL/HVA battlefield roles. Both pass the bounded presentation
sanity gate (section-aware, HVA-applied, palette-colored, and normalized to a
maximum width of 0.85 cells and height of 0.372 cells). Six SHP structure roles
and one VXL/HVA role remain explicit fallback at existing strict reader
boundaries.
The VXL reader correction in this delivery only fixes inclusive-end
available-byte arithmetic for an exact three-byte command; it does not relax
span bounds, exact logical-Z consumption, or duplicate-count validation.
