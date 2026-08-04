# Source comparison and license boundaries

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## 1. Method

This dossier compares public sources as evidence, not as code-import candidates. For each source it records:

- project and permanent locator;
- pinned commit/revision where available;
- relevant files/pages;
- license;
- reader, writer, editor, reimplementation, or runtime category;
- likely code/knowledge lineage;
- evidence grade;
- `reference_only: true` where appropriate;
- `code_imported: false`.

Agreement between related tools is not counted as multiple independent original-runtime votes.

## 2. Evidence grades

- `ConfirmedByOfficialRuntimeSource`: direct original-game runtime source. Not obtained for the complete graph.
- `ConfirmedByOfficialEditorSource`: EA public FinalSun/FinalAlert 2 editor behavior.
- `ConfirmedByIndependentImplementation`: public reimplementation/editor behavior independent enough to compare.
- `CommunityDocumented`: ModEnc, PPM, RA2 DIY, tutorials, and reverse-engineering notes.
- `ObservedByFutureProjectBaselineAudit`: future sanitized aggregate observations only.
- `ConfiguredForProjectPolicy`: defensive project decision.
- `Unresolved`: insufficient or conflicting evidence.

## 3. EA FinalSun / FinalAlert 2

- Project: `electronicarts/CNC_TS_and_RA2_Mission_Editor`
- Pin: `6abf0f557469baea73079c6bf6550709e2e3584e`
- License: GPL-3.0-or-later
- Category: official editor source, not original game runtime source
- Relevant paths:
  - `MissionEditor/functions.cpp`
  - trigger/team dialogs and map-data files
  - FinalSun/FinalAlert editor data INIs
- Independence: official editor implementation; includes XCC-related components elsewhere in repository
- Use: reference-only
- `code_imported: false`

Proves or strongly supports:

- editor-side Trigger repair/default behavior;
- editor parameter-type catalogs;
- identity selection and UI conventions;
- official editor read/write and repair behavior.

Does not prove:

- original runtime accepts the same malformed records;
- default values are format requirements;
- editor names are protocol-standard names;
- runtime Event/Action execution semantics;
- editor canonical output is byte-identical to stock maps.

## 4. World-Altering Editor

- Project: `CnCNet/WorldAlteringEditor`
- Pin: `b4c9481e9b00fb0a38739049a046f528b6054ce2`
- License: GPL-3.0-or-later
- Category: independent editor/reimplementation
- Relevant paths:
  - `Models/Trigger.cs`
  - `Models/TriggerCondition.cs`
  - `Models/TriggerAction.cs`
  - `Models/Tag.cs`
  - `Models/TeamType.cs`
  - `Models/TaskForce.cs`
  - `Models/Script.cs`
  - `Models/AITriggerType.cs`
  - `CCEngine/TriggerEventType.cs`
  - `Initialization/MapLoader.cs`
  - `Initialization/MapWriter.cs`
- Use: reference-only
- `code_imported: false`

Strongly supports:

- common Trigger eight-field writer profile;
- Tag three-field profile;
- Events count plus opcode and configured parameter count;
- Actions count plus opcode and seven parameter slots;
- TeamType list plus per-ID section;
- TaskForce six-slot editor model and `count,type` entries;
- Script `action,argument` entries;
- AITrigger 18-field candidate;
- editor handling of missing references and unknown editor-config opcodes.

Important limitations:

- WAE applies defaults, skips invalid records, or refuses unsupported editor data;
- WAE catalogs are configurable and may include extension behavior;
- reader behavior is not original runtime behavior;
- implementation names such as `ConditionIndex`, `Repeating`, and `BaseDefense` remain evidence labels, not protocol truth.

## 5. OpenRA

- Project: `OpenRA/OpenRA`
- Pin: `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`
- License: GPL-3.0-or-later
- Category: engine reimplementation/importer
- Use: supplementary reference-only
- `code_imported: false`

OpenRA can provide useful cross-checks for legacy map import and declarative trigger concepts, but its internal actor/trait model intentionally differs from Westwood runtime architecture. It is not used as the sole source for RA2/YR field or execution semantics.

## 6. CnCNet XNA client

- Project: `CnCNet/xna-cncnet-client`
- Pin: `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`
- License: GPL-3.0
- Category: map consumer/client tooling
- Use: supplementary reference-only
- `code_imported: false`

The client is relevant for map metadata and consumer compatibility, but it does not provide a complete original Trigger/AI execution implementation for this dossier.

## 7. MapTool

- Project: `Starkku/MapTool`
- Pin: `f85f2226905496139f1258b5854fad915f9bbac6`
- License: GPL-2.0-or-later
- Category: map transformation tool
- Use: reference-only
- `code_imported: false`

MapTool is useful for:

- corroborating map object and identity conventions;
- demonstrating transformation/canonicalization behavior;
- exposing extension-aware map processing.

It is not counted as independent runtime evidence when behavior follows shared community documentation or related parsing conventions.

## 8. CNCMaps

- Project: `zzattack/ccmaps-net`
- Pin: `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`
- License: repository default plus explicit imported-code exceptions; treat as mixed
- Category: renderer/map parser
- Use: reference-only
- `code_imported: false`

CNCMaps is useful as an additional consumer/editor-era implementation. Imported OpenRA/XCC-derived areas are not counted as independent lineage.

## 9. Chrono Divide public SDK

- Pin used by prior dossiers: `5943c4ae6c19897929d348a417d6d2f1481b75fd`
- Category: public SDK/reimplementation surface
- License: record per repository/module before any later use
- Use: reference-only
- `code_imported: false`

No complete, pinned public implementation was located that independently resolves all Trigger, TeamType, TaskForce, ScriptType, and AITrigger storage conflicts. Absence of evidence is recorded rather than filled with assumptions.

## 10. XCC / OmniBlade lineage

- Fixed mirror candidate used by prior dossiers: `6f91bf8b00d3acabb1be765118a37c0cb74e85ec`
- Category: legacy tool/library lineage
- License: verify per file/repository; generally GPL/reference-only for this project
- Use: reference-only
- `code_imported: false`

XCC-related knowledge appears in multiple tools and the EA editor repository. Those occurrences are not treated as multiple independent confirmations.

## 11. ModEnc permanent revisions

Relevant permanent/current pages include:

- Triggers;
- Events;
- Actions in maps;
- Tags;
- TeamTypes;
- TaskForces;
- ScriptTypes and Script actions;
- AITriggerTypes;
- AITriggerTypesEnable;
- VariableNames;
- map scripting hierarchy.

License/category: community documentation, quotation and attribution limits apply.

Use:

- field-name and community-semantic candidates;
- common writer/layout descriptions;
- version and editor warnings;
- identity graph descriptions.

Limitations:

- not official runtime source;
- pages may aggregate shared community knowledge;
- labels can reflect editor terminology;
- some claims explicitly remain incomplete or speculative.

## 12. Project Perfect Mod

Fixed forum discussions and tutorials are used for:

- AITrigger field interpretation;
- comparator and weights;
- Team/TaskForce/Script authoring behavior;
- extension and editor quirks.

Category: `CommunityDocumented`.

Forum consensus does not become runtime confirmation without stronger evidence.

## 13. Ares documentation

- Category: extension documentation
- Relevant areas: Trigger Events, Trigger Actions, editor support, scripting extensions
- Use: explicit `AresExtensionProfile` only
- `code_imported: false`

Ares proves that:

- Event/Action opcode spaces can be extended;
- Event parameter shapes can differ from vanilla candidates;
- extension-specific editor metadata exists;
- new semantic categories require profile isolation.

Ares semantics must never be described as stock RA2/YR behavior.

## 14. Phobos documentation

- Category: extension documentation
- Relevant areas: AI scripting, Trigger/Script actions, AITrigger weight operations, target groups
- Use: explicit `PhobosExtensionProfile` only
- `code_imported: false`

Phobos documents high Script-action ranges and new AI behaviors. This is strong extension evidence and a warning against range-based rejection, but not vanilla evidence.

## 15. RA2 DIY and other tutorials

Only publicly fixed pages, archived revisions, or stable code dictionaries should be cited in a future update. Unstable forum recollection must be labeled accordingly.

Category: community/editor guidance.

## 16. Lineage cautions

The following may share code or knowledge lineage:

- EA editor and bundled XCC components;
- CNCMaps and imported OpenRA/XCC code;
- tools derived from ModEnc field tables;
- community editors using the same FinalAlert conventions;
- Ares/Phobos editor catalogs copied into third-party tools.

The source matrix records lineage to prevent false confidence by vote counting.

## 17. License policy

For the future implementation:

- do not port GPL source into project Core;
- do not translate switch statements or parsers line-by-line;
- do not reproduce near-structural C# pseudocode from GPL control flow;
- derive independent specifications from factual record shapes and independently designed fixtures;
- retain source and evidence attribution in research documentation;
- conduct a separate dependency/license review before adopting any parser library or catalog data.

## 18. Opcode catalog licensing

An opcode catalog can contain facts, names, and evidence annotations, but copying a complete GPL editor catalog or its descriptive prose may create licensing concerns.

Recommended route:

1. define an original schema;
2. enter numeric facts and short neutral labels from multiple sources;
3. record provenance per entry;
4. avoid copying long descriptions;
5. keep extension catalogs separate;
6. have legal/license review before distribution if large tables are imported from a single source.

## 19. Evidence conflict examples

- Trigger final field: repeating, reserved, or unused depending on source/tool.
- Event tuple width: fixed base versus opcode/editor-config additional parameters.
- unknown Event handling: editor refuses map versus lossless parser preservation.
- Action parameter 7: raw slot versus editor-generated `A` fallback.
- TaskForce gaps: editor slots, community ascending order, or runtime unknown.
- Script gaps: WAE stops at first gap, while lossless Core preserves later entries.
- AITrigger fields 11 and 13: unused, unknown, or named behavior candidates.
- global/local identity: `-G` convention versus source-layer truth.

Each conflict remains visible in the data model.

## 20. Source matrix summary

| Source | Category | Key contribution | Runtime proof? | Import |
|---|---|---|---|---|
| EA editor | official editor | repair/UI/read-write behavior | No | false |
| WAE | independent editor | layouts and typed models | No | false |
| OpenRA | reimplementation | supplementary cross-check | No | false |
| CnCNet client | consumer | compatibility context | No | false |
| MapTool | transformation tool | parser/writer cross-check | No | false |
| CNCMaps | renderer/parser | supplementary behavior | No | false |
| Chrono Divide | SDK | limited independent context | No | false |
| XCC/OmniBlade | legacy lineage | historical format knowledge | No | false |
| ModEnc | community docs | field/layout candidates | No | false |
| PPM | community discussion | AI/trigger details | No | false |
| Ares | extension docs | extension opcodes/params | Extension only | false |
| Phobos | extension docs | extension Script/AI behavior | Extension only | false |

## 21. Conclusion

The public evidence is sufficient to design a raw-preserving declarative graph API and comprehensive tests. It is not sufficient to claim complete original RA2/YR execution semantics. Implementation must remain independent, profile-driven, and separated from the future executor.
