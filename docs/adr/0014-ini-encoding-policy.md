# ADR 0014: Explicit INI encoding policy

- Status: Accepted
- Date: 2026-08-03

## Context

INI punctuation is ASCII, but names, values, and comments may contain bytes
whose code-page meaning depends on the game distribution and locale. Host
defaults such as `Encoding.Default`, the current Windows ACP, current culture,
or permissive decoder fallback would make the same content parse differently
on another machine.

The four controlled WP-02F ProjectBaseline samples are BOM-less and entirely
7-bit ASCII. Their bytes cannot distinguish ASCII, UTF-8, or a legacy
ASCII-compatible code page. The inspected FinalAlert 2 and OpenRA sources do
not establish a portable YR runtime code-page rule.

## Decision

- Raw bytes remain authoritative for every encoding.
- Structural punctuation is recognized only as exact ASCII bytes or exact
  UTF-16 code units selected by an explicit BOM.
- A BOM-less input is `RawSingleByte`. No Unicode or system-code-page identity
  is inferred. Non-ASCII fields remain raw until a caller selects a policy.
- UTF-8, UTF-16LE, and UTF-16BE BOMs are preserved and select strict physical
  validation. UTF-16 payloads must contain complete two-byte code units.
- Strict Unicode decoders use exception fallback. They never insert a
  replacement character, normalize Unicode, trim, or rewrite newlines.
- `IniTextEncodingPolicy` creates an optional decoded view. It does not alter
  the stored bytes, node slices, offsets, or canonical model hash.
- Raw-ASCII queries support explicit ordinal comparison only in WP-02F.
  Culture-sensitive and game-runtime case rules are not provided.
- NUL is a hard error. Other unsupported controls are preserved only through
  an opaque line when physical safety can be proven.
- A BOM/length contradiction or malformed declared encoding is a failed parse,
  not an opaque line.

## Consequences

The parser is deterministic across host locale and platform. BOM-less legacy
text remains usable for structure and identity output even when its eventual
display code page is unknown. A caller cannot obtain text accidentally; it
must select a strict policy and handle a decoding failure.

This decision does not claim that YR accepts BOM-marked Unicode files. BOM
support is a bounded preservation and validation feature for explicit input,
not an original-runtime compatibility result. Runtime language selection,
locale code pages, CSF lookup, fonts, and UI text remain unimplemented.

## Alternatives

- Decode BOM-less input as UTF-8: rejected because the controlled ASCII bytes
  provide no evidence for that identity.
- Use the Windows ACP or `Encoding.Default`: rejected because results vary by
  machine and platform.
- Decode with replacement fallback: rejected because malformed input would be
  changed silently and could not round-trip as the same semantic text.
- Store decoded strings as authority: rejected because byte offsets, unknown
  code pages, and exact identity output would be lost.
- Reject every non-ASCII BOM-less byte: rejected because legacy single-byte
  content must remain losslessly preservable pending code-page research.
