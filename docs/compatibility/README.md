# Compatibility matrix

`matrix.yml` is the authoritative compatibility inventory. It is initialized
to `未实现`; an entry advances only when its linked test and evidence meet the
definition below. A successful parse, display, or launch never implies
behavioral compatibility.

`format.bounded-reader` is a format-neutral safety foundation. Its `可解析`
status means synthetic bounded-input, budget, diagnostic, and tail tests pass.
WP-02C separately promotes only the tested MIX container, ID, encryption,
checksum, virtual-source, nesting, and XCC interoperability entries. WP-02D
promotes only strict raw PAL parsing after both synthetic and three fixed
ProjectBaseline samples pass. It does not promote SHP, VXL/HVA, TMP, PCX, CSF,
INI, map Pack, Texture2D, shaders, player remap, theater selection, rendering,
or runtime game behavior.

## Status vocabulary

| Status | Required evidence |
|---|---|
| `未实现` | No qualifying implementation evidence exists. |
| `可解析` | Valid inputs produce a bounded, inspectable model; malformed inputs are diagnosed. |
| `可显示` | Parsed data produces the intended visual/audio/text result; parsing alone is insufficient. |
| `可执行` | The feature participates in the deterministic runtime with defined state transitions. |
| `行为近似` | Runtime behavior is usable but one or more original comparisons remain outside tolerance or unmeasured. |
| `原版对照通过` | A reproducible YR baseline comparison, exact baseline hash, inputs, observations, and tolerance all pass. |
| `往返通过` | Read/write/reopen comparison preserves semantics and unknown data under the defined FA2 procedure. |
| `已知限制` | A separately linked limitation applies. It never promotes another status. |

Each entry has three independent status dimensions:

- `implementation`: current parse/display/execute/approximation capability;
- `original_comparison`: whether the original baseline comparison passed;
- `roundtrip`: whether a relevant write/reopen round trip passed.

`limitations` is an array. A nonempty array applies the `已知限制` flag even if
another dimension has passed. Empty evidence or test arrays cannot support a
status promotion.

## Evidence policy

Evidence records may contain paths, sizes, SHA-256 hashes, counts, offsets,
derived pixel/sample hashes, commands, and observations. They must not contain
original asset bodies, decoded original images/audio, reconstructable binary
payloads, or proprietary tools. Public CI uses synthetic fixtures only; local
golden evidence identifies user-supplied files by hash.

The recorded `YR1001_ProjectBaseline` directory manifest identifies patched
development content containing the official map add-on, music pack, and
compatibility patch. Its complete directory, MIX, and per-index PAL audit
manifests remain outside the repository; public evidence contains only
manifest SHA-256 values, aggregates, target IDs/sizes/hashes, container chains,
model hashes, scan facts, and approved representative metadata. Only the raw
PAL payload structure is interpreted; visual output is not. This still does
not constitute a clean YR 1.001 manifest, visual comparison, or original
behavior comparison.

For XCC, `往返通过` means the specifically recorded synthetic semantic
contract passed: entry sets/order where required and extracted payload hashes
were preserved. It does not imply byte-identical archive reconstruction. The
XCC-created input and project PreserveEntryOrder rebuild have different hashes,
and that result is retained as an explicit limitation.

## Update procedure

1. Add or update a focused matrix entry; do not use a broad feature to hide an
   untested sub-feature.
2. Link automated test IDs and the local or synthetic evidence record.
3. Record the exact YR content manifest and FA2 executable hash when relevant.
4. Add any limitation without weakening or deleting prior evidence.
5. Review matrix schema and evidence in the same change as implementation.
