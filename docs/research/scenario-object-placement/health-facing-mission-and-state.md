# Health, facing, mission, and state boundaries

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Raw-first state model

Placement state fields are serialized source values, not ready-to-run simulation values. Core first preserves:

- `ScenarioHealthRaw`;
- `ScenarioFacingRaw`;
- `ScenarioMissionRaw`;
- `ScenarioVeterancyRaw`;
- `ScenarioGroupRaw`;
- `ScenarioHighRaw`;
- `ScenarioFollowsRaw`;
- recruitment/autocreate raw flags;
- structure-specific raw booleans and upgrade fields.

Interpretation requires a record-layout profile and, for some values, a bound Rules type.

## Health

ModEnc and MapTool describe placement health as units of `1/256` of the type's total `Strength=`. The common editor range is `0..256`, where 256 represents full health.

This is strong community documentation and cross-tool convergence, with implementation independence unproven. The corresponding formal grade is `Underconfirmed`. It is not complete official runtime source evidence, and it does not justify changing source values during parsing.

Keep separate:

```text
HealthRaw
HealthScaleProfileCandidate
HealthRatioCandidate
BoundRulesStrength
DerivedCurrentHitPointsCandidate
DestroyedStateCandidate
DamageVisualCandidate
```

The parser must not calculate final hit points before type binding. Even after binding, integer rounding and runtime behavior require an explicit policy.

### Tool behavior

WAE and MapTool clamp health into `0..256` and use a default when numeric parsing fails. This is editor/tool recovery. Strict Core reports:

- invalid numeric text;
- negative candidate;
- above-profile range;
- overflow;
- unresolved scale.

It preserves the raw token and does not clamp, replace with 256, or mark partial data as fully valid.

## Facing

Community documentation describes a 256-based facing domain with cardinal/diagonal examples in multiples of 32. Public editor models commonly store an integer or byte.

Keep separate:

- `BodyFacingRaw`;
- `TurretFacing` if another format supplies it;
- movement direction;
- target direction;
- sprite frame;
- voxel rotation;
- rendered angle;
- Unity rotation.

No placement parser performs modulo arithmetic, degree conversion, or Unity quaternion creation.

Suggested profiles:

- `Facing256ClockwiseGameNorth`;
- `Facing8DirectionDisplay`;
- `Facing32DirectionCandidate`;
- `UnknownFacingEncoding`.

A value outside a configured profile is diagnosed, not normalized.

## Structure facing

Structure facing uses the same raw field position as other techno types in the common layout, but the type may have discrete foundations, facings, turret behavior, or Art frames. Those semantics belong to later Rules/Art/simulation layers.

The parser does not decide that a building facing is irrelevant merely because a specific building appears symmetrical.

## Mission

The mobile placement layouts serialize a mission token as text. Public tools recognize names such as Guard, Area Guard, Sleep, Hunt, Move, Attack, Construction, or Selling, but the complete runtime mission set and type-specific validity are outside this dossier.

Raw model:

```text
ScenarioMissionRaw
- TextRaw
- NormalizedSpacingCandidate
- RecognizedTokenCandidate
- Profile
- EvidenceGrade
```

MapTool converts spaces to underscores for enum parsing and converts them back when writing. That is a tool normalization and can alter exact token identity.

Core must not:

- execute the mission;
- replace an unknown token with Guard;
- infer a mission from object family;
- reject a placement solely because the mission is unknown;
- build an AI behavior tree;
- validate every mission against every type.

## Veterancy

The common Unit, Infantry, and Aircraft layouts contain an integer veterancy/experience token. Public tools store it as an integer, but exact scale and runtime thresholds are not established here.

Keep:

- raw integer/text;
- known editor profile candidate;
- type/runtime interpretation unresolved.

Do not convert directly to a rank enum or veteran/elite flags in the parser.

## Group

The common mobile layouts contain `Group`, frequently with negative sentinel candidates such as `-1`. It must not be confused with:

- TeamType identity;
- TaskForce identity;
- record key;
- source-order index;
- runtime object ID;
- player control group.

The group field remains raw until a dedicated AI/team semantics package establishes its contract.

## High and bridge state

Units and Infantry common layouts contain `High`; Aircraft does not in the common 12-field profile.

`HighRaw` may be related to bridge occupancy, but it is not equivalent to:

- IsoMap Level;
- TMP HeightRaw;
- TMP RampTypeRaw;
- bridge health/state;
- rendered elevation;
- pathfinding layer.

This dossier only preserves and labels the field.

## FollowsIndex

The Unit layout contains `FollowsIndex`. WAE writer derives it from the in-memory Units list index and writes `-1` when no follower is present. This is strong editor behavior.

Unresolved questions include whether runtime resolution uses:

- source occurrence order;
- numeric record key;
- a canonical reordered list;
- another object index.

Core records multiple reference candidates but does not bind one merely because the integer equals a record key.

## Recruitment/autocreate flags

The final fields of common Unit, Infantry, and Aircraft layouts are documented as recruitment flags for teams with Autocreate disabled/enabled. They are kept as raw boolean/integer candidates.

Parsing does not:

- form teams;
- recruit objects;
- validate Group compatibility;
- produce replacement units;
- run factories or AI.

## Structure state fields

The 17-field Structure profile includes candidates for:

- AI sellable;
- AI rebuildable;
- powered;
- upgrade count;
- spotlight;
- three upgrade type references;
- AI repairable;
- nominal display state.

Each remains a raw token plus profile interpretation. WAE comments that AI rebuildable is a leftover, but still writes/preserves the position. This is evidence that “unused by this tool” is not permission to remove a field.

Upgrade IDs are opaque BuildingType references until Rules binding. Upgrade count and the number of non-none upgrade tokens can disagree; both are preserved and reported.

## Default and sentinel handling

Public tools use defaults such as:

- health 256;
- facing 0 or maximum;
- group `-1`;
- tag `None`/`<none>`;
- follows `-1`;
- boolean defaults.

Core records recognized sentinel candidates but never replaces missing or invalid source fields with editor defaults while claiming source success.

## Typed descriptor boundary

A typed descriptor may expose derived candidates only after raw preservation:

```text
ScenarioPlacementStateInterpretation
- HealthCandidate
- FacingCandidate
- MissionCandidate
- VeterancyCandidate
- GroupCandidate
- HighCandidate
- FollowsCandidate
- RecruitmentCandidates
- EvidenceGrades
- Diagnostics
```

The descriptor still cannot create a live simulation object.
