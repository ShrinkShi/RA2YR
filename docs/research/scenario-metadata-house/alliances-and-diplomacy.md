# Alliances and diplomacy

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. No GPL or unclear-license code was imported. `code_imported: false`.

## Scope

Authored alliance metadata is a raw reference graph. This document does not implement diplomacy, hostility, lobby teams, Trigger actions or runtime alliance locking.

## Raw model

```ini
[HouseA]
Allies=HouseA,HouseB
```

Each token creates a directed candidate edge:

```text
HouseA → HouseB
```

Core preserves exact text, empties, whitespace, duplicate tokens, trailing delimiters, order, case and duplicate `Allies` keys. It does not synthesize `HouseB → HouseA`, remove self edges, deduplicate, correct case or drop missing targets.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes and authors Allies text/defaults | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior only. | Preserve an editor-compatible profile. | `NotRun` |
| WAE reads/writes comma-separated House-name lists | `ImplementationSpecificBehavior` | World-Altering Editor | Named editor behavior. | Retain raw ordered tokens. | `NotRun` |
| Directed ordered House-reference list is the leading authored model | `Underconfirmed` | Official editor, WAE and community docs | Runtime symmetry and malformed handling are unsourced. | Store directed raw edges first. | `NotRun` |
| Runtime alliance symmetry, self-edge requirements and missing-target behavior | `Unresolved` | No original-runtime source located | No reliable unique contract. | Future diplomacy/session policy. | `NotRun` |
| Sources/layers implying symmetric teams versus directed authored edges | `ConflictingSources` | Map, lobby/client and gameplay terminology | Lobby team number is not authored Allies. | Keep graph and session teams separate. | `NotRun` |
| No reverse-edge synthesis, no repair and duplicate/case preservation | `DefensiveDesign` | Project policy | Preservation/fail-closed design. | Return diagnostics and candidates. | `NotRun` |

## FixedAlliance boundary

`FixedAlliance` is separate raw SpecialFlags metadata. It does not repair, symmetrize or validate the authored graph during parsing. A future session adapter may apply a named policy using the raw graph plus lobby/game-mode context.

## Identity boundaries

House instance, Country/HouseType, Side, lobby team, TeamType, local player and network peer are distinct. Similar names or numbers do not create alliance edges.

## Diagnostics

Suggested statuses include self edge, duplicate edge, missing target, case collision, duplicate source key, asymmetric pair, ambiguous target and unresolved sentinel. None mutates source data.

## Runtime boundary

The parser does not answer whether alliances are reciprocal in combat, shared vision/resources, victory grouping, AI cooperation or multiplayer locking. Those remain runtime/session questions.
