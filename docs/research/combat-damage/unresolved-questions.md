# Unresolved combat questions

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## P0

Unresolved before runtime-equivalence claims: exact weapon slot/fallback and elite behavior; Damage/ROF/Range/MinimumRange/Burst/Ammo units and defaults; projectile algorithms, integration, collision, bridge/elevation and line-of-fire; stock Armor order/applicability; Verses token count, percentage representation, missing/extra behavior and side-effect semantics; CellSpread geometry/distance/falloff/building enumeration; AffectsAllies owner/self/special-effect scope; damage modifier order, rounding, minimum/negative/healing and overflow; target acquisition/retaliation/force-fire; ammo consumption/reload/aircraft behavior; status stacking/immunity/removal/savegame; death weapon/animation/debris/economy sequencing; deterministic RNG and multiplayer/replay state.

## P1/P2

Further research: terrain/projectile interactions, shields/extension Armor, temporal/mind-control/radiation/EMP profiles, gattling/charge/deploy-to-fire, scatter/burst retargeting, projectile trails and presentation, AI weapon selection, save/load serialization, client visual/audio behavior and extension version gates.

## Resolution discipline

- actual original runtime/source may support `ConfirmedByOriginalRuntimeSource`;
- FinalAlert/FinalSun only supports `ConfirmedByOfficialToolSource`;
- named engines/tools/extensions are `ImplementationSpecificBehavior`;
- stable community conventions are `ConfirmedCommunityConvention`;
- multiple implementations require proven independent lineage or remain `Underconfirmed`;
- direct disagreement is `ConflictingSources`;
- project explicit profiles/raw preservation/determinism/no execution are `DefensiveDesign`;
- insufficient evidence remains `Unresolved`.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

Future aggregate observations do not close original-runtime questions or promote compatibility. Screenshots, plausible damage values, one successful map, Unity physics and source-count voting are not valid closure methods.
