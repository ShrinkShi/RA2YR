# ADR 0012: CSF ordered raw document model

- Status: Accepted
- Date: 2026-08-03

## Context

A CSF file is an ordered sequence of labels, and each label declares an
ordered sequence of normal or extended values. Duplicate labels and multiple
values are structurally representable. Main text is stored as an inverted
UTF-16LE code-unit sequence, while label names and extended extra text use
separate byte-length-prefixed ASCII fields.

Several observable tools collapse this structure into a map, case-fold label
names, keep only one value, or use lossy Unicode decoding. Those choices erase
evidence needed to research original lookup behavior and cannot support a
strict compatibility engine.

## Decision

- `CsfDocument` retains an immutable header and immutable labels in file order.
- Each `CsfLabel` retains its original-case ASCII name and immutable values in
  file order. Duplicate labels are legal model entries and are not merged.
- Each `CsfValue` retains its normal/extended kind, an immutable sequence of
  decoded raw `UInt16` code units, and a separate optional ASCII extra field.
- Main text is decoded by bitwise-inverting each stored byte and explicitly
  combining little-endian pairs. No normalization, trimming, newline rewrite,
  current-code-page conversion, or replacement fallback is performed.
- Derived string views may be provided, but the raw code-unit sequence remains
  authoritative and cannot be overwritten by that view.
- Query APIs return all explicit matches in file order. They do not silently
  apply first-wins, last-wins, case-insensitive lookup, or language fallback.
- Header version, label count, total value count, reserved value, and raw
  language code remain visible. Unknown language numbers are representable.
- WP-02E accepts only version 3 and the exact YR markers `" FSC"`, `" LBL"`,
  `" RTS"`, and `"WRTS"`. It validates count equality, budgets, arithmetic,
  ASCII fields, and complete consumption. Any structural error rejects the
  whole result.
- Normalized model fingerprints are domain-separated and include header
  values, record kind, exact order, label bytes, raw code units, and extended
  bytes. Provenance and host paths are excluded.

## Consequences

The model is larger than a dictionary and runtime lookup requires an explicit
policy, but it retains all evidence needed for later behavior research,
diagnostics, deterministic hashing, and a future semantics-safe writer. A
surrogate pair remains two ordered code units; isolated surrogates are not
silently replaced because the current format evidence does not prove they are
structurally forbidden.

The parser can validate ProjectBaseline bytes without deciding how the game
chooses duplicate labels, languages, fonts, or UI presentation. Future runtime
localization policies can be added above Core without changing the parsed
document truth.

WP-02E remains read-only. Original comparison, round-trip writing, runtime
localization, language fallback, placeholder formatting, fonts, and UI
rendering remain unimplemented.

## Alternatives

- Store labels in a dictionary: rejected because it loses order and silently
  selects or overwrites duplicate labels.
- Store only decoded .NET strings: rejected because decoder fallback can lose
  arbitrary UTF-16 code units and obscure the exact on-disk semantics.
- Reject every isolated surrogate: rejected because the format stores raw
  16-bit units and the available evidence does not establish that validation.
- Accept unknown markers and continue scanning: rejected because record
  boundaries cannot be recovered safely without format-specific evidence.
- Implement runtime lookup while parsing: rejected because original case,
  duplicate, language, and fallback behavior has not been verified.
