# Map terrain composition foundation

M3-C7 composes existing raw results into an immutable, Unity-free candidate
document. It does not rescan the filesystem or infer semantics from visual
plausibility.

The composer requires explicit IsoMap tile-id and Overlay coordinate profiles.
It preserves IsoMap `LevelRaw`, TMP `HeightRaw`, `RampTypeRaw`, and
`TerrainTypeRaw` as separate fields. Unknown ramp and terrain values remain raw.
GlobalTileId lookup uses the explicit theater registry ranges. Missing registry,
TMP, Overlay, or optional Preview state is represented as an incomplete binding;
there is no silent winner, clamping, synthesis, or fallback to a different
profile.

The ProjectBaseline audit is map-driven and aggregate-only. The current patched
source traversal completed with no discovered map candidates, so no map binding
was fabricated. Original runtime compatibility remains `NotConfirmed`.

No palette conversion, LAT rendering, passability, gameplay, writer, or Unity
asset creation is included.
