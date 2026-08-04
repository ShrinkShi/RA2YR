# Implementation boundaries

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Design goal

Define a safe Core architecture that preserves source identity while allowing evidence-gated semantic interpretation. This is an interface and data-model proposal only; no implementation code is provided.

## Proposed top-level models

### Document and section

- `ScenarioPlacementDocument`
- `ScenarioPlacementSection`
- `ScenarioPlacementRecordRaw`
- `ScenarioPlacementTokenRaw`
- `ScenarioRecordKey`
- `ScenarioRecordLayoutProfile`

### Coordinates

- `ScenarioCellIdRaw`
- `ScenarioCellCoordinate`
- `ScenarioCoordinateInterpretation`
- `ScenarioCoordinateDomainAnalysis`

### Raw semantics

- `ScenarioObjectTypeRaw`
- `ScenarioOwnerRaw`
- `ScenarioHealthRaw`
- `ScenarioFacingRaw`
- `ScenarioMissionRaw`
- `ScenarioTagReferenceRaw`

### Binding and result

- `ScenarioPlacementDescriptor`
- `ScenarioTypeBindingResult`
- `ScenarioOwnerBindingResult`
- `ScenarioReferenceResolution`
- `ScenarioPlacementDiagnostic`
- `ScenarioPlacementReadLimits`
- `ScenarioPlacementConsistencyAnalysis`
- `ScenarioRoundtripDescriptor`

### Family-specific raw models

- `StructurePlacementRaw`
- `UnitPlacementRaw`
- `InfantryPlacementRaw`
- `AircraftPlacementRaw`
- `TerrainPlacementRaw`
- `SmudgePlacementRaw`
- `WaypointRaw`
- `CellTagRaw`

Family models may share interfaces after parsing but do not inherit one serialized universal field layout.

## Explicit policies

- `ScenarioRecordLayoutPolicy`
- `ScenarioTokenizationPolicy`
- `ScenarioCoordinatePolicy`
- `ScenarioTypeBindingPolicy`
- `ScenarioOwnerBindingPolicy`
- `ScenarioReferencePolicy`
- `ScenarioDuplicatePolicy`
- `ScenarioDomainPolicy`
- `ScenarioExtensionPolicy`
- `ScenarioRoundtripPolicy`

All policies are explicit inputs, serializable, and included in result provenance. No policy is chosen by semantic plausibility or success count.

## Parser boundary

Input:

```text
bounded lossless INI section occurrences
+ read limits
+ tokenization policy
+ record-layout profile set
```

Output:

```text
raw placement records
+ raw token views
+ candidate numeric values
+ layout match results
+ diagnostics
```

The parser does not receive Rules, Art, houses, theater assets, or Unity services.

## Tokenizer boundary

The tokenizer consumes only the raw value span and produces indexed raw tokens. It must:

- retain empty tokens;
- retain trailing empty tokens;
- retain source whitespace;
- bound token count and total token characters;
- advance input on every step;
- avoid unbounded substring allocation;
- report invalid or unsupported quoting candidates;
- never replace invalid numeric text.

The lossless INI layer, not the placement tokenizer, owns comment recognition.

## Layout interpreter boundary

The layout interpreter maps token indices to candidate fields under an explicit profile. It does not rewrite tokens.

Result includes:

- canonical/minimum count;
- missing known fields;
- empty known fields;
- unknown tail fields;
- conflicting matching profiles;
- evidence grade.

If multiple profiles match, the result is ambiguous unless caller policy selected one before interpretation.

## Coordinate decoder boundary

Input:

- raw X/Y tokens or combined cell token;
- explicit coordinate profile;
- numeric limits.

Output:

- decoded candidate coordinate;
- checked-arithmetic status;
- profile and evidence;
- no Unity vector.

Map-domain validation is a later step and cannot change the coordinate.

## Owner binder boundary

Consumes:

- `ScenarioOwnerRaw`;
- composed map house descriptor;
- composed Rules house/country descriptors;
- explicit matching and special-house profile.

It does not create a house or player. It returns unique, ambiguous, known-special, or unresolved results.

## Type binder boundary

Consumes:

- family-specific TypeRaw;
- an already-composed registry descriptor;
- already-composed typed sections;
- explicit map-local/extension policy.

It does not scan MIX files, reopen Rules, inspect visual files, or create objects.

## Art resolver boundary

The optional resolver consumes a successfully bound logical Rules type and composed Art view. It returns logical visual candidates only.

No SHP/VXL/HVA bytes are read in this layer. Missing Art is not a placement parse failure.

## Reference graph boundary

Reference extraction creates opaque edges from raw fields. Resolution is identity matching only. Trigger, TeamType, TaskForce, and ScriptType execution is prohibited.

## Consistency analysis

A later analyzer may combine records without deleting them:

```text
ScenarioPlacementConsistencyAnalysis
- DuplicateKeyGroups
- NormalizedKeyCollisions
- DuplicateCoordinateGroups
- CooccupancyCandidates
- DomainFailures
- OwnerFailures
- TypeFailures
- DanglingReferences
- UnknownExtensionCounts
```

Analysis results are derived and never overwrite the raw document.

## Diagnostics

Suggested diagnostic dimensions:

### Syntax

- missing value;
- token count below profile;
- unknown tail;
- empty known field;
- unsupported quoting;
- invalid numeric/boolean;
- integer overflow.

### Key and duplicate

- duplicate raw key;
- normalized key collision;
- key gap;
- nonnumeric key under numeric profile;
- ambiguous source-order reference.

### Coordinate

- cell decode failure;
- negative candidate;
- radix component overflow;
- outside Size;
- outside LocalSize;
- missing IsoMap cell.

### Binding

- unknown owner;
- duplicate house identity;
- missing registry entry;
- duplicate type identity;
- missing type section;
- missing Art;
- unsupported extension.

### Reference

- dangling Tag;
- duplicate Tag;
- missing Trigger;
- ambiguous Follows basis;
- cycle candidate.

Diagnostics carry severity and evidence; a warning never becomes silent repair.

## Limits and safety

`ScenarioPlacementReadLimits` should include:

- maximum section occurrences;
- maximum records per family and total;
- maximum raw key length;
- maximum raw value length;
- maximum tokens per record;
- maximum total token characters;
- maximum duplicate-group size;
- maximum reference edges;
- maximum graph traversal depth for analysis;
- maximum diagnostics.

All arithmetic is checked. No input controls an unbounded allocation.

## Input equivalence

Memory, Stream, short-read Stream, and MIX entry window adapters feed the same lossless INI and placement state machines. Short reads must not change token boundaries, source order, diagnostics, or hashes.

A MIX adapter supplies a bounded byte window and logical provenance only. It does not choose a placement layout or Rules profile.

## No-progress protection

Every text/record/token loop must either:

- consume input;
- emit a bounded record/token;
- terminate;
- or return a structured failure.

Malformed delimiters or very long tokens cannot cause infinite loops.

## Fixture independence

Synthetic fixture builders must not reuse production:

- comma tokenization;
- cell coordinate formula;
- field index constants;
- owner/type resolution;
- duplicate grouping.

Expected data is assembled independently so a shared bug cannot make both production and fixture agree.

## Round-trip descriptor

```text
ScenarioRoundtripDescriptor
- LosslessIniIdentityPossible
- RawPlacementIdentityPossible
- TokenIdentityPossible
- SemanticBindingStable
- CanonicalRewriteProfile
- EditorReopenClaim
- RuntimeAcceptanceClaim
- PreservedUnknowns
```

Byte-identical round-trip and semantic round-trip are distinct claims.

## Forbidden dependencies

Core must not depend on:

- `UnityEngine`;
- file-system/MIX discovery inside placement readers;
- SHP/VXL/HVA/TMP/palette readers;
- Rules loader inside placement parser;
- trigger execution;
- AI/team execution;
- rendering, collision, pathfinding, or combat.
