# Source comparison and conflict register

> GPL and unclear-license code is reference-only. No decoder code, control flow, line translation, or mechanical rewrite is included.

## 1. Pinned source table

| Source | Pin / revision and path | License | Concrete evidence |
|---|---|---|---|
| Electronic Arts FinalSun/FinalAlert 2 | `6abf0f557469baea73079c6bf6550709e2e3584e`; `MissionEditorPackLib/MissionEditorPackLib.cpp/.h`, `MissionEditor/MapData.cpp` | GPL-3.0 for released editor; bundled XCC retains lineage | official editor integration for Base64, Format80, chunk headers, LZO map packs; not original game runtime |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`; `LCWCompression.cs`, `LZOCompression.cs`, `ImportGen2MapCommand.cs` | GPL-3.0-or-later; miniLZO-derived file also carries GPL lineage | five LCW commands, absolute/relative switch, map chunk loops, LZO1X decoder naming |
| World-Altering Editor | `b4c9481e9b00fb0a38739049a046f528b6054ce2`; `Format80.cs`, `Format5.cs`, `MiniLZO.cs`, `MapWriter.cs`, `MapLoader.cs` | GPL-3.0-or-later; miniLZO port GPL | modern writer/reader, 8192-byte chunks, 70-char fragments, explicit miniLZO 2.06 lineage |
| CnCNet XNA client | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4`; `MapPreviewExtractor.cs` | repository license; reference-only | Preview chunk bounds and `width*height*3` output expectation |
| OmniBlade/XCC mirror | `62bb77080f13bdf65c79c84837b7cc264bdd432d`; compression/map utility files | GPL-2.0 SourceForge lineage | historical tool behavior; exact mirror-to-release file equivalence unresolved |
| XCC SourceForge | XCC Utilities 1.46 source release | GPL-2.0 | historical release/license anchor |
| ModdingWiki | Westwood Format-80, permanent revision `oldid=12721` | wiki terms | command masks, optional relative marker, overlap description |
| ModEnc | Format80/map pack pages at fixed old revisions where available | community reference | terminology and map usage; not original source |
| Chrono Divide mod SDK | `5943c4ae6c19897929d348a417d6d2f1481b75fd` | no clear repository license located | no pinned public compression implementation located; no algorithm vote |
| LZO official project | LZO 2.10 and miniLZO distribution documentation | GPL-2-or-later or commercial license | LZO/miniLZO license and LZO1X codec capabilities |
| lzokay C++ | `AxioDL/lzokay`, pin `db2df1fcbebc2ed06c10f727f72567d40f06a2be` | MIT | permissive LZO1X implementation candidate; later dependency/security review required |
| lzokay-rs | crate 2.0.1 / `encounter/lzokay-rs` | MIT | pure Rust LZO1X candidate; native/FFI suitability requires separate review |
| Project Perfect Mod | fixed compression discussions | forum/community terms | edge-case leads and historical observations only |

## 2. Source URLs

- EA editor: `https://github.com/electronicarts/CNC_TS_and_RA2_Mission_Editor`
- OpenRA: `https://github.com/OpenRA/OpenRA`
- WAE: `https://github.com/CnCNet/WorldAlteringEditor`
- CnCNet client: `https://github.com/CnCNet/xna-cncnet-client`
- XCC SourceForge: `https://sourceforge.net/projects/xccu/files/`
- ModdingWiki Format80: `https://moddingwiki.shikadi.net/wiki/Westwood_Format-80_Compression`
- LZO: `https://www.oberhumer.com/opensource/lzo/`
- lzokay: `https://github.com/AxioDL/lzokay`
- lzokay-rs: `https://github.com/encounter/lzokay-rs`

## 3. Behavior conflicts

| Topic | Evidence | Current decision |
|---|---|---|
| LCW name | widely synonymous with Format80 | alias only after variant/envelope declared |
| medium/long position | absolute in WAE map code; relative option in OpenRA/community | explicit variant |
| relative marker | community description; not handled by inspected WAE map decoder | underconfirmed for RA2/YR maps |
| payload input window | map header declares it; some decoders do not receive it | strict bounded window required |
| output overflow | partial return, unsafe write, or exception in references | structured failure only |
| exact output | tools may trust declared size | backend result must match |
| terminator trailing bytes | commonly unreported | strict failure |
| zero chunk | readers break on either zero; writers omit sentinel | strict reject; optional final 0/0 experiment |
| max chunk output | WAE writer uses 8192 | map-profile policy/convention, not original hard fact |
| LZO family | miniLZO/LZO1X implementations converge | raw LZO1X-compatible |
| original LZO encoder | not exposed by decode behavior | unresolved |
| fragment order | source enumeration in consumers; numeric keys from writers | raw view plus explicit normalized policy |
| fragment zero/gaps | no original proof | unresolved/fail-closed default |
| license route | GPL reference implementations dominate historical ecosystem | permissive reviewed backend or independent implementation |

## 4. Independence limits

- EA editor and WAE rely on XCC/OpenRA/community lineage.
- OpenRA and WAE miniLZO ports share the same translated origin.
- Multiple successful readers using the same code ancestry count as compatibility evidence, not independent proof.
- The official EA editor is official tool evidence, not the game executable’s decoder source.

## 5. Evidence not found

No pinned public source reviewed here establishes:

- original RA2/YR LZO compressor level;
- original acceptance of zero-size chunks;
- original fragment-gap or duplicate behavior;
- original Overlay relative-marker usage;
- legal patent clearance for a new implementation.

These remain explicit unresolved items.
