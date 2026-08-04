# Numbered Base64 fragment envelope

## 1. Layer boundary

Packed map sections are ordinary lossless INI sections until a caller requests a packed view.

```text
LosslessIniSection
→ PackedIniFragmentCollector
→ strict Base64
→ chunk envelope
→ codec
```

The generic INI composer does not merge, deduplicate, or reorder packed fragments by ordinary key-override rules.

## 2. Common writer form

```ini
[IsoMapPack5]
1=...
2=...
3=...
```

WAE writes:

- numeric keys beginning at 1;
- ascending key order;
- up to 70 Base64 characters per fragment.

Because 70 is not divisible by four, fragments must be concatenated before Base64 decode. Per-fragment decoding is incorrect.

## 3. Raw and normalized views

The collector preserves:

- every key occurrence;
- raw key text;
- raw value text;
- source ordinal;
- section provenance;
- duplicate occurrence identity.

It separately creates a normalized candidate:

- parsed nonnegative integer key;
- numeric sort order;
- canonical fragment sequence;
- diagnostics.

No raw occurrence is destroyed.

## 4. Ordering conflict

Inspected public consumers commonly concatenate section values in their INI API enumeration/source order. Writers emit ascending numeric order. Static evidence does not prove whether the original game:

- uses source occurrence order;
- parses numeric keys and sorts;
- queries `1`, `2`, ... actively;
- stops at the first missing key.

Proposed explicit policies:

- `SourceOccurrenceOrder`
- `NumericAscendingUnique`
- `SequentialFromOneUntilMissing` experimental

The research default for deterministic fixtures is `NumericAscendingUnique`, labeled `ConfiguredProjectPolicy`, with a diagnostic when it differs from source order.

## 5. Key cases

| Case | Strict candidate behavior |
|---|---|
| `1..N` unique | accept |
| key `0` | unresolved; reject in vanilla strict profile |
| missing number | diagnostic; policy-dependent failure |
| duplicate raw key | fail ambiguity |
| `1` and `01` | duplicate normalized index; fail ambiguity |
| negative key | reject |
| signed plus key | reject |
| nonnumeric key | preserve raw; reject packed view if nonempty |
| empty fragment | preserve and diagnose; default reject |
| whitespace around key | handled by lossless INI parser; normalized view records canonical parse |
| inline comment | parser decides value boundary; collector never strips comments itself |

## 6. Base64 policy

After concatenation:

- ASCII Base64 alphabet only;
- padding only in final quantum;
- no data after padding;
- decoded-size preflight where possible;
- total character and decoded-byte limits;
- no MIME line-break tolerance beyond whitespace already represented outside the extracted INI value;
- case preserved because Base64 is case-sensitive.

Invalid text fails before chunk parsing.

## 7. Duplicate and INI composition boundary

A duplicate numeric fragment is not a normal “higher key occurrence wins” situation. Packed data is a positional transport stream. Selecting one occurrence could silently create a different compressed stream.

Therefore duplicates fail closed and retain both provenance entries.

## 8. Provenance

`PackedIniFragmentCollectorResult` records:

- logical section;
- policy;
- raw occurrence count;
- accepted fragment count;
- normalized index range;
- gaps;
- duplicate groups;
- nonnumeric groups;
- source-order/numeric-order disagreement;
- total character count;
- canonical collector hash.

Public audit output never includes fragment text.
