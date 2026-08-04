# Future ProjectBaseline sanitized audit request

> **Source notice:** ChatGPT Web public-source research. No local `ProjectBaseline` access. Not a Codex artifact. No GPL or unclear-license code was copied, translated, mechanically rewritten, or ported. `code_imported: false`.

This document designs a future read-only Codex audit. It does not authorize this research branch to access `ProjectBaseline`, run games/editors, or publish scenario content.

## 1. Objective

Collect non-reconstructable aggregate evidence about:

- section presence and record shapes;
- Trigger, Tag, Event, Action, TeamType, TaskForce, ScriptType, and AITrigger layout candidates;
- count/tuple consistency;
- opcode ranges and unknown-opcode frequency;
- identity duplicate, case-collision, dangling, and ambiguity rates;
- global/local definition composition;
- variable and difficulty-field presence;
- extension-tail incidence;
- input-mode equivalence.

Audit observations receive only:

```text
ObservedByFutureProjectBaselineAudit
```

They do not become original-runtime confirmation.

## 2. Execution owner and isolation

A future local Codex task may:

- read ProjectBaseline in a controlled environment;
- use read-only tooling;
- run the same parser through Memory, Stream, short-read Stream, and bounded MIX-window adapters;
- emit only the allowlisted aggregate report.

It must not:

- commit source map content;
- modify maps;
- run scenario execution;
- create units or teams;
- load scenario assets for display;
- publish per-map graph data;
- send original records to this research branch.

## 3. Sample selection

Use selection criteria rather than public map identities.

Required categories:

- campaign and skirmish/multiplayer map classes;
- six theater categories where available;
- small, medium, and large map-size bands;
- Trigger-heavy candidates;
- TeamType/TaskForce/Script-heavy candidates;
- AITrigger-heavy candidates;
- local-variable candidates;
- CellTag-heavy candidates;
- object-Tag candidates;
- maps with no scripting sections;
- extension-field/opcode candidates;
- duplicate-identity candidates;
- dangling-reference candidates;
- unknown-opcode candidates;
- different source/provenance categories.

Selection must not publish map names, filenames, titles, or IDs.

## 4. Minimum category diversity

The audit should target enough files to cover, where present:

- Trigger records with seven, eight, and extra fields;
- Tags with common and unusual repeat values;
- Events using different declared counts;
- Actions using different declared counts;
- Event opcode base and higher ranges;
- Action opcode base and higher ranges;
- nonzero values in catalog-unused parameter slots;
- TeamTypes with/without Tags, transport waypoints, and extension flags;
- TaskForces with sparse/gapped entries and maximum common entry count;
- Scripts with gaps, unknown actions, and high extension ranges;
- AITriggers with one/two TeamTypes, comparator variation, and weight variation;
- difficulty flag combinations;
- local/global variable references;
- map-local and global identity collisions.

## 5. Processing pipeline

```text
ProjectBaseline source
→ bounded file selection
→ lossless INI parse
→ section occurrence collection
→ raw graph-family parse
→ explicit selected profiles
→ identity/reference analysis
→ aggregate-only sanitizer
→ report schema validation
```

No executor stage is present.

## 6. Input-mode equality

For every selected logical sample, compare:

- complete memory input;
- normal Stream;
- deterministic short-read Stream;
- bounded MIX entry window when the source is archive-backed.

Allowed equality outputs:

- same structural status;
- same record counts;
- same field-count histograms;
- same aggregate identity/reference classifications;
- same diagnostic-code multiset;
- same non-reversible canonical aggregate hash.

Do not output raw graph hashes that can be linked to a known map.

## 7. Allowed public output

### 7.1 Selection and provenance

- `SelectionBasis` category;
- broad map class: campaign/skirmish/other;
- theater category;
- source category: official/official-addition/user-addition/unknown category, if known without naming;
- size band;
- sample count per category.

### 7.2 Section presence

Aggregate counts for presence/absence of:

- Triggers;
- Events;
- Actions;
- Tags;
- CellTags;
- TeamTypes;
- TaskForces;
- ScriptTypes;
- AITriggerTypes;
- AITriggerTypesEnable;
- VariableNames/local/global variable candidates.

### 7.3 Record shapes

- total record counts per family;
- minimum/maximum/coarse histogram of field counts;
- empty-token count;
- trailing-empty count;
- unknown-tail count;
- duplicate-section count;
- duplicate-key count;
- nonnumeric-list-key count;
- normalized-key collision count.

### 7.4 Trigger and Tag aggregates

- Trigger common-field-count classification;
- nonempty linked-Trigger candidate count;
- disabled/easy/normal/hard raw classification counts;
- Trigger tail zero/nonzero/empty categories;
- Tag common-field-count classification;
- repeat-value coarse frequency categories;
- Trigger/Tag duplicate, dangling, case-collision, and cycle counts.

No Trigger/Tag IDs or names.

### 7.5 Event and Action aggregates

- declared Event-count histogram;
- parsed Event-tuple-count histogram;
- Event count-mismatch categories;
- Event opcode numeric minimum/maximum and coarse bins;
- unknown/negative/overflow Event opcode counts;
- Event parameter-slot nonzero/empty counts by anonymous slot number;
- declared Action-count histogram;
- parsed Action-tuple-count histogram;
- Action count-mismatch categories;
- Action opcode numeric minimum/maximum and coarse bins;
- unknown/negative/overflow Action opcode counts;
- Action parameter-slot nonzero/empty counts by anonymous slot number;
- count of nonnumeric final Action-slot values;
- extension-profile-required count.

Do not publish per-map or ordered opcode sequences.

### 7.6 Identity and reference aggregates

- identity count by anonymous kind;
- duplicate identity-group count;
- case-collision count;
- uniquely resolved edge count;
- sentinel edge count;
- missing target count;
- ambiguous target-kind count;
- duplicate-target count;
- cycle count and coarse cycle-size histogram;
- orphan Events/Actions aggregate count;
- Tag→Trigger, placement→Tag, CellTag→Tag, TeamType-component, and AITrigger→TeamType edge category counts.

Do not publish graph topology.

### 7.7 TeamType aggregates

- TeamType count;
- listed-with-section / listed-without-section / unlisted-section counts;
- global/local/collision categories;
- known/unknown property counts;
- TaskForce/Script/Tag/House/Waypoint binding success categories;
- boolean spelling categories;
- extension-tail/unknown-flag counts.

No TeamType IDs, names, flags list, or references.

### 7.8 TaskForce and Script aggregates

- TaskForce count;
- entry-count histogram;
- key-gap/duplicate/out-of-profile count;
- count value range and invalid/overflow categories;
- Rules type binding success/failure category counts;
- ScriptType count;
- step-count histogram;
- key-gap/duplicate count;
- Script action numeric range and coarse bins;
- unknown/high-extension action counts;
- negative argument count;
- candidate jump-cycle aggregate count.

Do not publish team composition or ordered Script steps.

### 7.9 AITrigger aggregates

- AITrigger count;
- field-count histogram;
- primary/secondary Team binding status counts;
- owner/condition-object binding categories;
- comparator length/parse categories;
- initial/minimum/maximum weight coarse numeric ranges;
- invalid/nonfinite/ordering-anomaly counts;
- side and difficulty flag coarse categories;
- AITriggerTypesEnable resolved/dangling/invalid counts;
- extension-tail count.

No AITrigger IDs, comparator strings, exact weights, Team IDs, or condition objects.

### 7.10 Variables and difficulty

- local/global/unknown variable-reference counts;
- resolved/dangling variable-reference counts;
- duplicate variable-ID count;
- operation-category counts without opcode sequence;
- easy/normal/hard combination aggregate counts;
- invalid boolean spelling count.

### 7.11 Diagnostics and hashes

- diagnostic-code counts;
- severity counts;
- parser-profile IDs;
- evidence-grade counts;
- non-reversible aggregate hashes scoped to the entire anonymized category;
- Memory/Stream/MIX equivalence status.

Hashes must not be per-map, per-record, per-ID, or linkable to known content.

## 8. Forbidden public output

Never publish:

- map filename, title, scenario name, or path;
- archive filename when identifying;
- username, machine name, or absolute path;
- INI section bodies;
- raw record values;
- raw tokens or key spelling;
- Trigger, Tag, TeamType, TaskForce, ScriptType, AITrigger, House, Waypoint, variable, object, or type IDs;
- display names or text strings;
- exact comparator blobs;
- exact weight triples;
- exact variable names or values;
- opcode sequence for a map;
- parameter tuples;
- ordered Events, Actions, TaskForce entries, or Script steps;
- graph adjacency lists, topology, or paths;
- object/cell coordinates;
- team composition;
- type-to-Art/SHP/VXL/HVA mapping;
- compressed, decoded, or file bytes;
- Base64 or hex;
- screenshots, images, previews, or rendered maps;
- per-record, per-ID, per-section, or per-map hashes;
- any combination sufficient to reconstruct scenario logic or identify a map.

## 9. Anti-reconstruction rules

- use coarse bins rather than exact rare values;
- suppress categories with too few samples;
- combine rare opcodes into anonymous high/unknown bins;
- never pair exact counts across many dimensions for a single sample;
- do not emit row-per-map reports;
- do not publish timestamps or source ordering;
- do not include example records;
- do not include a complete opcode histogram for one map;
- do not retain reversible pseudonyms.

## 10. Audit profiles to compare

Only explicit profiles may be compared:

- TS vanilla candidate;
- RA2/YR vanilla candidate;
- official-editor comparison profile;
- WAE configured profile;
- Ares extension profile where selection basis confirms it;
- Phobos extension profile where selection basis confirms it;
- exact-case versus case-folded identity analysis;
- strict count contract versus observation-only raw classification.

Do not attempt every profile and report the one that yields the fewest errors.

## 11. Variant voting prohibition

A sample that can be interpreted under multiple profiles does not vote for one. Report:

- unique profile classification count;
- ambiguous profile count;
- no-profile count;
- profile conflict categories.

No automatic compatibility promotion.

## 12. Audit diagnostics

Required sanitized diagnostic families:

- missing section;
- duplicate section/key/identity;
- field-count mismatch;
- empty-token and tail-field categories;
- invalid count;
- tuple truncation/extra tokens;
- unknown/extension opcode;
- invalid boolean;
- numeric overflow;
- dangling/ambiguous reference;
- graph cycle;
- Team component missing;
- AI comparator/weight issue;
- budget/no-progress/input-mode mismatch.

Diagnostics must use codes and counts, not raw values.

## 13. Security and resource limits

The audit tool must set:

- maximum files;
- maximum input bytes per file;
- maximum sections and records;
- maximum tokens and token characters;
- maximum declared Events/Actions;
- maximum graph nodes/edges;
- maximum diagnostics;
- execution timeout;
- cancellation support;
- no network access;
- read-only filesystem access where possible.

## 14. Required audit declaration

Every report must state:

- no map content was published;
- no scenario execution occurred;
- no Unity or game process was run;
- no maps were modified;
- no IDs, records, sequences, topology, or coordinates were emitted;
- observations are `ObservedByFutureProjectBaselineAudit` only;
- no compatibility status changed.

## 15. Acceptance criteria

The audit is acceptable only when:

- every output field is on the allowlist;
- automated checks reject forbidden keys and suspicious free text;
- no row identifies a single map;
- all categories meet minimum aggregation thresholds;
- input-mode results match;
- raw bytes and strings are absent from artifacts and logs;
- the report can be reviewed without access to ProjectBaseline;
- all generated temporary content is deleted according to the local task policy.

## 16. Suggested output schema

High-level, non-normative schema:

```text
AuditReport
- MethodVersion
- SelectedProfiles
- SelectionCategoryAggregates
- SectionPresenceAggregates
- ShapeAggregates
- OpcodeBinAggregates
- ReferenceStatusAggregates
- TeamAiAggregates
- VariableDifficultyAggregates
- DiagnosticCounts
- InputModeEquivalence
- DisclosureChecks
```

No field accepts arbitrary source text.

## 17. Non-goals

The audit does not:

- determine exact runtime opcode semantics;
- prove malformed-record acceptance;
- execute Triggers, Teams, Scripts, or AI;
- compare rendered outcomes;
- validate campaign progression;
- publish a complete opcode catalog;
- create fixtures from original records;
- update compatibility matrices or ADRs.
