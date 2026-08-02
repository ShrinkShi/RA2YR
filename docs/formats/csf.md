# Westwood CSF string tables

## Scope

WP-02E reads the compiled string table used by the controlled Yuri's Revenge
content baseline. It preserves the physical document structure and raw text
semantics. It does not write CSF files or implement runtime localization,
language selection, missing-label fallback, UI formatting, or font behavior.

The implementation is independently authored for this Apache-2.0 repository.
XCC source is GPL-licensed and is used only to establish format facts and
observable limitations. No source, translation, or mechanical rewrite is
imported.

## Evidence

The format conclusions are cross-checked against:

- the fixed `YR1001_ProjectBaseline` entry `langmd.mix -> ra2md.csf`, already
  located by the bounded MIX audit as ID `0xBD835079`, 332,973 bytes, SHA-256
  `1B90BB0756137F46FF529AF043FE798D7F1F9FA1713A4110F17E1D674DE81F1C`;
- XCC SourceForge SVN r1201 and OmniBlade/xcc commit
  `62bb77080f13bdf65c79c84837b7cc264bdd432d`;
- the independently implemented `iron-curtain-engine/cnc-formats` commit
  `77da596ed72a1201740e054855bf2ff60640bfa9`;
- LewisXY's independently published CSF tool revision
  `ba6046f6aa031a1bbab0a56c8e8bc7625ab5604f`.

The pinned OpenRA commit
`a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` contains no CSF implementation or
CSF format documentation and is not used as WP-02E evidence. Exact source,
license, and reference-only boundaries are recorded in
`docs/third-party/sources.yml`.

## Header

The header is exactly 24 bytes. All numeric fields are unsigned 32-bit
little-endian values.

| Offset | Size | Meaning |
|---:|---:|---|
| `0x00` | 4 | Exact bytes `20 46 53 43`, ASCII `" FSC"` |
| `0x04` | 4 | Format version |
| `0x08` | 4 | Label-record count |
| `0x0C` | 4 | Total value-record count |
| `0x10` | 4 | Reserved value |
| `0x14` | 4 | Numeric language code |

Yuri's Revenge uses version 3. WP-02E supports version 3 only and rejects an
unknown version instead of guessing its layout. The reserved value is retained
verbatim and is not required to be zero. The language code is likewise
retained as its raw number so values outside the documented set remain
representable. The controlled sample has reserved value 0 and language code 9
(Chinese).

## Ordered records

The label records immediately follow the header:

```text
bytes[4]  exact marker " LBL"
UInt32LE  value count for this label
UInt32LE  label-name byte length
bytes[N]  non-NUL-terminated label name
value records in file order
```

The label length is a byte count. Cross-checked documentation and the
controlled sample identify label names as 7-bit ASCII. Parsing preserves the
original bytes' case and whitespace; it does not trim, normalize, or case-fold
the name.

The value count may express zero, one, or multiple values. The sum of all
per-label counts must equal the header's total value count. Labels and values
retain file order. The physical format has no uniqueness field, so duplicate
labels are preserved rather than collapsed into a dictionary.

## Normal and extended values

The exact on-disk value layouts are:

```text
normal:
    bytes[4]  " RTS"
    UInt32LE  main-text length in UTF-16 code units
    bytes[N * 2] encoded main text

extended:
    bytes[4]  "WRTS"
    UInt32LE  main-text length in UTF-16 code units
    bytes[N * 2] encoded main text
    UInt32LE  extra-text length in bytes
    bytes[M]  non-NUL-terminated ASCII extra text
```

XCC writes the C++ multi-character literals as `STR ` and `STRW`; their
little-endian file representation is `" RTS"` and `"WRTS"`. WP-02E accepts
only the exact YR on-disk markers. It does not broaden the grammar to
`" STR"`, `"STRW"`, or another implementation's permissive aliases.

The extra text is a distinct field and is never appended to the main text.
Its length is a byte count, and the controlled sample contains only 7-bit
ASCII extra bytes.

## Raw UTF-16 semantics

Each stored main-text byte is inverted with bitwise NOT, equivalently XOR
`0xFF`. Each resulting low/high byte pair is then combined as one little-endian
16-bit code unit. The declared main-text length counts these code units, not
bytes, Unicode scalar values, or rendered characters.

The authoritative model retains the resulting `UInt16` sequence exactly. It
does not use the current Windows code page, Unicode normalization, trimming,
newline replacement, or a decoder fallback that can insert replacement
characters. A supplementary character remains its high/low surrogate pair in
the original order. The format sources do not establish that an isolated
surrogate is structurally invalid, so raw code units remain representable
without repair.

Any later interpretation of code points `0x80` through `0x9F`, fonts, or legacy
Windows-1252 display behavior belongs to the runtime localization and rendering
layers, not to the CSF parser.

## Strict read policy

The reader is fail-closed and uses the bounded binary foundation. It requires:

- version 3 and exact structural markers;
- checked count, offset, multiplication, and allocation arithmetic;
- explicit input, record, per-label, string, cumulative code-unit, and
  allocation budgets;
- strict 7-bit ASCII for label and extra fields;
- exactly the declared number of labels and values; and
- complete input consumption with no trailing bytes.

Truncation, an unsupported version, an unknown marker, a count mismatch, an
invalid length, a budget failure, a read failure, or trailing data rejects the
whole document. The parser never pads, truncates, clamps counts, skips an
unknown record, or exposes a partial-success document.

The inspected XCC readers are not a strictness model: they lower-case strings,
store labels in a map, overwrite duplicates, do not preserve order, and do not
correctly model multiple values per label. Those limitations are recorded as
negative evidence and are not reproduced.

## Controlled sample boundary

The earlier bounded MIX audit established the fixed entry identity. A
research-only structural scan of a repository-external, byte-identical XCC
extract observed 5,211 labels and values, 4,007 normal values, 1,204 extended
values, four empty main values, no duplicate labels, and no trailing bytes.
This scan does not replace the WP-02E golden requirement: status promotion must
remount `langmd.mix`, open the `ra2md.csf` entry window, revalidate its fixed
chain, size, and SHA-256, and parse that bounded window.

Neither research evidence nor the public golden summary contains label names,
string bodies, translations, complete record lists, Base64, or absolute paths.

## Compatibility boundary

WP-02E does not determine original runtime label case rules, duplicate-label
winner rules, language-pack precedence, current-language selection, missing
text fallback, placeholder substitution, UI rendering, fonts, or glyphs. It
also does not support CSF writing or claim clean YR 1.001 original comparison.
