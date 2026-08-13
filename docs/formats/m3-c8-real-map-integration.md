# M3-C8 real-map integration

M3-C8 runs the existing bounded ProjectBaseline audits as one read-only
vertical integration observation. It reuses the C1-C7 readers and managed
RawLzo1X backend; it does not add a second codec, map writer, renderer, or
runtime map loader.

The sanitized aggregate separates packed-stage observations from terrain
binding. A candidate IsoMapPack5/PreviewPack observation is not counted as a
fully bound terrain cell. Missing map-shell selection, theater/TMP candidates,
or C7 map-driven candidates remain unresolved and produce
`CompleteWithFailures` rather than a false success.

The configured ProjectBaseline is a patched development corpus. Source
fingerprints are compared before and after every child audit. The summary only
contains bounded counts, stable diagnostic totals, and one aggregate hash; it
does not publish names, paths, coordinates, bytes, pixels, or per-map hashes.
Original-runtime equivalence and clean YR 1.001 comparison remain unresolved.
