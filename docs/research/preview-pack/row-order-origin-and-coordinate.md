# Preview row order, origin, and coordinate boundaries

> Source notice: researched by **ChatGPT Web** from public sources; no local `ProjectBaseline`; not a Codex Agent product; no code copied, translated, mechanically rewritten, or imported (`code_imported: false`).

## 1. Raw stream does not contain coordinates

The decoded payload is a flat byte sequence. Width and height come from metadata. Pixel coordinates are derived only after selecting a layout profile.

Core must not treat byte offset as an implicit Unity, screen, map-cell, or world coordinate. This separation is `DefensiveDesign`.

## 2. Explicit layout profiles

### `RowMajorTopDown`

Rows occur from top to bottom. Pixels inside each row occur left to right.

### `RowMajorBottomUp`

Rows occur from bottom to top. Pixels inside each row occur left to right.

### `ColumnMajor`

Columns are contiguous. No inspected standard writer required this profile, but it remains available as an unresolved comparison profile.

### `Unknown`

No coordinate mapping is selected.

Every profile consumes the same immutable decoded bytes.

## 3. EA editor conversion

EA's released editor generates a 24-bit DIB minimap. Its source row uses:

```text
height - destinationRow - 1
```

while its destination offset advances by ordinary destination row. The operation converts the bottom-up DIB representation into a top-down packed stream. Pixels within a row advance left to right.

```text
EvidenceGrade: ConfirmedByOfficialToolSource
Source: EA FinalSun / FinalAlert 2
AuditStatus: NotRun
```

This confirms `RowMajorTopDown` for generated official-editor output. It does not prove the original game's reader implementation or every third-party map layout.

## 4. CnCNet consumer

CnCNet iterates decoded rows from `0` through `height-1`, creates a corresponding consumer row, and performs only a channel swap. It does not vertically reverse decoded PreviewPack rows.

```text
EvidenceGrade: ImplementationSpecificBehavior
Source: CnCNet XNA client
AuditStatus: NotRun
```

This supports top-down consumption in that client.

## 5. CNCMaps

CNCMaps iterates bitmap rows in increasing `y`, writes a flat sequence, and reverses only component order as required by the bitmap API. Its extraction path follows the same row progression.

```text
EvidenceGrade: ImplementationSpecificBehavior
Source: CNCMaps
AuditStatus: NotRun
```

This supports a symmetric row-major profile in that tool, although bitmap stride sign and API conventions remain adapter details.

## 6. WAE

WAE calls `Texture2D.GetData` and writes the returned color array in linear order without an explicit vertical flip. The source does not independently define the texture API's conceptual origin.

```text
EvidenceGrade: ImplementationSpecificBehavior
Source: World-Altering Editor
AuditStatus: NotRun
```

This proves that WAE preserves its texture API's linear order, not one external runtime standard.

## 7. MapTool

MapTool passes the exact decoded `width × height × 3` array to a graphics helper. The inspected map file does not expose whether that helper flips rows.

```text
EvidenceGrade: Unresolved
Source: MapTool map-file caller only
AuditStatus: NotRun
```

MapTool therefore provides no independent row-order result from the reviewed file.

## 8. Normalized row-order summary

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert emits top-down row-major preview bytes after reversing DIB rows | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official-editor writer behavior only. | Preserve as a named writer profile. | `NotRun` |
| CnCNet consumes rows top-down without a Core flip | `ImplementationSpecificBehavior` | CnCNet XNA client | Named consumer behavior. | Keep bitmap/consumer transforms outside Core. | `NotRun` |
| CNCMaps uses a symmetric row-major conversion | `ImplementationSpecificBehavior` | CNCMaps | Tool-specific behavior with bitmap API caveats. | Comparison only. | `NotRun` |
| WAE preserves texture linear order | `ImplementationSpecificBehavior` | World-Altering Editor | External conceptual origin is not established by the writer alone. | Do not count as independent runtime proof. | `NotRun` |
| `RowMajorTopDown` is the leading standard candidate | `Underconfirmed` | FinalAlert, CnCNet, CNCMaps | Convergence exists, but implementation independence and original-runtime behavior are not established. | Require an explicit row profile. | `NotRun` |
| One unique original-runtime row contract | `Unresolved` | No original-runtime source located | Bottom-up or extension-specific payload acceptance is unknown. | Preserve top-down, bottom-up, column-major, and unknown profiles. | `NotRun` |
| No decoded scanline padding in the reviewed standard paths | `Underconfirmed` | FinalAlert, public tools, ModEnc | Reviewed writers allocate `width × height × 3`; this does not prove universal runtime rejection of padded variants. | Standard profile requires tight rows. | `NotRun` |
| Unity vertical correction changes the format row contract | `DefensiveDesign` | Project architecture | Unity flips are adapter behavior and must not mutate raw bytes or Core profile evidence. | Record adapter transform separately. | `NotRun` |

## 9. No scanline padding

ModEnc permanent revision `oldid=28503` states that each packed scanline is exactly `width × 3` bytes. Public writers allocate exactly `width × height × 3` and do not add row padding to the LZO input.

Windows bitmap stride padding is added only when CnCNet or other consumers build a bitmap buffer. It is not part of the reviewed decoded PreviewPack streams.

## 10. Metadata origin fields

`Size` fields 0 and 1 may be called X/Y, left/top, or origin candidates. No inspected pixel reader uses them to offset into the decoded array. Consumers generally decode a full `raw2 × raw3` rectangle beginning at preview pixel `(0,0)`.

Therefore:

- metadata origin is not used as a byte offset;
- negative origin does not authorize negative array indexing;
- preview origin is not an IsoMap coordinate;
- preview pixel `(x,y)` is not a map cell;
- crop and display positioning belong to consumers.

These are `DefensiveDesign` boundaries pending stronger source evidence.

## 11. Coordinate domains to keep separate

- `PreviewPixelCoordinate`
- `[Preview]` origin candidate
- source bitmap/DIB coordinate
- display-control coordinate
- texture coordinate
- normalized UV
- screen coordinate
- map IsoMap raw coordinate
- map normalized diamond coordinate
- simulation/world coordinate
- Unity texture coordinate

No implicit conversion crosses these domains.

## 12. Unity vertical orientation

Unity texture upload conventions can make a top-down CPU array appear vertically inverted depending on adapter choices. Fixing that belongs to a Unity adapter and must not change the Core row-order profile or decoded bytes.

An adapter may:

- upload rows in reverse;
- adjust UVs;
- flip a display quad;
- use a shader transform.

The adapter records the transformation in provenance.

## 13. Project policy

Initial project policy:

- retain raw stream;
- require explicit `PreviewRowOrderProfile`;
- use `RowMajorTopDown` as the leading standard candidate;
- reject automatic top/bottom trial rendering;
- reject image-orientation plausibility tests;
- reject column-major fallback;
- do not use metadata fields 0/1 to shift pixels without separate evidence;
- include row profile in semantic hashes and cache keys.

```text
PolicyClassification: DefensiveDesign
AuditStatus: NotRun
```

## 14. Derived pixel indexing

Only after validation and profile selection may an interpreter derive a pixel view. Arithmetic is checked and bounded by validated pixel count. No pixel array is created if width, height, component count, or decoded length is invalid.

## 15. Round-trip effect

Changing row-order interpretation does not change decoded identity. Re-encoding with a different row order would change semantic pixel placement and compressed bytes, so a future writer must never flip by default.
