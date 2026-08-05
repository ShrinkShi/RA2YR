# Basic and Map metadata

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. No source code was imported. `code_imported: false`.

## `[Basic]` boundary

`[Basic]` is a mixed metadata section. Raw fields can describe display text, campaign media/progression, authored Player-House candidates, carry-over candidates, version/format markers, multiplayer hints, editor/client data and extensions. Field names do not by themselves establish runtime semantics.

Keep raw occurrences, casing, values, duplicates and provenance. Display metadata, campaign invocation, lobby/session values and runtime state remain separate.

## `[Map]` boundary

Leading raw inputs include `Size`, `LocalSize` and `Theater`, plus unknown/profile-specific metadata. The parser retains all four rectangle tokens and the exact Theater token before interpretation.

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes width/height, LocalSize and Theater editor controls | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior/UI only. | Named editor profile; UI limits are not runtime limits. | `NotRun` |
| WAE reads/writes Basic/Map fields and uses its own defaults | `ImplementationSpecificBehavior` | WAE | Named editor behavior. | Preserve raw fields instead of defaults. | `NotRun` |
| Common Basic/Map field names and meanings | `ConfirmedCommunityConvention` | ModEnc/community docs | Stable authoring convention, not complete runtime proof. | Provenance and product applicability retained. | `NotRun` |
| Exact runtime meaning/precedence of inherited, campaign, client and extension fields | `Unresolved` | No original-runtime source located | No reliable universal contract. | Expose typed candidates only. | `NotRun` |
| Tool/client interpretations of Player, GameMode, Min/MaxPlayer and related fields | `ConflictingSources` | Editors, clients and community | Authored-map and launch-context meanings differ. | Keep multiple evidence candidates. | `NotRun` |
| Lossless raw metadata, no defaulting, no mode/player inference | `DefensiveDesign` | Project policy | Preservation/layering. | Future adapters resolve with explicit context. | `NotRun` |

## Special boundaries

- `Player` is an authored candidate, not the current machine's multiplayer assignment.
- `MinPlayer`/`MaxPlayer`, `GameMode(s)` and `MultiplayerOnly` are evidence, not a complete mode classifier.
- campaign media/theme/next-scenario fields are references, not execution.
- carry-over, credits and lobby money are distinct.
- unknown/inherited TS fields remain raw and profile-scoped.
- Digest and Lighting are separate sections and not inferred from Basic/Map.
