# MAP/MPR/YRM container and INI shell

## 1. Document shape

RA2/YR maps are text documents compatible with an INI-style parser. They mix ordinary configuration with Base64 fragments that reconstruct compressed binary data.

The first parser stage should be the repository's lossless INI document model. It must preserve:

- section occurrence order;
- key occurrence order;
- duplicate sections and keys;
- original names and comparison candidates;
- comments, blank lines and unrecognized text where supported;
- exact raw value text;
- encoding/BOM/newline diagnostics;
- source/provenance and bounded offsets.

Recognized semantic views must reference occurrences rather than destroy the raw document.

## 2. Family-role metadata

Suggested map identity fields:

```text
MapContainerIdentity
- LogicalName
- ExtensionRaw
- FamilyCandidate: Map | Mpr | Yrm | UnknownTextMap
- DiscoveryRole: OfficialScenario | CustomMultiplayer | PackageMember | Unknown
- ProviderProvenance
- Length
- Sha256
```

Extension is discovery evidence, not sufficient proof of scenario semantics. `[Basic]`, PKT/campaign references and content-provider context remain separate evidence.

## 3. Foundational sections

### `[Basic]`

Carries version/format and scenario metadata. `NewINIFormat=4` is a strong RA2/YR-family candidate, but changing one value does not convert a map from another game family.

### `[Map]`

Common keys include:

- `Size=` — full isometric map rectangle;
- `LocalSize=` — playable/visible local rectangle;
- `Theater=` — theater profile name.

Parse comma fields with checked integer conversion and preserve extra/missing fields as diagnostics. Do not allocate terrain from unvalidated dimensions.

### `[Preview]`

`Size=x,y,width,height` describes the dimensions used to interpret `PreviewPack`. The first two fields are commonly zero but should remain raw.

## 4. Packed sections

Recognized candidates include:

- `[IsoMapPack5]`
- `[OverlayPack]`
- `[OverlayDataPack]`
- `[PreviewPack]`

Their values are concatenated in a defined key order before Base64 decoding. The reader must not rely on dictionary enumeration.

Recommended ordering policy:

1. parse canonical decimal keys using invariant culture;
2. preserve all original occurrences;
3. diagnose duplicate normalized numeric keys;
4. sort unique numeric keys ascending for the packed-view candidate;
5. retain nonnumeric keys as unresolved evidence rather than appending them arbitrarily;
6. never select among duplicate keys by filesystem order or hash.

Historical tools normally write keys beginning at `1`, but key-base legality should remain explicit rather than being inferred from one writer.

## 5. Object placement sections

Common placement sections include:

- `[Structures]`
- `[Units]`
- `[Infantry]`
- `[Aircraft]`
- `[Terrain]`
- `[Smudge]`
- `[CellTags]`
- `[Waypoints]`

Values are comma-separated records with section-specific schemas. A lossless record model should retain:

- raw field strings;
- field count;
- parsed candidates;
- record identity/key occurrence;
- extra fields;
- unresolved references;
- exact source occurrence.

Do not use `Split(..., RemoveEmptyEntries)` in a lossless reader because empty positional fields are meaningful evidence.

## 6. House and mission-definition sections

Relevant registries and definition sections include:

- `[Countries]` and/or house-type lists;
- `[Houses]` and individual house sections;
- `[Triggers]`, `[Events]`, `[Actions]`;
- `[Tags]`;
- `[TeamTypes]` and per-team sections;
- `[TaskForces]` and per-task-force sections;
- `[ScriptTypes]` and per-script sections;
- `[AITriggerTypes]` and `[AITriggerTypesEnable]`;
- `[VariableNames]` and other mission-local state sections.

These form a directed reference graph. Parsing and graph binding should be separate passes.

## 7. Unknown and extension sections

A map may contain:

- map-local Rules sections;
- `ART.<section>` extensions;
- editor metadata such as `Editor*` sections;
- Ares/Phobos-specific fields;
- mod-specific sections;
- unknown future fields.

The default round-trip model must preserve them. Typed views can ignore unsupported semantics without deleting source occurrences.

## 8. Section order

Some modern editor code states that original TS/YR executables expect `[Preview]` and `[PreviewPack]` near the beginning and actively moves them first when saving. This is strong tool/runtime-observation evidence but not a license to reorder all sections.

Recommended policy:

- preserve source order by default;
- expose an explicit compatibility writer profile when a section-position requirement is independently accepted;
- never reorder during read;
- record every normalization performed by a writer.

## 9. Size and encoding limits

Before semantic parsing, bound:

- total map bytes;
- line length;
- section count;
- key/occurrence count;
- comment/raw-text bytes;
- value length;
- packed-fragment character total;
- duplicate diagnostics;
- decoded and decompressed outputs.

Malformed text must not cause quadratic concatenation, unbounded diagnostics or culture-dependent number parsing.

## 10. Canonical model hashing

A safe canonical shell hash may cover:

- normalized occurrence kind and ordinal;
- raw section/key/value byte hashes;
- duplicate classifications;
- recognized packed-section metadata without decoded body publication;
- source identity and parse-policy version.

Public audit output must not include original map text or per-occurrence value hashes if they could expose content.
