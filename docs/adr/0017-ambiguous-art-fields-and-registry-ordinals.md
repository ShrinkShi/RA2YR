# ADR 0017: Ambiguous Art fields and duplicate registry ordinals remain unresolved

## Status

Accepted for the PR #10 review correction.

## Context

WP-02G2 accepts a complete G1 resolution and then applies an explicit typed
name policy. A G1 exact-name policy can preserve both `Image` and `image`, while
an ASCII case-insensitive G2 policy matches both. Selecting the first match
would invent a winner. Likewise, different original ordinal spellings such as
`0` and `00` identify the same parsed integer inside one Rules registry.

## Decision

- A multi-match Art field is `Ambiguous`, has no single `Parsed` value, and
  retains every parsed candidate with its complete source trace.
- Ambiguous candidates are placed in a deterministic canonical order before
  model hashing. The field produces no resource reference and cannot determine
  an SHP/VXL route.
- Equal parsed ordinals are diagnosed only within one Rules registry. All
  entries and their original ordinal spellings remain; no winner is selected.
- Both conditions make the typed result `Incomplete`. They do not select stock
  semantics or change ProjectBaseline precedence.

## Consequences

Callers cannot accidentally consume `matches[0]` or infer first/last registry
behavior. The model remains useful for evidence and later research while
making unresolved identity explicit. Existing ProjectBaseline aggregates and
model hashes remain stable when no real Art ambiguity or duplicate ordinal is
present.
