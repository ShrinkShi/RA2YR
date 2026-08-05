# M3-R14 — Combat damage and targeting research dossier

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex artifact. Public implementations are reference-only; no combat, projectile, damage, AI, renderer or test code was copied or ported. `code_imported: false`.

## Boundary

```text
raw Rules/Art and type references
→ weapon/projectile/warhead raw records
→ explicit product and extension profiles
→ logical combat reference graph
→ targeting and impact candidates
→ deterministic damage-command/result contracts
→ future simulation and presentation adapters
```

Parsing never fires, tracks, collides, damages, applies status, consumes ammo, schedules bursts, kills actors or creates Unity objects.

## Non-collapse rules

Weapon definition, mount, selected slot and runtime weapon instance are distinct. Projectile definition, logical flight state, collision query and rendered projectile are distinct. Warhead, Armor identity, Verses multiplier, targeting permissions, applied status and health mutation are distinct. Damage expression is not simulation mutation. Presentation references never define collision or legality.

## Formal grades

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

No complete original RA2/YR combat runtime source was found, and no claim has proven independent implementation lineages sufficient for `ConfirmedByMultipleIndependentImplementations`. FinalSun/FinalAlert behavior is `ConfirmedByOfficialToolSource`; named engines/tools/extensions are `ImplementationSpecificBehavior`; stable community conventions are `ConfirmedCommunityConvention`; cross-tool candidates remain `Underconfirmed`; direct model conflicts use `ConflictingSources`; complete runtime algorithms remain `Unresolved`.

Raw preservation, explicit profiles, checked arithmetic, stable shot/impact identities, no field fallback, no visual/Unity inference and no simulation during parsing are `DefensiveDesign`.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

These fields do not imply ProjectBaseline access and cannot promote compatibility or runtime evidence.

## Normalized claims

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes combat field catalogs and editor validation | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior only. | Named editor profile; do not inherit repairs. | `NotRun` |
| OpenRA, WAE, Chrono Divide and extensions implement combat models | `ImplementationSpecificBehavior` | Named implementations | Target/profile-specific. | Keep models separate. | `NotRun` |
| Common Weapon/Projectile/Warhead/Armor/Verses fields | `ConfirmedCommunityConvention` | ModEnc/PPM/RA2 DIY | Stable authoring convention, not runtime execution proof. | Preserve product/profile provenance. | `NotRun` |
| Eleven-armor ordering, 256-facing, CellSpread and common damage candidates | `Underconfirmed` | Tools/community | Runtime strictness and lineage independence are unproven. | Explicit profiles. | `NotRun` |
| Verses side effects, projectile algorithms, rounding, AoE and friendly-fire rules | `ConflictingSources` | Tools/engines/extensions/community | Public models differ directly. | Preserve alternatives and raw parameters. | `NotRun` |
| Exact runtime targeting, flight, collision, damage order, status and death behavior | `Unresolved` | No original-runtime source located | No complete contract. | Future deterministic simulation adapter. | `NotRun` |
| No fallback/repair/execution and deterministic contracts | `DefensiveDesign` | Project policy | Preservation and architecture. | Fail closed. | `NotRun` |

## Non-goals

No combat parser implementation, projectile movement, Unity physics, targeting, damage, status/death, ammo/reload state machine, AI, animation/audio, game/editor execution, ProjectBaseline audit or compatibility promotion is included.
