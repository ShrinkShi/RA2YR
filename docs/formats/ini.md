# Westwood RA2/YR INI physical documents

## Scope

WP-02F defines a lossless physical-document layer for the INI files consumed
by Red Alert 2, Yuri's Revenge, and FinalAlert 2. The authoritative state is
the original byte sequence plus ordered physical lines. Sections, key/value
records, comments, blank lines, and opaque lines are derived views over those
bytes.

This work does not define Rules, Art, AI, theater, or map-override semantics.
It also does not decide cross-file precedence, duplicate winners, defaults,
registries, references, or runtime text decoding. Identity output means only
that an unmodified document is emitted byte-for-byte unchanged.

## Evidence and license boundary

The physical syntax boundary was cross-checked against:

- the controlled `YR1001_ProjectBaseline` entries listed below;
- the FinalAlert 2 source published by Electronic Arts at commit
  `6abf0f557469baea73079c6bf6550709e2e3584e`, especially
  `MissionEditor/IniFile.cpp`, `MissionEditor/IniFile.h`,
  `MissionEditor/Loading.cpp`, and `MissionEditor/MapData.cpp`;
- OpenRA commit `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`, especially
  `OpenRA.Mods.Common/FileFormats/IniFile.cs`; and
- the already pinned XCC SourceForge and OmniBlade references, which were
  reviewed but did not establish an additional byte-preserving RA2/YR INI
  grammar.

The EA Mission Editor and OpenRA sources are GPL-licensed. XCC is GPLv2.
Every source is reference-only with `code_imported: false`; this Apache-2.0
implementation is independently authored. Exact source and license metadata
is in `docs/third-party/sources.yml`.

The two inspected parsers are behavioral evidence, not a complete statement
of original game behavior. Their disagreements are retained as unknowns
instead of being hidden behind a general-purpose INI library.

## Controlled baseline observations

The following entries were opened read-only through the bounded MIX virtual
content source. Their fixed lengths and hashes matched the prior MIX evidence.

| Logical entry | Provenance | Bytes | SHA-256 | CRLF | Other line endings |
|---|---|---:|---|---:|---:|
| `artmd.ini` | `ra2md.mix -> localmd.mix -> artmd.ini` | 336535 | `E1F0378394313C04EBBD5073F47785EE3E46F1B3C62D65724E8F3C310EE7BA31` | 19604 | 0 |
| `ai.ini` | `ra2.mix -> local.mix -> ai.ini` | 84972 | `1FEAC6DDEA6886B177DDF7E5F8580B7A99A63F12684F2CBB42831671BB7A8A79` | 4501 | 0 |
| `rulesmd.ini` | `expandmd01.mix -> rulesmd.ini` | 743215 | `3D341EF8A13A4B5AB24AF2EEF48AC94931AC2BB87D950FE3330A07E2D25672EF` | 31071 | 0 |
| `rulesmd.ini` | `ra2md.mix -> localmd.mix -> rulesmd.ini` | 742958 | `06761DD7F714E7D9400216EC3C06109EC5C1461F6A0727BE7401EB9D8B0F6D05` | 31061 | 0 |

All four are BOM-less, contain only bytes `0x00` through `0x7F`, contain no
NUL or other C0 control byte except tab and line endings, and use CRLF only.
They are therefore valid UTF-8 byte sequences, but that observation does not
identify UTF-8: the same bytes are valid under ASCII and the common legacy
single-byte code pages. The correct baseline classification is
`raw-single-byte / ASCII-compatible`, not inferred UTF-8 or a host code page.

The two `rulesmd.ini` candidates are distinct documents. WP-02F preserves and
audits both; it does not choose a winner or infer MIX precedence.

No public evidence contains section names, key names, values, comments,
complete line hashes, Base64, source bodies, or machine paths.

## Physical line model

The byte-order mark, when present, is a separate raw slice. Starting after the
BOM, line splitting uses the active physical encoding's exact representations
of CR and LF. CRLF is matched before a lone CR. Each line records:

- a stable zero-based physical line ID;
- its absolute byte offset within the bounded logical source;
- its original content bytes;
- its exact ending bytes and one of CRLF, LF, CR, or none; and
- the contiguous full raw slice.

No line is synthesized after end of input. This preserves an empty file, a
file containing only a BOM, mixed line endings, multiple final blank lines,
and a nonempty last line without a terminator without normalizing any of them.

## Conservative structural classification

Syntax punctuation is recognized as ASCII code units only. Space means ASCII
space or tab for structural decisions; no culture-aware whitespace operation
is used.

| Input shape | WP-02F classification | Reason |
|---|---|---|
| Empty or ASCII-space/tab-only content | `Blank` | No semantic payload is inferred. |
| First non-space byte/code unit is `;` | `Comment` | FA2 and OpenRA both treat this as a comment. |
| First non-space unit is `[`, followed by `]`, with only space/tab or a semicolon tail | `Section` | Conservative common section form. Name bytes and tail remain raw slices. |
| Nonempty key followed by the first `=` inside a current nonempty section | `KeyValue` | The first equals separates key and value; later equals remain value bytes. |
| Anything not covered above but safe to retain | `Opaque` | Identity output remains possible without inventing semantics. |

A key/value view retains the original key, both sides' whitespace, the equals
offset, and the complete value slice. An inline semicolon is never deleted.
Because original game behavior has not been established, an inline semicolon
produces an ambiguity diagnostic while the complete physical line remains
authoritative.

The following are deliberately opaque in WP-02F:

- a key/value-shaped line before a valid section;
- a line without an equals sign;
- an unterminated section header;
- an empty section name;
- a section header with unresolved trailing content;
- an unsupported control character; and
- a nonstandard directive or MOD extension.

This is a preservation policy, not a statement that every opaque line is
rejected by the game. A complete document containing opaque lines is reported
as `structured-with-opaque-lines`, distinct from a fully structured document
and from a failed parse.

## Evidence conflicts and unresolved grammar

The EA FinalAlert 2 parser removes bytes from the first semicolon onward,
searches a line for the first bracket pair, and, on the RA2/YR loading paths,
trims key and value views. It merges repeated sections and overwrites a
repeated key in its map. Its writer sorts and reformats the result.

The pinned OpenRA parser recognizes a section only when `[` is the first
character, removes the first semicolon and trims entries, treats a no-equals
line as an empty-valued key, merges case-folded sections, and keeps the first
duplicate key.

These observations establish useful common punctuation but conflict on
no-equals lines, leading section whitespace, case treatment, and duplicate
winners. Neither source establishes original YR runtime semantics for:

- empty section or key names;
- first-wins versus last-wins duplicates;
- line continuation;
- quotation or escaping;
- `#` comments;
- semicolons inside quoted or otherwise special values;
- control characters; or
- text code pages.

WP-02F therefore preserves all such input and does not expose a game-runtime
answer. Line continuation and quote syntax are not active grammar. A `#` line
is not silently treated as a comment.

## Encoding boundary

The physical parser never uses `Encoding.Default`, the Windows ACP, current
culture, or a BOM-less UTF-8 guess.

- No BOM: classify as `RawSingleByte`. ASCII punctuation can be parsed, while
  non-ASCII fields remain undecoded raw slices.
- UTF-8 BOM (`EF BB BF`): preserve the BOM and require strict UTF-8 if a text
  view is requested or validation is required. Replacement fallback is off.
- UTF-16LE BOM (`FF FE`): preserve the BOM, require an even post-BOM byte
  length, and recognize punctuation and line endings as little-endian code
  units.
- UTF-16BE BOM (`FE FF`): the same rule applies with big-endian code units.

A declared Unicode encoding with malformed bytes is a parse failure. An
explicit `IniTextEncodingPolicy` may create a strict decoded view; that view
never replaces the raw byte authority. Future RA2/YR code-page selection is a
separate runtime decision.

## Failure and identity-output boundary

NUL, a BOM/length contradiction, a confirmed encoding error, an input or node
budget failure, checked-arithmetic overflow, a read failure, loss of original
bytes, or an identity writer that cannot prove complete output fails closed.
No writable document is returned after such a failure.

For a successful unmodified document, `IniIdentityWriter` emits the retained
BOM and every ordered full-line slice. The writer does not reconstruct syntax,
trim, sort, merge, change case, choose a newline, decode/re-encode text, or
write in place. Publication is limited to approved repository-external cache
or test-result roots and must use a temporary file, flush, SHA-256 and byte
verification, reread validation, and atomic publication.

This supports only `unmodified-byte-identical` round trips. Semantic editing,
FinalAlert 2 edited round trips, Rules saving, and an original writer clone
remain unimplemented.
