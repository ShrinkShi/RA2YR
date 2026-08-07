# Identity, ownership, and Rules type binding

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. GPL and unclear-license code was not copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Three identities must not be conflated

For techno placement records, distinguish:

1. the INI record key;
2. the logical object type token in the value;
3. any runtime object identity created after loading.

The record key is commonly written as a numeric list position, but ModEnc documents techno record keys as any unique string. Public writers generally emit sequential decimal keys. This supports a writer convention, not a proven runtime object-ID contract.

Core stores:

```text
ScenarioRecordKey
ScenarioObjectTypeRaw
ScenarioRuntimeIdentityCandidate // never created by parser
```

A future writer cannot renumber by default because fields such as Unit `FollowsIndex` may refer to list/source order rather than a stable raw key.

## Owner raw model

```text
ScenarioOwnerRaw
- TextRaw
- NormalizedCaseCandidate
- NoneSentinelCandidate
- SourceSpan
```

The raw string is preserved even when no house can be resolved.

## House identity layers

Owner binding must distinguish:

- map `[Houses]` list identity;
- a map-local house section;
- Rules country or house type identity;
- an editor-created missing house;
- multiplayer-generated runtime player instances;
- special candidates such as Neutral, Special, or civilian houses;
- alliance, diplomacy, color, side, and starting-slot state.

Only logical identity binding belongs in this work package. It does not create runtime players, alliances, colors, or diplomacy.

## Public editor leniency

WAE resolves techno owners through a `FindOrMakeHouse` style path, creating editor models for owner names that are absent. That is useful editor recovery behavior, but the strict Core binder must not silently create a house and then report a successful source binding.

Recommended outcomes:

- `BoundToDeclaredMapHouse`;
- `BoundToComposedRulesHouseCandidate`;
- `KnownSpecialHouseCandidate`;
- `UnknownOwner`;
- `DuplicateHouseIdentity`;
- `EditorFabricationAvailable`.

`UnknownOwner` never becomes Neutral automatically.

## Case and duplicates

House/type matching policies must be explicit. Public tools commonly use case-insensitive INI identities, but raw spelling remains part of source identity.

A binder retains:

- raw token;
- normalized candidate;
- every matching source occurrence;
- selected winner under the configured composition policy;
- suppressed candidates;
- ambiguity diagnostics.

Duplicate house IDs or duplicate registry names cannot be silently merged.

## Type registries

Each placement family binds to a separate composed Rules registry:

| Placement family | Registry candidate | Typed section candidate |
|---|---|---|
| Structures | `[BuildingTypes]` | section named by `TypeRaw` |
| Units | `[VehicleTypes]` | section named by `TypeRaw` |
| Infantry | `[InfantryTypes]` | section named by `TypeRaw` |
| Aircraft | `[AircraftTypes]` | section named by `TypeRaw` |
| Terrain | `[TerrainTypes]` | section named by terrain value |
| Smudge | `[SmudgeTypes]` | section named by smudge type token |

Placement values refer to logical names, not directly to the registry ordinal. The registry ordinal is still preserved for provenance and conflict analysis.

## Ordered composition input

The binder consumes the already-composed semantic INI view. It does not scan archives or independently load Rules files.

The intended composition path is:

```text
RA2 base
→ YR layer
→ expandmd01..99 in established order
→ loose files in established precedence
→ map-local layer only under explicit policy
→ SectionName + KeyName composition
→ typed registry and type sections
```

The exact content-discovery rules belong to the content-load-order dossier. This dossier only specifies the placement binding interface.

## Registry descriptor

```text
ScenarioTypeRegistryDescriptor
- RegistryKind
- NumericKeyRaw
- NumericOrdinalCandidate
- LogicalNameRaw
- SourceLayer
- SourceOccurrence
- Winner
- SuppressedCandidates
- MapLocalContribution
- DuplicateOrdinalGroup
- DuplicateNameGroup
- GapAnalysis
- EvidenceGrade
```

The binder must not:

- infer ordinals from section enumeration order;
- compress gaps;
- move later IDs because an earlier entry or resource is missing;
- choose a type because its Art image exists;
- choose a different type when the configured type has no visual resource;
- treat extension types as vanilla.

## Official editor evidence

EA's released FinalSun/FinalAlert source searches the global Rules registry first and then a map-local registry for BuildingTypes and TerrainTypes. It gives map-local editor entries a separate internal number range. This confirms editor support for map-local type identity, not the original runtime's exact internal ordinal scheme.

The project therefore preserves map-local contributions but does not import the editor's internal numeric offsets into the public Core identity model.

## Unknown and missing types

A placement with an unknown type can still have:

- valid lossless syntax;
- valid token count;
- valid coordinate;
- valid owner;
- valid references.

The binder returns an unresolved placement descriptor rather than deleting the record.

Suggested result:

```text
ScenarioTypeBindingResult
- TypeRaw
- RegistryKind
- RegistryCandidates
- SelectedLogicalType
- TypedSectionCandidate
- Status
- EvidenceGrade
- Diagnostics
```

## Art is a later optional binding

After a logical Rules type is resolved, an optional Art resolver may determine:

- `Image=` override;
- logical Art section;
- SHP/VXL/HVA candidate identity;
- theater-specific visual candidate;
- VisualAssetId.

It does not determine:

- whether the placement record is valid;
- owner identity;
- health or mission;
- foundation, collision, or footprint solely from image dimensions;
- whether a missing image should cause type rebinding.

## Map-local Rules and Art

Map-local sections can modify or add type properties only through explicit composition policy. The placement binder records whether a selected type or property came from:

- global base;
- YR override;
- expansion layer;
- loose file;
- map-local section.

Map-local Art behavior remains a separate profile. Chrono Divide's `ART.<section>` convention and other extensions are not enabled as vanilla defaults.

## Round-trip identity

Lossless round-trip must preserve:

- record key spelling;
- owner/type spelling and case;
- duplicate keys;
- unresolved owner/type tokens;
- registry and type provenance;
- unknown extension fields.

Canonicalizing type or owner names to the composed winner is not lossless and cannot be the default writer behavior.
