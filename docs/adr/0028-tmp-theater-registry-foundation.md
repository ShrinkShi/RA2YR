# ADR-0028: TMP and theater data remain explicit raw foundations

- Status: Accepted for synthetic/configured implementation
- Date: 2026-08-12

## Context

TMP cell bytes, theater control INI values, TileSet asset names, and later
terrain semantics are related but are not the same contract. The research
records a 52-byte cell header and multiple competing plane-layout strategies;
it also leaves channel, height, ramp, terrain, palette, and runtime role
semantics unresolved.

## Decision

Implement separate bounded Core components:

- `TmpRawReader` preserves the 16-byte file header, offset table, 52-byte
  cell headers, raw flags, raw candidate fields, and explicit plane windows.
- `TheaterControlReader` consumes an already-composed INI resolution and
  retains effective-value provenance.
- `TheaterTileRegistryBuilder` orders numeric TileSet sections and allocates
  checked cumulative GlobalTileId ranges independently of asset presence.
- `TmpAssetResolver` produces explicit variation and fallback candidates and
  fails closed on provider ambiguity.

All plane strategies and asset policies are explicit; no parser guesses a
profile or falls back after a failed interpretation. The read-only
ProjectBaseline audit publishes only sanitized aggregates and does not
confirm original-runtime compatibility.

## Consequences

The project can inspect raw TMP structure and deterministic registry ranges
without introducing palette conversion, rendering, terrain semantics,
passability, writers, or gameplay. The M3-C4 managed RawLzo1X backend is
reused by future packed adapters; this work package adds no codec or writer.
