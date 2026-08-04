# Alliances and diplomacy

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, or mechanically ported. `code_imported: false`.

## Scope

This document models authored alliance metadata as a reference graph. It does not implement diplomacy, hostility, team assignment, Trigger actions, or multiplayer alliance locking.

## Raw alliance source

Leading public candidate:

```ini
[HouseA]
Allies=HouseA,HouseB
```

WAE writes `Allies` as a comma-separated list of House logical names. The official editor exposes an Allies text field and commonly generates self-alliance in its authoring defaults.

Evidence:

- official editor behavior: `ConfirmedByOfficialEditorSource`;
- WAE reader/writer model: `ConfirmedByIndependentImplementation`.

## Directed edge model

Default representation:

```text
ScenarioAllianceEdgeRaw
- SourceHouseIdRaw
- TargetHouseIdRaw
- ListOccurrence
- TokenOccurrence
- RawToken
- ResolutionState
- EvidenceGrade
```

Each token creates one directed raw edge candidate:

```text
HouseA → HouseB
```

The parser does not create:

```text
HouseB → HouseA
```

unless it is explicitly authored.

## Raw delimiter

The leading delimiter is comma. Preserve:

- exact raw text;
- empty entries;
- whitespace;
- duplicate tokens;
- trailing comma;
- source order;
- case;
- repeated `Allies` keys.

A semantic tokenizer may offer trimmed candidates while retaining the raw token.

## Self alliance

```text
HouseA → HouseA
```

Self-alliance is common in editor-created sections and may be meaningful, redundant, or required by a profile.

Core preserves it and records:

```text
IsSelfReference = true
```

It does not delete or synthesize self edges.

## Duplicate ally

```ini
Allies=HouseB,HouseB
```

Both token occurrences remain in the raw list. A graph analysis may report a duplicate directed edge but does not deduplicate the lossless view.

## Missing ally

```ini
Allies=MissingHouse
```

Result:

```text
Raw edge retained
Target resolution = MissingTarget
```

WAE may skip an unresolved allied House in its in-memory model. That is recovery behavior, not Core policy.

## Case collision

If `Alpha` and `ALPHA` exist or a reference uses a different case:

- exact match candidate;
- case-insensitive match candidates;
- collision state;
- selected profile comparison rule.

No automatic case correction is written back.

## Asymmetric alliance

Example:

```text
A → B
B has no A edge
```

Possible interpretations:

- valid one-way authored relationship;
- malformed incomplete symmetric alliance;
- editor output anomaly;
- runtime input accepted as directed;
- runtime later symmetrizes;
- profile-specific behavior.

Project default:

```text
Preserve directed graph
Analyze symmetry separately
```

## Symmetry analysis

```text
ScenarioAlliancePairAnalysis
- AtoBCount
- BtoACount
- IsSymmetricCandidate
- IsSelfPair
- MissingIdentity
- EvidenceGrade
```

Possible classifications:

- symmetric;
- asymmetric;
- duplicate symmetric;
- dangling;
- case-ambiguous;
- unresolved.

No repair occurs.

## `FixedAlliance`

`[SpecialFlags] FixedAlliance` is a separate raw policy candidate. Community documentation associates it with preventing in-game alliance changes in multiplayer.

Important separation:

```text
Authored Allies graph
≠ alliance mutability policy
```

`FixedAlliance=yes` does not instruct the parser to:

- make every authored edge reciprocal;
- merge lobby teams;
- remove missing allies;
- create enemies;
- freeze a runtime diplomacy system.

It only contributes metadata for a future session/simulation policy.

## Enemies and neutrality

Potential sources may describe:

- enemy lists;
- neutral relations;
- default hostility;
- special/civilian diplomacy;
- team numbers;
- Trigger-driven changes.

No universal inverse relation is derived from absence in `Allies`.

A missing alliance edge does not itself mean explicit enemy.

## Neutral and Special

Neutral, Special, and civilian Houses can have authored alliance tokens, special runtime defaults, or hardcoded relationships.

Core records raw edges and identity resolution. It does not impose hardcoded diplomatic defaults in this dossier.

## Trigger-driven alliance changes

M3-R8 records Trigger Action parameters and reference graph candidates. A future executor may apply alliance-changing actions.

This dossier distinguishes:

```text
Initial authored alliance graph
Runtime diplomacy state
Trigger command stream
```

The parser does not simulate Trigger actions.

## Multiplayer team number

Lobby team number is not an authored House alliance edge.

```text
LobbyTeamNumber
≠ TeamType
≠ House.Allies
≠ Country/Side
```

A future session policy may translate players assigned to the same lobby team into initial runtime alliances. That translation is external to map parsing.

## Cooperative group

A cooperative scenario may provide authored Houses and alliances, while a client may separately group human players.

The mode descriptor records both evidence sources and does not conflate them.

## TeamType distinction

`TeamType` is an AI/unit-team template studied in M3-R8. It is unrelated to multiplayer lobby teams or House diplomacy despite sharing the word “team.”

## Country and Side distinction

Two Houses using the same Country or Side are not automatically allies.

No alliance is inferred from:

- same Country;
- same Side;
- same color;
- adjacent start positions;
- same owner type;
- same player team number unless a session policy explicitly maps it.

## Potential bitmask candidates

Some games or internal runtime systems may use alliance bitmasks. The map-facing leading evidence is a list of House IDs.

If a source exposes a numeric/bitmask alliance field, it must be modeled as a separate profile rather than reinterpreting a string list automatically.

## Alliance graph result

```text
ScenarioAllianceGraph
- HouseIdentities[]
- DirectedRawEdges[]
- ResolvedEdges[]
- DanglingEdges[]
- DuplicateEdgeGroups[]
- SymmetryAnalysis[]
- FixedAllianceRaw
- Diagnostics[]
```

It is immutable and non-executable.

## Consistency diagnostics

Suggested diagnostics:

- `AllianceEmptyToken`;
- `AllianceDuplicateToken`;
- `AllianceSelfReference`;
- `AllianceMissingTarget`;
- `AllianceCaseCollision`;
- `AllianceDuplicateSourceProperty`;
- `AllianceAsymmetricPair`;
- `AllianceUnknownIdentityDomain`;
- `FixedAllianceInvalidBoolean`;
- `LobbyTeamVsAuthoredAllianceConflict`.

Self-reference and asymmetry may be informational rather than fatal depending on policy.

## Player-control interaction

A local player assignment does not mutate the alliance graph. It only identifies which House/controller pair the local session controls.

## Starting-state interaction

Alliances can be resolved before or after start-slot binding, but do not choose start locations.

## Runtime adapter boundary

A future diplomacy initializer may consume:

```text
ScenarioAllianceGraph
ScenarioSpecialFlagsRaw
SessionPlayerAssignments
GameModeInitializationDescriptor
RuntimeHouseRegistry
```

and produce runtime diplomacy state under an explicit deterministic policy.

The parser never performs this operation.

## Roundtrip

Preserve:

- raw `Allies` value;
- token order;
- whitespace;
- duplicates;
- self-reference;
- asymmetric relations;
- missing targets;
- case differences;
- duplicate keys;
- extension diplomacy fields.

No default writer symmetrizes, deduplicates, sorts, or canonicalizes alliance lists.

## Open evidence boundary

The following remain unresolved without official runtime evidence:

- whether stock runtime reads alliances directionally;
- whether it automatically inserts reverse relationships;
- whether self-alliance is required;
- duplicate-token behavior;
- missing-target behavior;
- Neutral/Special defaults;
- interaction with `FixedAlliance` at load time;
- exact precedence between authored allies, lobby team assignment, and Trigger actions.
