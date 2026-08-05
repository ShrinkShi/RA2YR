# Layer and section boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. `code_imported: false`.

## Lossless input boundary

The scenario INI layer retains every physical section/key occurrence, exact spelling, raw value text, whitespace, empties, comments where supported, duplicates, physical order and source provenance. It does not merge Lighting sections, parse decimals, normalize media identifiers, bind Rules types, execute Triggers, apply palettes or construct presentation state.

## Section ownership

- `[Lighting]`: raw normal/Ion/Dominator/extension profile candidates; not palettes, current storm, visibility or playback state.
- `[SpecialFlags]`: mixed capability/configuration metadata; environment keys are classified individually and do not transfer ownership of the whole section.
- `[Basic]`: Theme/media and general scenario metadata remain separate.
- `[AudioVisual]`: Rules-layer or explicit map-local Rules data only under a named composition policy.
- Trigger sections: raw graph input supplied by the Trigger boundary; environment commands remain declarative.
- Rules type sections: local-light and sound properties are type data, not placed-object runtime state.
- Placement sections: identify instances and authored state; they do not redefine type-level fields.
- `[Map] Theater`: raw logical Theater token; downstream palette/resource binding cannot rewrite it.
- lobby/session/savegame visibility and weather settings: separate layers, never folded into authored map metadata.

## Ordered composition

Composition is section/key-aware. Scenario Lighting and SpecialFlags remain scenario metadata; map-local Rules composition is applied only to explicitly classified type/AudioVisual sections; client options do not become map values. Duplicates remain raw, and any first/last/editor-compatible semantic candidate records winners, suppressed occurrences and provenance.

## Resource and command boundary

Logical Theme/Sound/Speech/Movie/ObjectType/Theater references are produced before resource discovery. Missing or duplicate resources do not rewrite identifiers. Trigger opcode/parameters may produce an `EnvironmentCommandCandidate`, but parsing never activates lighting/weather, reveals cells, plays audio, fades screens or schedules timers.

## Evidence rules

| Evidence | Grade |
|---|---|
| FinalAlert/FinalSun fields, labels and authoring behavior | `ConfirmedByOfficialToolSource` |
| Named tool/client/renderer/extension behavior | `ImplementationSpecificBehavior` |
| Stable community field/media conventions | `ConfirmedCommunityConvention` |
| Cross-tool candidates with unproven lineage/runtime applicability | `Underconfirmed` |
| Direct composition, field or layer disagreement | `ConflictingSources` |
| Exact runtime behavior without sufficient evidence | `Unresolved` |
| Raw preservation, explicit composition, no fallback/execution | `DefensiveDesign` |

No claim here reaches `ConfirmedByOriginalRuntimeSource` or `ConfirmedByMultipleIndependentImplementations`.

Future ProjectBaseline work remains separate:

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields do not imply that ProjectBaseline was read or observed.

## Roundtrip

Lossless roundtrip preserves duplicate sections/fields, numeric spelling, locale forms, invalid/unknown keys, SpecialFlags text, media casing, Trigger parameters, extension fields and editor/client-private sections. Canonical rewrite is explicit and never the default.
