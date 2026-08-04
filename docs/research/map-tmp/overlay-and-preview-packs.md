# OverlayPack, OverlayDataPack and PreviewPack

## 1. Overlay pair

`OverlayPack` and `OverlayDataPack` are parallel arrays indexed by a fixed map-cell coordinate function.

For the conventional RA2/YR format:

```text
index = x + 512 × y
array length = 512 × 512 = 262144 bytes
```

- `OverlayPack[index]` is the overlay-type index.
- `OverlayDataPack[index]` is the overlay frame/state byte.
- overlay type `0xFF` is the convergent no-overlay sentinel.

The arrays are compressed independently using Format80/LCW and encoded through numbered Base64 fragments.

## 2. Overlay identity boundary

The byte does not name an overlay. It indexes the effective `[OverlayTypes]` registry after global and map-local configuration composition.

Binding must preserve:

- raw type byte;
- raw data/frame byte;
- coordinate/index;
- registry version/provenance;
- resolved candidate or unresolved status.

Ore, walls, rails, bridges and other special overlays have runtime semantics outside the packed-array decoder.

## 3. Fixed canvas versus map bounds

The decoded arrays cover the 512×512 storage domain regardless of playable map dimensions. A strict reader should:

- require or explicitly profile the expected decoded length;
- retain entries outside declared local/full map bounds as diagnostics/evidence;
- not truncate the array to `[Map] Size` before hashing;
- not allocate larger arrays from hostile dimensions.

The fixed array does not prove all 262144 coordinates are legal placements.

## 4. Extended overlay formats

Modern editors can write an extended two-byte overlay-type array when a newer `NewINIFormat` profile is selected. This is extension behavior and must not be silently accepted as vanilla RA2/YR format 4.

Suggested discriminator:

```text
OverlayElementWidth
- Byte1VanillaCandidate
- UInt16ExtensionCandidate
- Unresolved
```

Output length, format metadata and source profile must agree before selecting the wider interpretation.

## 5. OverlayData value 255

Unlike OverlayPack, `OverlayDataPack` may legitimately use the full byte range depending on the overlay. Do not treat `0xFF` as no-data without overlay-specific evidence.

## 6. Preview metadata

`[Preview] Size=` supplies four comma-separated values, with width and height conventionally in the third and fourth positions.

Validate:

- exactly/at least the required fields under an explicit policy;
- positive bounded width and height;
- checked `width × height × 3`;
- optional relation to map/local size as a diagnostic, not a parser rule.

Community evidence reports different RA2 and YR size conventions and visual artifacts for unusual ratios. These are presentation/runtime constraints, not proof that the binary stream is malformed.

## 7. PreviewPack compression

The strongest public readers/writers use:

```text
numbered Base64 fragments
→ repeated {u16 compressedSize, u16 outputSize, LZO payload}
→ width × height × 3 bytes
```

Each pixel contributes three bytes.

### RGB/BGR naming conflict

- WAE writes channel values in R, G, B byte order but comments call the format BGR888.
- CnCNet preview extraction treats the decompressed array as 24-bit RGB and swaps red/blue when preparing a BGR graphics-library buffer.
- Tool comments may describe destination-memory order rather than file-byte order.

Initial Core model must name fields neutrally:

```text
PreviewPixel3
- Component0Raw
- Component1Raw
- Component2Raw
```

An approved color-order adapter should be selected only after comparing independent writer/reader behavior and sanitized image-level hashes. No preview pixels are published.

## 8. Preview section position

WAE records that TS and RA2/YR can require `[Preview]` and `[PreviewPack]` to occur first and moves them to the beginning when writing. Treat this as strong compatibility-tool evidence.

A lossless reader preserves original position. A writer may offer an explicit `OriginalGameCompatibility` ordering profile and report the move.

## 9. Missing preview

WAE writes a small dummy/hidden preview when the sections are absent because it reports crashes in original executables. This is writer/runtime-observation evidence, not permission for the parser to fabricate preview data.

Reader result states should include:

- `Absent`
- `MetadataOnly`
- `PackOnly`
- `Complete`
- `Malformed`
- `HiddenPreviewPatternCandidate`

## 10. Integrity and limits

For overlays:

- exact decoded-size validation;
- Format80 command and output limits;
- fixed index-domain checks;
- no last-write repair.

For preview:

- bounded dimensions and product;
- block count and cumulative output limits;
- exact expected pixel-byte count;
- zero-size block policy explicit;
- incomplete output fails without zero-padding;
- extra output remains trailing-data failure/diagnostic.

## 11. Forbidden behavior

Do not:

- combine OverlayPack and OverlayDataPack before both validate;
- assume every overlay byte maps to a current Rules registry;
- treat overlay frame `0xFF` as universally empty;
- infer bridge height from overlay packs alone;
- use preview as map terrain;
- silently swap preview channels until an image looks plausible;
- generate a dummy preview during read;
- apply editor UI frame limits as file-format limits.
