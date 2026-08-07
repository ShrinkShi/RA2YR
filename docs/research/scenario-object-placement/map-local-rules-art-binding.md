# Map-local Rules and Art binding

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Scope

This document defines how a placement record consumes already-composed Rules and optional Art views. It does not implement composition, archive discovery, Rules parsing, Art parsing, or visual asset loading.

## Ordered Rules composition

Placement binding must use the repository's established ordered composition rather than opening one Rules file in isolation:

```text
RA2 base Rules
→ YR Rules override
→ expandmd01..99 layers in established order
→ loose Rules layers in established precedence
→ map-local sections under explicit policy
→ SectionName + KeyName semantic composition
→ typed registries and type sections
```

The exact archive and file precedence is owned by the content-load-order and INI-composition work packages.

## Registry plus type section

A placement type binding requires two related views:

1. registry membership, such as `[BuildingTypes]`;
2. the typed section named by the logical type ID.

A logical type can appear in one but lack the other. Keep these statuses separate:

- registered and section present;
- registered but section missing;
- section present but registry entry missing;
- duplicate registry name;
- duplicate ordinal;
- map-local addition;
- unknown type.

Do not fabricate registration solely because a named section exists unless an explicit extension profile permits that behavior.

## Placement-family registries

```text
Structure TypeRaw → BuildingTypes
Unit TypeRaw      → VehicleTypes
Infantry TypeRaw  → InfantryTypes
Aircraft TypeRaw  → AircraftTypes
Terrain TypeRaw   → TerrainTypes
Smudge TypeRaw    → SmudgeTypes
```

The placement record's logical name is preserved. Registry ordinals are metadata/provenance and must not replace that name.

## Provenance

Every composed binding should expose:

```text
ScenarioTypeResolutionTrace
- TypeRaw
- RegistryKind
- RegistryOccurrences
- TypedSectionOccurrences
- SelectedWinner
- SuppressedCandidates
- SourceLayers
- MapLocalContribution
- DuplicateGroups
- EvidenceGrade
- Diagnostics
```

This allows a later user or audit to explain why a type resolved without exposing raw ProjectBaseline content.

## Map-local additions and overrides

EA's released editor source checks global Rules registries and map-local type lists, demonstrating official editor support for map-local types. WAE initializes map Rules with map-local content and writes modified house/type properties into maps.

These facts support a map-local semantic layer, but do not prove every extension or editor internal ordinal behavior is accepted identically by the original runtime.

Project policy must specify whether a map-local layer may:

- override existing type properties;
- add registry entries;
- add a section without registry membership;
- alter house/country definitions;
- define Art behavior.

No implicit “map always wins everything” rule is introduced here.

## Owner and house composition

Owner binding can draw from:

- map `[Houses]` list;
- map-local house sections;
- global country/house definitions;
- known special house profiles.

A map-local house can reference a country/type defined in Rules. This is a two-stage binding, not one string lookup.

Keep separate:

```text
OwnerRaw
LogicalHouseId
HouseSectionBinding
CountryOrHouseTypeBinding
RuntimePlayerInstance // out of scope
```

## Art binding path

After Rules type binding:

```text
Logical Rules type
→ Image= candidate or default logical image name
→ Art section candidate
→ visual asset logical reference
→ SHP or VXL/HVA candidate
→ renderer adapter
```

Terrain and Smudge typically lead to SHP-like visual candidates; Units and many Aircraft lead to VXL/HVA candidates; Infantry and many Buildings lead to SHP candidates. These are type-specific possibilities, not parser decisions.

## Visual binding cannot repair type binding

Forbidden feedback loops:

- choose a different type because the expected SHP is missing;
- bind an unknown placement by matching a VXL filename;
- infer a registry ordinal from Art section order;
- delete a placement because no image is found;
- decide vanilla versus extension type from visual plausibility.

## Art is not simulation authority

Art may expose visual metadata candidates, but it does not alone determine:

- owner;
- health;
- mission;
- current hit points;
- foundation/collision solely from image dimensions;
- passability;
- weapon or locomotor;
- bridge layer;
- player instance.

Rules and later simulation systems own gameplay properties.

## Image override and logical identity

`Image=` can redirect visual lookup without changing the placement's Rules type identity. Preserve:

```text
PlacementTypeId
RulesTypeId
ImageOverrideRaw
ArtSectionId
VisualAssetId
```

Do not collapse them into one string.

## Theater-specific visual references

Some Art references vary by theater or use theater suffixes. Theater selection belongs to a visual-resource resolver that consumes an already-bound scenario theater profile.

The placement parser does not inspect file extensions or current theater to decide whether the object type exists.

## Missing Art

Recommended outcomes:

- `RulesTypeBoundArtBound`;
- `RulesTypeBoundArtMissing`;
- `RulesTypeBoundVisualAssetMissing`;
- `RulesTypeUnboundArtNotAttempted`;
- `ArtAmbiguous`.

Only the first stage affects the typed placement descriptor. Missing Art is a visual diagnostic, not a syntax failure.

## Extensions

Ares, Phobos, WAE, MapTool, and other ecosystems can add types, fields, image rules, or local include behavior. Extension profiles must be explicit and provenance-rich.

Chrono Divide's documented `ART.<section>` convention is implementation-specific and must not be enabled as a vanilla RA2/YR map-local Art rule without explicit policy.

## Unity boundary

The Art binder returns logical references only. It does not load bytes or create:

- Texture2D;
- Sprite;
- Mesh;
- Material;
- GameObject;
- animation controller;
- collider;
- VFX.

## Round-trip

Lossless round-trip preserves placement tokens and map-local sections independently of whether the current environment can bind them. A different mod environment may resolve previously unknown types; therefore unresolved entries must never be discarded.
