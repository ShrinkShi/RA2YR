# Engineering maintainability rules

These rules apply to every implementation and evidence PR.

## Layering

- Readers parse bounded structure and preserve raw fields.
- Decoders transform a validated payload under explicit budgets.
- Audits aggregate facts and write only sanitized evidence.
- Adapters translate Core results into host- or provider-specific objects.
- A layer may consume the public contract of the preceding layer; it must not
  reach through and reuse private assumptions or mutable state.

## Independent fixtures

Synthetic fixture builders must not call or copy the production decoder path
that they are intended to test. Expected values and encoded fixtures require
an independently stated contract so a production defect cannot validate
itself.

## Complexity and constants

- Split complex classes by responsibility. If a deliberately cohesive class
  remains large, document the reason and its test boundary in an ADR.
- Use named constants for format sizes, markers, flags, limits, and offsets.
  Unexplained numeric format literals are not accepted.
- Preserve raw fields even when a derived interpretation exists. Unknown or
  unresolved values remain representable and diagnosable.
- File-driven allocation and arithmetic remain bounded and checked before use.

## Pull-request evidence contract

Every format or behavior PR updates the relevant:

- ADR or explicit decision note;
- format/architecture documentation;
- synthetic test matrix;
- sanitized ProjectBaseline or interoperability evidence when applicable;
- compatibility matrix;
- known limitations and development record.

Status is promoted only after the corresponding evidence exists. Research,
audit observations, and configured project policy are not silently relabeled
as original-runtime confirmation.
