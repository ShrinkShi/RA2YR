# Scenario placement test matrix

> **Source notice:** ChatGPT Web public-source research only. ProjectBaseline was not read. This is not a Codex Agent artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## Matrix contract

This is an implementation design, not test code. Each case is tagged as:

- `Fact` — supported format/editor/tool fact;
- `Conflict` — sources or interpretations differ;
- `Policy` — strict project defense requirement;
- `Unresolved` — requires stronger evidence or future audit.

Total: **140 cases**.

## A. Lossless tokens and record keys — 20

| ID | Case | Basis |
|---|---|---|
| T001 | Section entry with an empty raw value is preserved | Policy |
| T002 | One empty CSV token remains one token | Policy |
| T003 | Consecutive commas preserve the empty middle token | Policy; conflicts with WAE/MapTool removal |
| T004 | Trailing comma preserves a trailing empty token | Policy |
| T005 | Leading comma preserves the first empty token | Policy |
| T006 | Leading and trailing token whitespace remains available beside normalized candidates | Policy |
| T007 | Spaces inside owner, mission, Tag, or extension text are not collapsed in raw data | Policy |
| T008 | INI inline comment boundary is supplied by lossless INI, not re-parsed by CSV tokenizer | Policy |
| T009 | Quote characters are preserved and diagnosed under the default non-quoted profile | Unresolved; Policy |
| T010 | Field count below selected profile minimum produces a structured mismatch | Policy |
| T011 | Exact canonical field count produces a layout match without default insertion | Policy |
| T012 | Extra fields are retained as an extension tail | Policy |
| T013 | Invalid integer text remains raw and does not become zero | Policy |
| T014 | Signed integer overflow is detected with checked parsing | Policy |
| T015 | Unsigned/narrowing overflow is detected before conversion | Policy |
| T016 | Duplicate byte-identical raw keys form a duplicate group | Policy |
| T017 | Numeric key gaps do not trigger renumbering | Fact/Policy |
| T018 | Raw keys `1` and `01` remain distinct but form a normalized collision | Policy |
| T019 | Nonnumeric key remains raw; numeric-profile interpretation fails without deletion | Policy |
| T020 | Duplicate section occurrences remain separate inputs | Policy |

## B. Structure, Unit, Infantry, and Aircraft layouts — 32

| ID | Case | Basis |
|---|---|---|
| T021 | Structure record with 17 nonempty canonical fields maps indices without reordering | Fact |
| T022 | Structure record with 16 fields reports missing field 16 | Policy |
| T023 | Structure record with an 18th token preserves extension tail | Policy |
| T024 | Empty Upgrade2 token does not shift Upgrade3 or later booleans | Policy; tool conflict |
| T025 | UpgradeCount disagrees with non-none upgrade tokens; both raw values remain | Conflict/Policy |
| T026 | Invalid structure boolean remains raw and diagnostic | Policy |
| T027 | Two structure records at one cell remain preserved for conflict analysis | Policy |
| T028 | Unit record with 14 canonical fields maps the common RA2/YR profile | Fact |
| T029 | Unit record with 13 fields does not receive an editor-default final flag | Policy |
| T030 | Unit record with extra fields preserves an extension tail | Policy |
| T031 | Unit HighRaw zero and nonzero remain distinct raw states | Fact/Unresolved semantics |
| T032 | Unit FollowsRaw `-1` is retained as sentinel candidate | Fact |
| T033 | Follows numeric value matching both raw key and source index is ambiguous | Conflict/Policy |
| T034 | Unit recruitment flag pair remains two separate raw fields | Fact |
| T035 | Infantry record with 14 fields maps SubCell at index 5, Mission at 6, Facing at 7 | Fact |
| T036 | Infantry record with 13 fields does not receive a default recruitment flag | Policy |
| T037 | Infantry SubCellRaw zero remains raw | Fact |
| T038 | Infantry nonzero SubCellRaw is distinct from TMP/IsoMap subtile | Policy |
| T039 | Invalid or out-of-profile Infantry SubCell is not clamped | Policy/Unresolved range |
| T040 | Two infantry in one cell with different subcells are preserved as cooccupancy candidate | Fact/Policy |
| T041 | Two infantry in one cell with the same subcell form a conflict candidate | Policy |
| T042 | Infantry Mission/Facing positions are not parsed using Unit indices | Policy |
| T043 | Aircraft record with 12 fields maps the common profile | Fact |
| T044 | Aircraft record with 11 fields does not receive default recruitment state | Policy |
| T045 | Aircraft extra fields remain an extension tail | Policy |
| T046 | Aircraft common profile has no Unit High/Follows positions | Fact |
| T047 | Aircraft mission remains raw even if tool enum does not recognize it | Policy |
| T048 | Structure text is not accepted through Unit profile merely because first fields resemble it | Policy |
| T049 | Unit text is not accepted through Aircraft profile by silently dropping High/Follows | Policy |
| T050 | Infantry text is not accepted through Unit profile | Policy |
| T051 | TS/editor/extension layout requires an explicit non-vanilla profile | Policy/Unresolved |
| T052 | Writer default values are not evidence that missing source fields existed | Policy |

## C. Coordinates and scenario-cell identity — 20

| ID | Case | Basis |
|---|---|---|
| T053 | Decode `Y*1000+X` with ordinary positive X/Y | Fact candidate |
| T054 | Decode `X*1000+Y` only under explicit comparison profile | Conflict/Policy |
| T055 | Combined cell ID zero produces a valid numeric candidate, then separate domain result | Policy |
| T056 | Negative combined cell ID remains raw and fails default coordinate profile | Policy/Unresolved runtime |
| T057 | X component greater than 999 is rejected as ambiguous under radix-1000 profile | Policy |
| T058 | Large Y candidate is checked against integer and map budgets | Policy |
| T059 | `Y*1000+X` multiplication/addition overflow is detected | Policy |
| T060 | Direct techno X/Y negative values remain raw and produce domain diagnostics | Policy |
| T061 | Direct techno X/Y narrowing overflow is rejected without clamp | Policy |
| T062 | Terrain key is interpreted as a cell only under Terrain profile | Fact |
| T063 | Waypoint value, not key, is interpreted as a cell | Fact |
| T064 | CellTag key, not value, is interpreted as a cell | Fact |
| T065 | Smudge X/Y are read from value tokens rather than key | Fact |
| T066 | Coordinate inside Size receives `WithinMapSizeCandidate` | Policy |
| T067 | Coordinate outside LocalSize but inside Size remains preserved | Policy |
| T068 | Coordinate with no matching IsoMap cell remains parsed and diagnosed | Policy |
| T069 | Both axis profiles yielding plausible cells produce ambiguity, not automatic selection | Policy |
| T070 | Overlay `X+512Y` storage formula is not reused as scenario-cell identity | Policy |
| T071 | TMP/IsoMap subtile fields are not used as Infantry SubCell | Policy |
| T072 | Core coordinate result contains no Unity Vector type | Architecture policy |

## D. Owner and Rules type binding — 20

| ID | Case | Basis |
|---|---|---|
| T073 | Owner uniquely matches a declared map house | Policy |
| T074 | Owner matches a composed global house/country candidate | Policy |
| T075 | Neutral spelling is retained and classified only under explicit special-house profile | Community/Policy |
| T076 | Special spelling is retained and classified only under explicit special-house profile | Community/Policy |
| T077 | Unknown owner remains unresolved and is not changed to Neutral | Policy |
| T078 | Duplicate house identities produce ambiguous owner binding | Policy |
| T079 | Case-insensitive candidate matching retains original spelling and provenance | Policy |
| T080 | Missing map house section does not fabricate a runtime player | Policy |
| T081 | Structure TypeRaw binds through BuildingTypes | Fact/Policy |
| T082 | Unit TypeRaw binds through VehicleTypes | Fact/Policy |
| T083 | Infantry TypeRaw binds through InfantryTypes | Fact/Policy |
| T084 | Aircraft TypeRaw binds through AircraftTypes | Fact/Policy |
| T085 | Terrain TypeRaw binds through TerrainTypes | Fact/Policy |
| T086 | Smudge TypeRaw binds through SmudgeTypes | Fact/Policy |
| T087 | Registry gap is retained and later ordinals do not shift | Policy |
| T088 | Duplicate registry ordinal forms an ambiguity group | Policy |
| T089 | Duplicate logical type name retains winner and suppressed provenance | Policy |
| T090 | Registered type with missing typed section is distinct from unknown registry name | Policy |
| T091 | Missing Art or visual asset does not invalidate Rules type binding | Policy |
| T092 | Map-local or extension type is enabled only by explicit composition/extension policy | Policy |

## E. Health, facing, mission, and state — 18

| ID | Case | Basis |
|---|---|---|
| T093 | HealthRaw `0` remains raw and receives 1/256-scale candidate | Community/Policy |
| T094 | HealthRaw `256` remains raw as full-health candidate | Community/Policy |
| T095 | Negative health is not clamped to zero | Policy; tool conflict |
| T096 | Health `257` is not clamped to 256 | Policy; tool conflict |
| T097 | Invalid health text does not become 256 | Policy |
| T098 | Derived HP is not calculated until Rules Strength and scale policy are available | Policy |
| T099 | FacingRaw `0` remains raw under 256-facing candidate | Community |
| T100 | FacingRaw `255` remains raw | Policy |
| T101 | FacingRaw `256` is not silently modulo-reduced | Policy/Unresolved runtime |
| T102 | Recognized mission token produces a candidate without execution | Policy |
| T103 | Unknown mission token remains raw and is not replaced with Guard | Policy |
| T104 | Empty mission token remains empty and does not shift later fields | Policy |
| T105 | Veterancy remains a raw integer candidate, not immediate rank enum | Policy |
| T106 | Group `-1` remains a sentinel candidate, not TeamType ID | Policy |
| T107 | HighRaw remains separate from IsoMap Level and bridge simulation | Policy |
| T108 | FollowsRaw is resolved only under explicit identity-basis policy | Policy |
| T109 | Recruitment/autocreate flags do not form or execute teams | Architecture policy |
| T110 | Structure state/upgrade inconsistencies are reported without repair | Policy |

## F. Terrain, Smudge, Waypoint, CellTag, and references — 18

| ID | Case | Basis |
|---|---|---|
| T111 | Terrain `cell=type` record preserves key and logical type | Fact |
| T112 | Unknown TerrainType remains a preserved unresolved placement | Policy; tool conflict |
| T113 | Duplicate Terrain cell entries remain an ambiguity group | Policy |
| T114 | Smudge `type,x,y,0` four-field candidate is recognized | Fact candidate |
| T115 | Smudge missing field is not filled with zero | Policy |
| T116 | Source smudge remains distinct from runtime-created damage smudge | Policy |
| T117 | Waypoint numeric key and cell value are preserved independently | Fact |
| T118 | Duplicate waypoint ID remains ambiguous and is not renumbered | Policy |
| T119 | Waypoint with invalid/out-of-domain cell remains preserved | Policy |
| T120 | CellTag cell key and Tag value form an opaque edge | Fact/Policy |
| T121 | CellTag with missing Tag remains dangling | Policy |
| T122 | Duplicate normalized CellTag coordinate retains all targets | Policy |
| T123 | Techno Tag with missing target remains raw and dangling | Policy |
| T124 | Duplicate Tag ID produces ambiguous resolution | Policy |
| T125 | Tag resolving to a missing Trigger produces a second-stage dangling edge | Policy |
| T126 | Circular Tag/Trigger graph is represented but not executed | Policy |
| T127 | Multiple Infantry in one cell are not automatically duplicate objects | Policy |
| T128 | Conflicting Structures or incompatible placements are analyzed without last-wins deletion | Policy |

## G. Safety, inputs, architecture, and audit — 12

| ID | Case | Basis |
|---|---|---|
| T129 | Memory input produces canonical raw records and diagnostics | Policy |
| T130 | Seekable Stream input equals Memory result | Policy |
| T131 | Arbitrary short-read Stream input equals Memory result | Policy |
| T132 | MIX bounded entry window equals Memory result and cannot read outside window | Policy |
| T133 | Per-record token budget fails before unbounded allocation | Policy |
| T134 | Per-section and aggregate record budgets fail structurally | Policy |
| T135 | Raw key/value and total token-character budgets are enforced | Policy |
| T136 | Diagnostic budget prevents input-driven diagnostic explosion | Policy |
| T137 | Malformed delimiters cannot produce a no-progress loop | Policy |
| T138 | Coordinate/index arithmetic uses checked operations | Policy |
| T139 | Synthetic fixtures do not reuse production token, coordinate, or binding formulas | Policy |
| T140 | Core creates no Unity object; sanitized audit emits aggregates only | Architecture/Audit policy |

## Required assertions across all cases

- raw text remains available after failure;
- no clamp, default insertion, token deletion, renumbering, or last-wins repair occurs silently;
- partial semantic interpretation is not reported as complete source success;
- unknown extension tails survive;
- diagnostics contain family, source occurrence, field/key location, policy, and evidence grade;
- test data is synthetic and independent from production formulas;
- no ProjectBaseline or original map content is embedded in fixtures.
