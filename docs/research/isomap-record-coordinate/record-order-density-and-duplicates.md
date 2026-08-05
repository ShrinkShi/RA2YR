> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# Record order, density, missing cells, and duplicates

## Separate models

The implementation design must retain all of:

```text
SourceRecordOrder
CoordinateIndex
DenseCanvasExpectation
MissingCoordinateSet
DuplicateCoordinateGroups
OutOfDomainRecords
SparseRecordSet
```

None is a substitute for another.

## Evidence classification

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| Official editor/tool paths support a dense valid-cell traversal | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Establishes official editor behavior, not a universal stock-runtime requirement. XCC/OpenRA and other tools corroborate with lineage caveats. | Dense expectation is an explicit analysis profile. | `NotRun` |
| Modern tools intentionally emit sparse streams | `Underconfirmed` | WAE and CNCMaps; ModEnc documentation | Writer behavior is observed, but source independence and stock RA2/YR sparse acceptance are not established. | Preserve sparse input and distinguish missing from explicit default. | `NotRun` |
| Omitted level-0 clear cells are always reconstructed identically by stock runtime | `Underconfirmed` | ModEnc community documentation | Community documentation supplies a candidate runtime behavior without original-runtime source proof. | Never synthesize during raw parsing; any effective default view is policy-labeled. | `NotRun` |
| Public tools exhibit last-assignment effects for duplicate coordinates | `ImplementationSpecificBehavior` | Array/dictionary-based tool implementations | Accidental overwrite behavior is implementation-specific and cannot become a universal format rule. | Preserve every occurrence; never select an implicit winner. | `NotRun` |
| Conflicting duplicates fail closed in the project default | `DefensiveDesign` | Project policy | This is a preservation and ambiguity-handling decision, not external evidence. | Retain all records and mark the effective coordinate ambiguous. | `NotRun` |
| Stock runtime duplicate winner semantics | `Unresolved` | No original-runtime source located | First-wins, last-wins, undefined, or fatal behavior remains unknown. | Compatibility profiles require new target-specific evidence. | `NotRun` |

## Dense behavior

Evidence supporting dense streams:

- EA FinalSun/FinalAlert 2 builds records for valid cells selected from its diamond traversal.
- XCC contains canonical diamond traversal routines.
- OpenRA's importer calculates `(2W-1) × H` cells and reads exactly that many records.
- CNCMaps and MapTool allocate a dense target and prefill missing decoded positions before reading.

This establishes strong editor/tool convention, not official runtime proof that every accepted stream must be dense.

A dense candidate means exactly one record for every valid normalized canvas coordinate. It does not mean the source records must be in canvas order.

## Sparse behavior

Evidence supporting sparse streams:

- WAE's writer removes records whose `Level == 0` and `TileIndex == 0`.
- CNCMaps has an explicit compression mode that removes default clear records and chooses a compression-friendly sort.
- ModEnc documents omission of level-0 clear cells and game-side/default reconstruction.

The first two prove modern tool behavior. ModEnc supplies community runtime documentation, but no official runtime source was found. Sparse acceptance therefore remains below official-runtime grade.

## Missing versus explicit default

These states are distinct:

1. no record exists for a coordinate;
2. a record exists with tile 0, subtile 0, level 0, final byte 0;
3. a record exists with tile 0 and nonzero metadata;
4. a record exists but its tile interpretation is unresolved;
5. a record exists outside the valid coordinate domain.

The raw parser must not convert state 1 into state 2. A later `MissingCellPolicy` may produce an effective default view while retaining the distinction and policy provenance.

## Record order

Observed writer/order behaviors include:

- official editor diamond traversal;
- XCC canonical diamond traversal;
- WAE sorting by X, then Level, then TileIndex;
- CNCMaps trying multiple sort keys and selecting the smallest compressed output;
- dictionary or array enumeration in tools;
- arbitrary legal source order candidate.

Therefore source order is not a semantic identity key.

Required analysis:

```text
IsoMapOrderClassification
  SourceOrderHash
  IsCanvasOrder
  IsRawXYAscending
  IsRawYXAscending
  IsWriterProfileCandidate
  HasOrderTies
  EvidenceGrade
```

No parser should reorder the stored document. A separately derived canonical view may sort records under an explicit policy.

## Duplicate coordinates

A duplicate group contains two or more records that resolve to the same coordinate under the selected coordinate profile.

Subtypes:

- `ByteIdenticalDuplicate`: all 11 bytes match;
- `SemanticIdenticalDuplicate`: selected interpretations match but raw unknown bytes differ;
- `ConflictingTileDuplicate`;
- `ConflictingSubTileDuplicate`;
- `ConflictingLevelDuplicate`;
- `ConflictingFinalByteDuplicate`;
- `InterpretationDependentDuplicate`: grouping differs by signedness or axis profile.

Public tools frequently overwrite an array or dictionary cell, producing effective last-wins behavior. That is implementation-specific and often accidental. It is not a suitable default.

## Project duplicate policy

```text
EvidenceGrade: DefensiveDesign
PolicyClassification: ProjectPolicy
```

- preserve every record and its ordinal;
- group duplicates after coordinate interpretation;
- do not choose first-wins or last-wins;
- mark the effective coordinate as ambiguous when records conflict;
- permit an explicit forensic view to show every candidate;
- allow an opt-in compatibility profile only after evidence identifies a target behavior;
- include duplicate-group membership in provenance and resolution traces.

Byte-identical duplicates are still duplicates. They may be downgraded to a warning by policy, but must remain observable.

## Out-of-domain records

Records outside the selected coordinate domain remain in `SourceRecordOrder` and `OutOfDomainRecords`. They do not enter the effective coordinate index.

Do not:

- clamp them to an edge;
- swap axes until they fit;
- wrap them with modulo arithmetic;
- silently drop them;
- count them as missing-domain fillers.

## Record count classifications

Given expected dense count `E = (2W-1) × H` and parsed full records `N`:

- `N == E` does not prove dense uniqueness; duplicates and missing coordinates can cancel.
- `N < E` is a sparse candidate only after checking domain and duplicates.
- `N > E` implies duplicates, out-of-domain records, a wrong dimension/profile, or another unresolved structure.

A robust density result reports:

```text
ExpectedDomainCount
SourceRecordCount
DistinctInDomainCoordinateCount
MissingCoordinateCount
DuplicateGroupCount
OutOfDomainRecordCount
DensityClassification
```

## Canonical writer boundary

A future canonical writer needs a named policy for:

- dense versus sparse emission;
- default-cell definition;
- source versus canonical order;
- duplicate rejection or compatibility selection;
- trailer emission;
- raw unknown-byte preservation;
- tile-field interpretation.

M3-R4 does not choose such a writer policy. Original byte-identical roundtrip requires preserving source order, duplicate records, unknown fields, decoded trailer, fragment grouping, and chunk boundaries or retaining the original packed representation.

## Security and budgets

- record count budget before building lists;
- coordinate-index entry budget;
- duplicate-group budget;
- diagnostic budget with truncation summary;
- checked expected-count arithmetic;
- deterministic results under shuffled input enumeration;
- no unbounded search for a missing coordinate or order profile.