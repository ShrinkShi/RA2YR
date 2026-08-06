# Resource Overlay boundaries

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Scope

This document separates map storage from the resource economy and rendering systems used for ore, gems, Tiberium-derived families, and extension-defined resources.

It does not define the final RA2/YR harvest algorithm.

## 2. Separate concepts

The following must not be collapsed:

- `OverlayTypeRaw`;
- bound Overlay registry entry;
- resource-family classification;
- `OverlayDataRaw`;
- image/frame candidate;
- resource stage or density candidate;
- resource growth/spread state;
- Rules resource value;
- remaining harvest value;
- harvester interaction;
- terrain passability;
- rendering variation;
- editor display state;
- simulation ownership or depletion state.

A map stores a type/data pair. It does not directly store all economic and gameplay results.

## 3. Public-source observations

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert/FinalSun resource-money estimates use the Overlay type/data pair | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Confirms editor-side accounting behavior only; it does not prove the original game's economy formula. | Keep editor estimates outside the raw parser. | `NotRun` |
| OpenRA applies a type-specific `ResourceFromOverlay` conversion | `ImplementationSpecificBehavior` | OpenRA importer | Demonstrates named-importer behavior and type-specific treatment, not stock-runtime semantics. | Preserve as a source-pinned adapter example. | `NotRun` |
| WAE links resource-family metadata while storing the raw byte as `FrameIndex` | `ImplementationSpecificBehavior` | World-Altering Editor | Shows a distinction between editor storage naming and resource semantics. | Keep the WAE semantic profile explicit. | `NotRun` |
| Community documentation calls the byte a frame index and associates resource families with the packed sections | `ConfirmedCommunityConvention` | ModEnc | Confirms a stable convention, not a complete runtime economy model. | Use as a candidate profile source only. | `NotRun` |
| One universal stock-runtime mapping from data byte to stage, credits, and remaining harvest value is established | `Unresolved` | No original-runtime source located | Stored stage, rendered frame, Rules value, and mutable harvest state are separate. | Do not expose a single universal quantity. | `NotRun` |

## 4. Resource semantic profile

A future resource profile may expose candidates such as:

```text
ResourceOverlaySemanticCandidate
- ResourceFamily
- VisualFrameCandidate
- StageOrDensityCandidate
- GrowthCandidate
- DepletionCandidate
- RulesValueReference
- EvidenceGrade
- Diagnostics
```

The profile must be selected from the bound type and configured game/extension, not inferred from color, image availability, or raw data distribution. Explicit profile selection is `DefensiveDesign`.

## 5. OverlayData and resource quantity

Do not equate:

```text
OverlayDataRaw
= resource amount
= frame index
= credit value
= remaining harvest value
```

Even where a value influences visual density and harvest quantity, conversion can depend on:

- resource family;
- hardcoded stage tables;
- Rules values;
- game version;
- extension behavior;
- simulation changes after map load.

The map's raw byte is only an input. The broader resource-stage candidate is `Underconfirmed`; exact runtime quantity and depletion semantics remain `Unresolved`.

## 6. Growth and spread

Resource growth/spread systems can involve:

- nearby cells;
- terrain suitability;
- resource generators or overlay families;
- timers and scenario rules;
- extension-specific logic.

These systems are outside the storage parser. The raw map record does not imply that growth is enabled or that a particular cell will retain its initial resource state.

## 7. Rendering boundary

Rendering may use:

- bound Overlay Art;
- `OverlayDataRaw` or a derived stage as a frame selector;
- theater palettes;
- lighting;
- random/variant rules;
- animation and shadow layout.

The renderer consumes semantic results. It does not decide registry ordinal or mutate raw map arrays. Art/frame evidence cannot be promoted to complete runtime resource semantics.

## 8. Harvest boundary

Harvester interaction belongs to simulation and may depend on:

- bound resource family;
- cell resource state;
- Rules values;
- harvester capacity and logic;
- depletion and replacement behavior;
- multiplayer determinism.

No harvest state is generated in the Overlay decoder.

## 9. Passability boundary

An Overlay may affect movement or building placement, but passability is not the data byte alone. Inputs can include:

- Overlay type flags;
- terrain/Land type;
- resource-family gameplay rules;
- bridges or walls sharing the Overlay registry;
- extension behavior.

The storage layer reports presence and binding only.

## 10. Unknown resource candidates

The following are `DefensiveDesign` requirements. When a type appears resource-like but evidence is insufficient:

- retain type/data raw values;
- retain all candidate profiles;
- do not classify by name substring;
- do not use image color or ore-like frames as proof;
- emit `ResourceSemanticUnderconfirmed`;
- allow future ProjectBaseline aggregate observation without publishing names or positions.

## 11. Consistency diagnostics

Useful diagnostics include:

- resource type with no semantic profile;
- data outside a documented stage range;
- empty type with nonzero data;
- bound resource type with missing Art;
- resource type outside scenario map domain;
- resource type at a coordinate with no IsoMap cell;
- extension resource under a vanilla profile;
- derived value overflow or unsupported Rules reference.

No diagnostic authorizes cleanup.

## 12. Roundtrip and preservation

A lossless map model must retain:

- raw type/data pairs for every storage coordinate;
- map-domain-external resource-like bytes;
- unknown high type IDs;
- values that the selected resource profile cannot interpret;
- original compressed/source provenance.

This preservation is `DefensiveDesign`. A canonical writer that regenerates resource frames from a high-level quantity is a separate future feature and cannot claim byte-identical roundtrip.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```
