# Source comparison and evidence register

> GPL and unclear-license code is reference-only. No source was copied, line-translated, mechanically rewritten, or converted into a near-structural C# port.

## 1. Pinned sources

| Source | Pin / path | License | Relevant behavior | Classification / limits |
|---|---|---|---|---|
| [OpenRA engine](https://github.com/OpenRA/OpenRA) | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`; `OpenRA.Game/FileSystem/FileSystem.cs`, `OpenRA.Mods.Cnc/FileSystem/MixFile.cs` | GPL-3.0-or-later | explicit mounts, later-mounted package wins generic file lookup, nested package opening by explicit request | `ImplementationSpecificBehavior`; not original RA2/YR discovery |
| [OpenRA RA2 mod](https://github.com/OpenRA/ra2) | `61e24e3c1d7b586aa55a86096d29e1559aa9b994`; `mods/ra2/mod.yaml` | GPL-3.0-or-later | explicit fixed package list including base and known nested packages | does not demonstrate stock automatic `expandmd` scanning |
| [Chrono Divide mod SDK](https://github.com/chronodivide/mod-sdk) | `5943c4ae6c19897929d348a417d6d2f1481b75fd`; `README.md` | no repository license file located; reference-only | exact `expand##/ecache##/elocal##` patterns, 99→00 priority, loose priority, explicit field-by-field rules/art overlay | RA2 reimplementation; YR mods unsupported; not original proof |
| [EA FinalSun/FinalAlert 2](https://github.com/electronicarts/CNC_TS_and_RA2_Mission_Editor) | `6abf0f557469baea73079c6bf6550709e2e3584e`; Mission Editor sources and bundled XCC integration | GPL-3.0 | official editor/tool package discovery and map output behavior | official source, but editor is not game runtime |
| [XCC / OmniBlade mirror](https://github.com/OmniBlade/xcc) | `62bb77080f13bdf65c79c84837b7cc264bdd432d`; MIX utility sources | SourceForge GPL-2.0 lineage | archive IDs, opening/extraction, known package names | tool behavior; no proof of executable global precedence |
| [XCC SourceForge](https://sourceforge.net/projects/xccu/) | XCC Utilities 1.46 / source archive | GPL-2.0 | historical release/licensing anchor | no unproven mirror↔SVN equivalence |
| [Ares documentation](https://ares-developers.github.io/Ares-docs/) | Ares 3.0 documentation; legacy docs repo `7e2a509b731efb3a523d64a6933f2fde01903623` | documentation/source terms unclear; reference-only | include order and MIX/loading extensions; language precedence changes | MOD extension, not vanilla |
| [Phobos](https://github.com/Phobos-developers/Phobos) | `0c3858fb11d31bccb227f5fefcaae2334cb0e828`; `src/Misc/Hooks.INIInheritance.cpp`, docs | GPL-3.0; reference-only | `$Include`, `$Inherits`, recursive ordered merges, typed reset semantics | MOD extension; cannot define vanilla empty/list behavior |
| [CnCNet XNA client](https://github.com/CnCNet/xna-cncnet-client) | `e6e367bbe04c1a0dc1e34a8fed2856ea3ab7e8c4` | repository license to be checked before reuse; behavior reference-only | launcher/mod packaging, generated settings and launch environment | client/launcher, not executable content loader proof |
| [ModEnc MIX](https://modenc.renegadeprojects.com/index.php?title=MIX&oldid=31890) | permanent revision `oldid=31890` | community documentation | family names, `expand` two-digit ranges, nested locations, cache/local caveats, loose-file discussions | `ConfirmedCommunityConvention`; some claims conflict or describe tools |
| [ModEnc Rules](https://modenc.renegadeprojects.com/index.php?title=Rules&oldid=32705) | permanent revision `oldid=32705` | community documentation | `rulesmd.ini` locations by YR version | location evidence, not complete load algorithm |
| [Project Perfect Mod](https://ppmforums.com/) | fixed forum/tutorial threads | unclear community prose/code terms | historical archive-order and modding observations | leads and conflict evidence only |
| [RA2DIY tutorials](https://bbs.ra2diy.com/) | fixed public posts where accessible | community terms; reference-only | Chinese modding conventions and practical package placement | not original source |
| Other loaders | only license-clear, pinned implementations | varies | comparison and distinguishing tests | agreement remains reimplementation evidence |

## 2. Source independence

Do not count lineage duplicates as independent:

- EA editor bundles or integrates XCC components;
- XCC mirrors share ancestry;
- OpenRA RA2 configuration uses the OpenRA filesystem;
- CnCNet clients often prepare files then launch the original/spawner executable;
- community tutorials may repeat ModEnc or XCC observations.

## 3. Evidence-level conclusions

| Conclusion | Level |
|---|---|
| MIX is an archive and individual readers should not own global precedence | `ConfirmedByMultipleIndependentImplementations` |
| OpenRA later-mounted package wins generic lookup | `ImplementationSpecificBehavior` |
| Chrono Divide supports exact two-digit 00–99 families | `ImplementationSpecificBehavior` |
| Community uses numbered expansion packages with larger number higher | `ConfirmedCommunityConvention` |
| Frozen project uses `expandmd01..99`, gaps allowed | `ConfiguredProjectPolicy` |
| `00` is valid in original YR 1.001 | `ConflictingSources` / `Underconfirmed` |
| `ecache`/`elocal` share expansion numeric order | `Unresolved`; do not assume |
| loose root files are highest in project profile | `ConfiguredProjectPolicy` |
| all vanilla file categories uniformly accept loose overrides | `Underconfirmed` |
| arbitrary nested MIX is recursively mounted | unsupported; `Unresolved` and disabled |
| known local/cache/theater children are explicit mounts | `ConfirmedCommunityConvention` plus reimplementation evidence |
| generic same-name binary candidate uses one winner | `ConfiguredProjectPolicy`, supported by common virtual FS designs |
| configured same-name INIs compose at section/key level | `ConfiguredProjectPolicy` |
| vanilla YR automatically composes every same-name INI across every MIX layer | `Underconfirmed` / `ConflictingSources` |
| game-mode/rules extensions can overlay fields | `ConfirmedCommunityConvention` / implementation-specific |
| Ares/Phobos include/inheritance/reset syntax is vanilla | false; `ImplementationSpecificBehavior` |

## 4. Principal conflicts

### Number range

- ModEnc and Chrono Divide include `00..99`.
- Project profile accepts `01..99`.
- Treat `00` as a configurable unresolved extension, not an implicit default.

### `ecache` and `elocal`

Some community descriptions use wildcard enumeration rather than numeric ordering. Filesystem enumeration is nondeterministic for a portable engine. A deterministic project policy must be explicit and must not be called original behavior.

### Loose files

Practical modding sources support loose overrides for several types, but exact original handling can vary by executable mode, lookup path, and file category. Project loose-provider behavior remains configured.

### Generic winner versus INI composition

Generic virtual filesystems frequently return the highest candidate. Separate rules/mode/extension evidence demonstrates field-level overlays. The project resolves this by exposing both APIs instead of forcing all content into one rule.

### Language precedence

Ares alters language/expansion precedence. Therefore Ares-enabled profiles need distinct descriptors; their result is not vanilla.

## 5. Licensing boundary

- GPL and unclear-license code: behavioral reference only.
- MIT/permissive code, if later found: still requires a separate implementation and attribution review.
- Wiki/forum prose: paraphrase facts and retain revision/thread URLs.
- No code is included in this dossier.
