# M3-R9 — Scenario metadata, House initialization, alliances, and game-mode research

> **Source and authorship notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a local Codex Agent artifact. GPL and unclear-license sources are reference-only; no code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

## Scope and boundary

This dossier studies `[Basic]`, `[Map]`, `[SpecialFlags]`, `[MultiplayerDialogSettings]`, `[Houses]`, `[Countries]`, per-House/Country sections, alliances, start slots, game-mode evidence, Digest/Lighting boundaries, and map-local composition.

```text
lossless map INI
→ raw metadata sections
→ explicit layout profiles
→ map geometry/theater descriptor
→ House/Country identity graph
→ authored starting-state descriptors
→ alliance/player-slot graph
→ game-mode initialization descriptor
→ future simulation/session adapters
```

Core does not create runtime Houses or players, assign network peers, select starts, generate units, mutate diplomacy/economy, apply lobby values, start campaign/session state, load art, or create Unity objects.

## Leading findings

- `[Basic]` mixes display, campaign, authored-player, multiplayer-hint, version, carry-over, editor/client and extension fields. Raw keys remain separate from runtime/session meaning.
- `[Map] Size` and `LocalSize` preserve four raw tokens. `x,y,width,height` is a leading candidate, not a universal runtime contract.
- Theater is a logical token. Unknown values do not fall back to Temperate; resource loading belongs elsewhere.
- House instance, Country/HouseType, Side, player slot, controller kind, local human, network peer and authored campaign player are distinct identities.
- `[Countries]`/`[Houses]` composition, list gaps, duplicates, missing sections and case collisions remain visible. Missing references are not repaired.
- Credits, carry-over money, lobby money, resource value, runtime credits and score remain distinct sources.
- `[Basic] Player`, House `PlayerControl`, lobby rows, AI rows, observer and network assignment are separate layers.
- `Allies=` is first stored as ordered directed raw House-reference edges. Reverse edges and symmetry are never synthesized.
- Low-numbered Waypoints are start-location candidates only under an explicit consumer profile; the parser never assigns slots or generates MCVs.
- Campaign/skirmish/multiplayer/co-op classification combines explicit caller context with multiple evidence sources; no single field is authoritative.
- SpecialFlags are raw metadata/profile candidates, not implemented simulation.
- Digest is opaque integrity metadata, not a trusted signature or stable scenario identity.
- Lighting is a future environment input, separate from Theater, House Color and Unity lighting.

## Formal evidence grades

Formal `Grade` fields use only:

```text
ConfirmedByOriginalRuntimeSource
ConfirmedByOfficialToolSource
ConfirmedByMultipleIndependentImplementations
ConfirmedCommunityConvention
ImplementationSpecificBehavior
DefensiveDesign
ConflictingSources
Underconfirmed
Unresolved
```

No complete original RA2/YR runtime source was found, so no reviewed claim reaches `ConfirmedByOriginalRuntimeSource`. FinalSun/FinalAlert behavior is `ConfirmedByOfficialToolSource`. WAE, OpenRA, CnCNet, MapTool, CNCMaps and extension/client behavior is recorded separately as `ImplementationSpecificBehavior`. Stable community conventions use `ConfirmedCommunityConvention`; cross-tool candidates without proven lineage/runtime applicability remain `Underconfirmed`; direct rectangle, identity, alliance, player-control or mode conflicts use `ConflictingSources`.

Raw preservation, explicit profiles, no registry renumbering, no default House creation, no alliance symmetrization, no start selection and no lobby/runtime inference are `DefensiveDesign`.

Future ProjectBaseline work is separate:

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

It does not imply that ProjectBaseline was read or observed and cannot automatically become runtime evidence or promote compatibility.

## Normalized claims

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert LocalSize/Theater/House/SpecialFlags editor fields and repairs | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior only. | Named editor profile; do not inherit UI limits/repairs. | `NotRun` |
| WAE Size/LocalSize, Countries/Houses, Allies and metadata behavior | `ImplementationSpecificBehavior` | World-Altering Editor | Named editor implementation. | Preserve source-specific profile. | `NotRun` |
| `x,y,width,height`, common Theater tokens and directed Allies conventions | `Underconfirmed` | Tools and community docs | Runtime applicability and independent lineage are not proven. | Explicit layout/identity policies. | `NotRun` |
| Client lobby/player/color/start/team behavior | `ImplementationSpecificBehavior` | CnCNet and named clients | Session/client behavior is not authored-map runtime state. | Keep outside Core metadata. | `NotRun` |
| Rectangle origins, House/Country precedence, alliance symmetry and mode authority | `ConflictingSources` | Editors, clients, community and extensions | Sources differ by layer and target. | Preserve candidates/provenance. | `NotRun` |
| Exact runtime player assignment, diplomacy, defaults, mode initialization and Digest enforcement | `Unresolved` | No original-runtime source located | No reliable complete contract. | Future session/simulation adapters. | `NotRun` |
| Raw identity graph, directed alliances, no repair and layer separation | `DefensiveDesign` | Project policy | Preservation/architecture decision. | Fail closed. | `NotRun` |

## Non-goals

No metadata parser, House/Country registry, player/session, network/lobby, diplomacy, starting units, campaign progression, economy, SpecialFlags execution, Digest verification, Lighting, Unity, code, test, configuration, compatibility, ADR, ProjectBaseline access, map modification or runtime execution is included.
