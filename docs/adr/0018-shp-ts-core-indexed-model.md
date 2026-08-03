# ADR 0018: SHP(TS) Core exposes immutable local indexed frames

## Status

Accepted for M2-SHP1.

## Context

SHP(TS) stores indexed local frame rectangles and depends on external PAL and
runtime metadata for display. Treating the first Core implementation as a
Unity texture or a full canvas would mix file facts with palette, transparency,
pivot, shadow, remap, and Art behavior that have not been established.

The descriptor also contains fields whose semantics remain incomplete:
coordinate signedness, four frame-color bytes, reserved data, duplicate data
offsets, and flags 2. None is an established dependency reference.

## Decision

- `ShpTsDocument` preserves the 8-byte header and all ordered 24-byte
  descriptors as immutable raw values with logical provenance and absolute
  binary offsets.
- `ShpTsIndexedLocalFrame` contains only `width * height` row-major palette
  indices for the local rectangle. It does not allocate a canvas-sized buffer
  or use index 0 to imply transparency for flags 0.
- The reader and decoder are UnityEngine-free and use the bounded binary and
  seekable-window foundation. Memory, stream, short-read stream, and MIX window
  paths must be equivalent.
- High coordinate bits, non-zero reserved data, unaligned or repeated offsets,
  and unresolved flags remain diagnostics plus preserved raw values.
- No `ShpFrameDependency`, dependency resolver, depth budget, delta chain,
  palette binding, renderer object, pivot, or shadow pairing exists in this
  work package.
- Canonical directory and decoded-document SHA-256 values are domain-separated
  and exclude host paths and renderer state.

## Consequences

Core can safely index and compare SHP(TS) structure and produce local indexed
frames for later adapters without claiming visual or gameplay compatibility.
Later palette, remap, shadow, animation, and Unity layers must consume this
model explicitly rather than mutate it.

## Rejected alternatives

- Decode directly into `Texture2D`, `Color32`, or `Sprite`.
- Expand every frame to the global canvas in Core.
- Treat zero indices as universally transparent.
- Interpret `FrameColorRaw`, `ReservedRaw`, repeated offsets, or frame order as
  reference/delta metadata.
- Guess signed X/Y or a Unity pivot from the file header.
