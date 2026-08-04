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

A tool that displays a preview correctly is not game runtime evidence. Several community projects sharing XCC/CNCMaps helpers are not counted as independent votes.

## 2. Source matrix

| Source | Pin/path | Role | License | Direct evidence | Does not prove |
|---|---|---|---|---|---|
| EA FinalSun / FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e`, `MissionEditor/MapData.cpp` | official editor writer | source header GPL-3.0-or-later | last two Size fields set to generated width/height; first two inherited from Map Size; exact `w×h×3`; DIB bottom-up/BGR converted into top-down RGB; packed-section encoder used; 70-char fragments | original game reader, arbitrary section order, malformed-input tolerance |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2`, `MapWriter.cs`, `Map.cs` | editor writer/fallback | GPL-3.0-or-later | `0,0,w,h`; three bytes; code writes R/G/B despite BGR comment; 8192 output blocks; key 1 and 70 chars; sections moved first; fixed 106×61 dummy | runtime requirement, comment correctness, universal block maximum |
| CnCNet XNA client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`, `MapPreviewExtractor.cs`, `FastMapPreviewExtractor.cs` | consumer reader | GPL-3.0 | reads fields 2/3; allocates `w×h×3`; calls data RGB; swaps to BGR consumer buffer; no scan padding in source; recognizes hidden payload; fast path preserves physical value order | exact runtime behavior; strict output completion; numeric fragment ordering |
| CNCMaps | `afb9c1ec118f5128cbc1f3fb5e35c7dfa0e422fb`, `CNCMaps.Engine/Map/ThumbInjector.cs` | bitmap reader/writer/injector | repository MIT default with imported OpenRA/XCC exceptions; reference only | writes/reads `0,0,w,h`; exact three-byte allocation; 70-char fragments; inserts PreviewPack relative to Preview/Basic; symmetric component conversion | independent codec ancestry; runtime section-order rule |
| MapTool | `f85f2226905496139f1258b5854fad915f9bbac6`, `MapTool.Logic/MapFile.cs` | reader/writer tool | GPL-2.0-or-later | reads fields 2/3; exact `w×h×3`; missing/invalid returns null; rewrites metadata to `0,0,w,h`; 70-char section rewrite | helper channel/row behavior from this file alone; lossless round-trip |
| ModEnc PreviewPack | `index.php?title=PreviewPack&oldid=28503` | community documentation | site documentation; reference only | Base64 fragments, u16/u16 chunk description, LZO, three bytes, no row padding, BGR888 claim, empirical size proportions | executable behavior, independent runtime proof |
| ModEnc Preview | `index.php?title=Preview&oldid=21306` | community documentation | site documentation; reference only | Preview holds size metadata, PreviewPack holds image | four-field semantics |
| PPM CNCMaps release | fixed topic `36021` | community/tool release documentation | forum post | generated PreviewPack placement changed to after Basic rather than behind Digest | game executable requirement |
| PPM MapResize | fixed topic `55391` | tool limitation report | forum post | resize tool leaves preview unchanged and requires separate generation | format semantics |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | searched reference implementation | GPL-3.0-or-later | no load-bearing PreviewPack path located at pin | no vote on layout or channels |
| Chrono Divide SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | searched public SDK | no repository license file located in prior dossier; reference only | no load-bearing PreviewPack path located | no vote |
| XCC / OmniBlade | fixed public mirrors and SourceForge lineage searched | historical tools/code lineage | GPL lineage; reference only | XCC can generate sections according to community docs | no pinned low-level Preview path used here |

## 3. Official-editor evidence limits

The EA repository is official editor source, not RA2/YR runtime source. Its evidence grade is `ConfirmedByOfficialEditorSource`.

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

## 4. WAE source/comment conflict

WAE's comment says BGR888 while its executable assignment writes R/G/B. The comparison retains both facts. Comments are not silently corrected, but code behavior is the stronger statement about what WAE emits.

Its dummy-preview and section-first policies are compatibility choices. They are not promoted to runtime facts.

## 5. CnCNet strictness limit

CnCNet performs useful bounds checks, but its decompressor can stop on input exhaustion or either zero size and return a preallocated destination without verifying aggregate written length. The image builder then sees the expected array length because allocation was fixed. This is lenient zero-fill behavior, not an exact decode contract.

The project strict profile intentionally differs.

## 6. CNCMaps naming and API layout

`PixelFormat.Format24bppRgb` bitmap memory is BGR-oriented. CNCMaps' variable names label bytes as if they were RGB and its comments describe a reversal. Tracing both injection and extraction shows a symmetric conversion consistent with raw RGB. The dossier avoids treating names alone as byte-order evidence.

## 7. MapTool helper boundary

The map file calls external graphics helpers. Without pinning and inspecting those helper implementations, MapTool provides evidence for exact three-byte sizing and symmetric get/set behavior, not an independent channel or row-order fact.

## 8. Community documentation conflict

ModEnc's BGR888 claim conflicts with the EA writer and CnCNet contract. It remains `CommunityDocumented` and is not deleted from the evidence set.

ModEnc's empirical preview-size ratios concern official-map generation/display appearance, not the packed-stream dimensions formula or a parser maximum.

## 9. Shared ancestry and independence

Potential sharing includes:

- CNCMaps map-pack helpers reused by WAE;
- XCC-derived packing concepts in the EA editor release and community tools;
- CnCNet ecosystem reuse of Rampastring/CNCMaps utilities;
- community documentation citing CnCNet code.

Therefore agreement is grouped by lineage where appropriate. Official-editor and CnCNet/CNCMaps/WAE agreement is strong practical evidence but still not official runtime source.

## 10. Legal implementation boundary

All executable sources are reference-only for this research. A future implementation may use:

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

## 11. Evidence summary

| Question | Leading result | Grade |
|---|---|---|
| Size fields 2/3 | width/height | official editor + independent implementations |
| Size fields 0/1 | origin/offset/raw preserved | unresolved |
| bytes per pixel | 3 | official editor + independent implementations |
| payload row padding | none | official editor + implementations + community docs |
| raw channel order | RGB leading; BGR documented conflict | conflicting sources |
| row order | row-major top-down leading | official editor + independent consumer |
| block header | u16 compressed/u16 output | independent implementations/community docs |
| 8192 max output | writer convention | independent implementation only |
| section placement | first vs after Basic conflict | unresolved runtime rule |
| missing-preview runtime behavior | compatibility concern | community/editor claim, unresolved |

## 12. `code_imported`

For every source above:

```text
code_imported: false
```

No formal third-party ledger was changed by this research.