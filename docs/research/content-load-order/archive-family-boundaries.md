# Archive family boundaries

## 1. Classification principle

A `.mix` suffix identifies an archive format candidate, not its runtime role. Runtime behavior is determined by a named archive-family descriptor plus mount context.

Each descriptor should record:

- canonical family name;
- RA2, YR, or shared scope;
- root-discovered, explicitly nested, or tool-only status;
- exact-name or sequence-pattern grammar;
- discovery and priority evidence level;
- whether it is a language, cache, local, map, movie, music, theater, or general layer;
- allowed child mount edges;
- diagnostics for names that resemble but do not match the family.

## 2. Family register

| Family | Typical placement | Candidate role | Current evidence |
|---|---|---|---|
| `ra2.mix` | root | RA2 base root and container for known child archives | official installs + community + reimplementations |
| `ra2md.mix` | root | YR expansion base and container for YR child archives | community/official-tool evidence; project policy |
| `expandNN.mix` | root | numbered RA2 patch/mod expansion | community convention; range details conflict |
| `expandmdNN.mix` | root | numbered YR patch/mod expansion | community convention; project policy `01..99` |
| `ecacheNN.mix` / `ecachemdNN.mix` | root or mod root | cache/resource extension family | community convention; stock ordering underconfirmed |
| `elocalNN.mix` / `elocalmdNN.mix` | root or mod root | local/language-adjacent extension family | community convention; stock ordering underconfirmed |
| `language.mix` / `langmd.mix` | root or known parent | language/UI/string resource layer | fixed-name family; Ares can alter precedence |
| `cache.mix` / `cachemd.mix` | known root/child | cached graphics/resource layer | fixed-name explicit mount |
| `local.mix` / `localmd.mix` | known root/child | INIs and local content | fixed-name explicit mount |
| `maps*.mix` / `mapsmd*.mix` | root or known parent | official/campaign/map packs | exact variants, not one universal wildcard |
| `mov*.mix` / `movmd*.mix` | root or known parent | video/movie package | explicit known names or installer role |
| `theme*.mix` / `thememd*.mix` | root or known parent | music/audio package | explicit known names; engine/tool support varies |
| `conquer.mix` | inside base family in common layouts | general art/resource child | explicit named child, not root wildcard |
| `generic.mix` | inside base family in common layouts | generic theater-independent assets | explicit named child |
| theater packages | commonly nested or explicitly mounted | `temperat`, `snow`, `urban`, `isogen`, `iso*`, etc. | fixed catalog depends on game/theater |
| user root MIX | root | mod-supplied package | only participates when a configured provider/family recognizes it |
| arbitrary nested MIX entry | any parent | opaque archive entry by default | not automatically mounted |

## 3. RA2 versus YR

The suffix `md` identifies the YR lineage in many fixed and numbered names, but family participation must be configured explicitly.

For the frozen YR 1.001 project profile:

- `ra2.mix` is the lowest base layer;
- `ra2md.mix` overlays the RA2 base;
- `expandmd01..99` are the configured numbered expansion layers;
- non-`md` `expandNN` is not silently scanned as a YR expansion family;
- `md` and non-`md` cache/local children may both be reachable through explicit parent mount graphs.

YR 1.000 and community tools sometimes describe non-`md` expansion behavior. That is a separate profile and must not leak into the YR 1.001 default.

## 4. Root versus nested

A fixed child filename does not imply root discovery. Examples such as `localmd.mix`, `cachemd.mix`, `conquer.mix`, `generic.mix`, and theater archives are commonly reached because a loader knows the parent/child relationship.

The model distinguishes:

- `RootArchiveCandidate`;
- `ExplicitNestedArchiveCandidate`;
- `OpaqueArchiveEntry`;
- `LooseFileCandidate`;
- `FuturePackageCandidate`.

Nested depth is provenance, not automatic priority.

## 5. Maps, movies, and theme packages

The prefixes `maps`, `mov`, and `theme` do not authorize arbitrary wildcard loading.

A data-driven profile must list:

- accepted exact names or constrained grammars;
- root or parent location;
- optional/required status;
- role scope;
- mount order;
- whether an extension engine or installer added the name.

Unknown variants remain `UnclassifiedMixName` rather than being guessed into the nearest family.

## 6. Language/cache/local are not equivalent

- Language packages may have special precedence and can be altered by Ares.
- Cache packages generally carry graphical/resource data and have different wildcard conventions.
- Local packages commonly carry INIs and local resources and are often explicit children.
- Expansion packages are numbered patch/mod layers.

Do not sort all four by one shared `SequenceNumber`.

## 7. Evidence status

The archive family names are mostly `ConfirmedCommunityConvention` and `ConfirmedByMultipleIndependentImplementations`. Exact original loading calls and all ordering edge cases remain `Underconfirmed` unless an official/original source establishes them.

The project profile remains `ConfiguredProjectPolicy` even where it intentionally chooses a deterministic behavior that is stricter than observed historical tools.
