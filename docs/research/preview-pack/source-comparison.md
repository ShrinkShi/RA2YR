# PreviewPack source, behavior, ancestry, and license comparison

> Source notice: researched by **ChatGPT Web** from public sources; no local `ProjectBaseline`; not a Codex Agent artifact; no code copied, translated, mechanically rewritten, or imported (`code_imported: false`).

## 1. Comparison rules

For each source this dossier records:

- exact project and pin;
- path or permanent revision;
- license boundary;
- reader/writer/editor/runtime/consumer role;
- facts directly established;
- facts not established;
- likely code or knowledge ancestry;
- reference-only status.

A tool that displays a preview correctly is not game-runtime evidence. Several community projects sharing XCC/CNCMaps helpers, map-pack knowledge, or cross-tool documentation are not counted as independent discoveries.

No reviewed source qualifies as `ConfirmedByOriginalRuntimeSource` for PreviewPack.

## 2. Source matrix

| Source | Pin/path | Role | Grade for direct behavior | Direct evidence | Lineage / limits |
|---|---|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e`, `MissionEditor/MapData.cpp` | official editor writer | `ConfirmedByOfficialToolSource` | last two Size fields set to generated width/height; first two inherited from Map Size; exact `w×h×3`; DIB bottom-up/BGR converted into top-down RGB; packed-section encoder used; 70-character fragments | official editor, not game runtime; packed helper lineage includes XCC-derived code |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2`, `MapWriter.cs`, `Map.cs` | editor writer/fallback | `ImplementationSpecificBehavior` | `0,0,w,h`; three bytes; code writes R/G/B despite BGR comment; 8192 output blocks; key 1 and 70 characters; sections moved first; fixed 106×61 dummy | shares ecosystem knowledge/components; does not prove runtime requirement or universal block maximum |
| CnCNet XNA client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`, `MapPreviewExtractor.cs`, `FastMapPreviewExtractor.cs` | consumer reader | `ImplementationSpecificBehavior` | reads fields 2/3; allocates `w×h×3`; calls data RGB; swaps to BGR consumer buffer; no source scanline padding; recognizes hidden payload; fast path preserves physical value order | consumer behavior, not runtime; exact output completion and numeric ordering are not established |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`, `CNCMaps.Engine/Map/ThumbInjector.cs` | bitmap reader/writer/injector | `ImplementationSpecificBehavior` | writes/reads `0,0,w,h`; exact three-byte allocation; 70-character fragments; inserts PreviewPack relative to Preview/Basic; symmetric component conversion | imported OpenRA/XCC exceptions and ecosystem ancestry prevent treating it as an independent runtime discovery |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6`, `MapTool.Logic/MapFile.cs` | reader/writer tool | `ImplementationSpecificBehavior` | reads fields 2/3; exact `w×h×3`; missing/invalid returns null; rewrites metadata to `0,0,w,h`; 70-character section rewrite | inspected file does not expose graphics-helper channel/row behavior; not lossless for origins |
| ModEnc PreviewPack | `index.php?title=PreviewPack&oldid=28503` | community documentation | `ConfirmedCommunityConvention` | Base64 fragments, u16/u16 chunk description, LZO, three bytes, no row padding, BGR888 claim, empirical size proportions | documentation, not executable or runtime source; BGR statement conflicts with executable evidence |
| ModEnc Preview | `index.php?title=Preview&oldid=21306` | community documentation | `ConfirmedCommunityConvention` | Preview holds size metadata; PreviewPack holds image | does not resolve four-field semantics |
| PPM CNCMaps release | fixed topic `36021` | community/tool release documentation | `ImplementationSpecificBehavior` | generated PreviewPack placement changed to after Basic rather than behind Digest | documents one tool release behavior, not game-executable requirement |
| PPM MapResize | fixed topic `55391` | tool limitation report | `ImplementationSpecificBehavior` | resize tool leaves preview unchanged and requires separate generation | tool behavior only |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | searched reference implementation | `Unresolved` | no load-bearing PreviewPack path located at pin | no vote on layout or channels |
| Chrono Divide SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | searched public SDK | `Unresolved` | no load-bearing PreviewPack path located | no vote |
| XCC / OmniBlade | fixed public mirrors and SourceForge lineage searched | historical tools/code lineage | `Underconfirmed` | XCC can generate sections according to community documentation | no pinned low-level Preview path used; historical lineage influences later tools |

All executable sources are reference-only. Every row has `code_imported: false`.

## 3. Official-tool evidence limits

The EA repository is official editor source, not RA2/YR runtime source.

```text
EvidenceGrade: ConfirmedByOfficialToolSource
Source: EA FinalSun / FinalAlert 2
AuditStatus: NotRun
```

It is especially valuable for:

- generated data byte order;
- generated row order;
- metadata writer behavior;
- exact raw input length;
- use of the map packed-section encoder.

It cannot settle:

- how the game handles malformed chunks;
- whether the game numerically sorts fragment keys;
- whether Preview sections must be first;
- whether the game accepts nonzero origins or unusual dimensions;
- whether missing Preview crashes every executable/version.

## 4. Source-lineage warning

Potential sharing includes:

- CNCMaps map-pack helpers or knowledge reused by WAE;
- XCC-derived packing concepts in the EA editor release and community tools;
- CnCNet ecosystem reuse of Rampastring/CNCMaps utilities;
- community documentation citing CnCNet or other tools;
- common community knowledge transferred across implementations.

Agreement among repositories may represent one or a few code/knowledge lineages rather than separate discoveries of runtime behavior. This dossier therefore does not use `ConfirmedByMultipleIndependentImplementations` for any reviewed PreviewPack claim.

## 5. WAE source/comment conflict

WAE's comment says BGR888 while its executable assignment writes R/G/B. The comparison retains both facts. Comments are not silently corrected, but executable assignment is the stronger description of what that writer emits.

Its dummy-preview and section-first policies are compatibility choices. They remain `ImplementationSpecificBehavior` and are not promoted to runtime facts.

## 6. CnCNet strictness limit

CnCNet performs useful bounds checks, but its decompressor can stop on input exhaustion or either zero size and return a preallocated destination without verifying aggregate written length. The image builder then sees the expected array length because allocation was fixed.

This is `ImplementationSpecificBehavior` with lenient zero-fill consequences, not an exact decode contract. The project strict profile intentionally differs under `DefensiveDesign`.

## 7. CNCMaps naming and API layout

`PixelFormat.Format24bppRgb` bitmap memory is BGR-oriented. CNCMaps' variable names label bytes as if they were RGB and its comments describe a reversal. Tracing injection and extraction shows a symmetric conversion consistent with raw RGB.

This is `ImplementationSpecificBehavior`; names alone are not byte-order evidence.

## 8. MapTool helper boundary

The map file calls external graphics helpers. Without pinning and inspecting those helper implementations, MapTool provides evidence for exact three-byte sizing and symmetric get/set behavior, not an independent channel or row-order fact. Channel and row order remain `Unresolved` for that source path.

## 9. Community documentation conflict

ModEnc's BGR888 claim is a `ConfirmedCommunityConvention`, but it conflicts with the EA writer and CnCNet consumer contract. The combined RGB/BGR result is `ConflictingSources`.

ModEnc's empirical preview-size ratios concern official-map generation/display appearance, not the packed-stream dimensions formula or a parser maximum.

## 10. Normalized evidence summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert writes fields 2/3 as preview width/height | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor writer behavior only. | Preserve all four raw fields. | `NotRun` |
| Fields 2/3 are the cross-tool width/height candidate | `Underconfirmed` | FinalAlert, WAE, CnCNet, CNCMaps, MapTool, ModEnc | Strong convergence but no original-runtime source or proven independent lineages. | Explicit metadata profile. | `NotRun` |
| Original-runtime meaning of fields 0/1 | `Unresolved` | No original-runtime source located | Tool writers and consumers treat them differently or ignore them. | Preserve raw; never force zero. | `NotRun` |
| FinalAlert emits `width × height × 3` raw bytes | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor output. | Source-pinned writer profile. | `NotRun` |
| `width × height × 3` is the leading standard decoded-length candidate | `Underconfirmed` | Official editor, public tools, ModEnc | Convergence does not prove runtime strictness. | Exact output is enforced as `DefensiveDesign`. | `NotRun` |
| No alpha, palette payload, scanline padding, or decoded trailer was found in reviewed standard paths | `Underconfirmed` | Reviewed public sources | Evidence-scope absence statement only. | Do not infer extra structures; preserve unknown tails on failure. | `NotRun` |
| FinalAlert writes top-down RGB preview bytes | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor writer behavior. | Leading producer profile. | `NotRun` |
| CnCNet RGB consumption, WAE assignment, and CNCMaps conversion | `ImplementationSpecificBehavior` | Named tools | Tool-specific behavior; not multiple independent runtime proof. | Keep source-pinned profiles. | `NotRun` |
| ModEnc BGR888 description | `ConfirmedCommunityConvention` | ModEnc | Stable documentation, not runtime source. | Retain BGR profile. | `NotRun` |
| One unique runtime channel order | `ConflictingSources` | RGB executable evidence versus BGR documentation | No runtime source resolves the disagreement. | Preserve raw components; no visual auto-selection. | `NotRun` |
| `RowMajorTopDown` is the leading row-order candidate | `Underconfirmed` | FinalAlert, CnCNet, CNCMaps | Runtime reader behavior remains unsourced. | Explicit row profile; Unity flips are adapter-only. | `NotRun` |
| Chunk header is `u16 compressed/u16 output` | `ConfirmedCommunityConvention` | Public tools and ModEnc | Stable toolchain convention; no runtime source. | Explicit envelope profile. | `NotRun` |
| WAE uses 8192-byte output blocks | `ImplementationSpecificBehavior` | WAE | Writer convention only. | Configurable writer profile. | `NotRun` |
| 8192 is a runtime hard limit | `Unresolved` | No runtime source located | Not established as LZO or runtime maximum. | Do not bake into backend. | `NotRun` |
| WAE first-section placement | `ImplementationSpecificBehavior` | WAE | Named writer behavior with a runtime claim. | Separate target profile. | `NotRun` |
| CNCMaps after-Basic placement | `ImplementationSpecificBehavior` | CNCMaps/PPM | Conflicts with WAE policy. | Separate target profile. | `NotRun` |
| Original-runtime section-order contract | `ConflictingSources` | First, after-Basic, and location-independent public behaviors | No original-runtime source selects a rule. | Preserve physical order losslessly. | `NotRun` |
| WAE fixed dummy and CnCNet recognition | `ImplementationSpecificBehavior` | WAE and CnCNet | Two named tool behaviors, not runtime standard. | Source absence and generated placeholder stay separate. | `NotRun` |
| Exact lengths, strict Base64, explicit LZO/profile, no trial decode, no fabrication, and non-authoritative preview boundary | `DefensiveDesign` | Project policy | Project preservation and fail-closed contracts. | Compatibility status remains unchanged. | `NotRun` |
| ProjectBaseline observations are already available | `Unresolved` | Audit not executed | ProjectBaseline was not read. | `FutureEvidenceSource: ProjectBaselineAggregateAudit`. | `NotRun` |

## 11. Legal implementation boundary

A future implementation may use:

- independently expressed field and envelope facts;
- black-box-compatible fixtures created without original assets;
- state-machine requirements;
- safety budgets;
- diagnostic categories;
- independently designed interfaces.

It must not:

- copy code;
- translate code line by line;
- reproduce distinctive control flow;
- port GPL helpers into production C#;
- use source-derived pseudo-code that is structurally equivalent;
- import original preview bytes or maps into public tests.

## 12. Audit status

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```

No ProjectBaseline data was accessed, and no compatibility status was changed.
