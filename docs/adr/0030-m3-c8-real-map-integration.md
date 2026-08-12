# ADR-0030: M3-C8 real-map integration remains aggregate and fail-closed

Status: Accepted for patched-development ProjectBaseline observation.

M3-C8 composes the already implemented bounded IsoMapPack5, PreviewPack,
TMP/theater, and MapTerrain audit services. The integration command preserves
each stage's counts and source-fingerprint checks, and reports unresolved
terrain binding separately from successful packed decoding. It never infers a
theater, coordinate profile, palette, renderer state, or original-runtime
meaning from aggregate success.

`CompleteWithFailures` is a valid truthful result when packed candidates are
observed but map-driven terrain binding is incomplete. The work package does
not add a writer, renderer, simulation, or new LZO algorithm.
