# ADR 0013: Raw-byte INI document authority

- Status: Accepted
- Date: 2026-08-03

## Context

RA2/YR INI files contain ordered physical lines, comments, blank lines,
duplicate sections and keys, significant spelling and formatting, and MOD
extensions that may not be understood yet. A dictionary model cannot retain
these facts. It also cannot reproduce an unmodified file byte-for-byte or
distinguish a confidently parsed line from an ambiguously preserved line.

The inspected FinalAlert 2 and OpenRA parsers both discard formatting and
comments, and they disagree on duplicate winners and some malformed or
noncanonical forms. Reproducing either lossy map would erase the evidence
needed to research original behavior.

## Decision

- `IniRawDocument` owns an immutable copy of the complete bounded input.
- A BOM, if present, remains an explicit raw slice.
- Every physical line retains stable ID, absolute byte offset, content bytes,
  exact line-ending bytes, and its contiguous full raw slice.
- Exactly one ordered node references each physical line. Nodes are section,
  key/value, comment, blank, or opaque views; they never replace source bytes.
- Section and key/value nodes retain raw slices for names, values, equals-side
  whitespace, trailing bytes, and separator positions.
- Duplicate sections and keys remain separate ordered nodes. Query methods
  return every candidate under an explicit raw-ASCII ordinal comparison.
- No first-wins, last-wins, case-folding, cross-file precedence, or runtime
  default policy is defined at this layer.
- A safe but semantically unresolved line becomes `Opaque` and adds a
  structure diagnostic. A successful document explicitly reports whether it
  contains opaque lines.
- A structural or resource failure returns no document; callers cannot forge
  a successful writable result through the public API.
- Canonical model hashes are domain-separated and cover physical encoding,
  BOM, completeness, all original bytes, node kinds, slices, offsets, and
  order. Host paths are excluded.
- `IniIdentityWriter` may emit only an unmodified successful document. It
  copies retained raw bytes and verifies byte equality; it is not a semantic
  serializer.

## Consequences

The document uses more memory than a dictionary, but it preserves every byte
required for diagnostics, later semantic research, and exact identity output.
Unknown MOD constructs do not block lossless ingestion unless they violate a
hard safety boundary.

Runtime rule lookup and editable serialization must be separate layers with
explicit evidence. They cannot mutate or reinterpret the raw document truth
silently. The two `rulesmd.ini` candidates remain distinct documents until a
later work package establishes archive precedence.

WP-02F can claim only unmodified byte-identical round trips. It cannot claim
semantic edit safety, FinalAlert 2 edited interoperability, or Rules/Art/AI
compatibility.

## Alternatives

- Dictionary of sections and values: rejected because it loses order,
  duplicates, case, comments, whitespace, unknown lines, and line endings.
- Reconstruct text from parsed nodes: rejected because even an equivalent
  semantic view can change bytes and destroy unknown data.
- Reject every unknown line: rejected because physical preservation is safe
  and necessary for MOD and future-format research.
- Accept partial documents after hard failures: rejected because the identity
  writer could no longer prove complete output.
- Select a `rulesmd.ini` candidate here: rejected because that is runtime MIX
  precedence, outside the physical-document scope.
