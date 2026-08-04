# TS/RA2/YR TMP terrain-template layout

## 1. Family

The target is the isometric TMP/template format used from Tiberian Sun through Red Alert 2 and Yuri's Revenge, normally stored with theater-specific extensions such as `.tem`, `.sno`, `.urb`, `.ubn`, `.lun` and `.des`.

The extension selects a theater family; it is not a complete format discriminator and does not embed the palette.

## 2. File-level candidate layout

The strongest public readers expose:

```text
u32le templateWidthInCells
u32le templateHeightInCells
i32le cellPixelWidth
i32le cellPixelHeight
u32le cellOffsets[templateWidth × templateHeight]
cell records at nonzero offsets
```

A zero offset is the convergent empty-cell candidate.

Required checks:

- checked `templateWidth × templateHeight`;
- offset-table length inside input;
- nonzero offsets inside input and beyond required headers;
- duplicate, reversed, overlapping and aliased cell ranges;
- cell-record budget;
- no seek outside the bounded TMP window.

## 3. Cell record broad structure

OpenRA's reader indicates a fixed metadata/header area of approximately 52 bytes before the normal diamond image. It reads/uses:

- raw metadata in the first 20 bytes;
- extra-image X/Y/width/height;
- a flags word whose bit 0 controls extra image presence;
- another 12 raw metadata bytes;
- normal diamond color indices;
- normal diamond depth values;
- optional rectangular extra color indices;
- optional rectangular extra depth values.

Exact field names within the raw metadata area vary across XCC/editor/community descriptions. Initial Core must retain raw bytes and expose candidate views rather than freezing disputed labels.

## 4. Diamond image shape

For a cell with nominal pixel width/height, public readers reconstruct the diamond row widths as:

```text
4, 8, 12, ... maximum ..., 12, 8, 4
```

The width changes by four pixels per row around the vertical midpoint. The total normal-image byte count is a derived diamond-area formula, not simply width × height.

Validate:

- even/expected dimensions only under a named profile;
- derived row starts and widths with checked arithmetic;
- exact color and depth byte counts;
- no row crossing the destination bounds.

## 5. Depth image

The normal depth image is a second diamond-sized byte plane, parallel to the normal color image.

It is not:

- map cell elevation;
- alpha/transparency;
- a normal-vector index;
- runtime pathfinding height.

It is renderer/depth-order evidence. Preserve bytes unchanged.

## 6. Optional extra image

When the candidate extra-data flag is set, the cell includes a rectangular extra color plane and matching extra depth plane.

Typical uses include cliff faces and other pixels extending beyond the flat diamond.

The extra rectangle is positioned relative to the template/cell coordinate frame. Public importers convert those coordinates to their own sprite bounds; the parser must retain raw offsets and dimensions.

Checks:

- signedness/raw views for X/Y;
- nonnegative bounded width/height;
- checked area;
- sufficient bytes for both planes;
- union/bounds calculations without overflow;
- no implicit clipping.

## 7. Raw metadata candidates

Community tools name fields including:

- tile/cell X and Y within the template;
- extra-data offset/size;
- flags;
- height level;
- terrain/land type;
- ramp type;
- left/right color or radar/color metadata.

Because exact offsets and meanings have historically conflicted, proposed model:

```text
TmpCellHeaderRaw
- PrefixRaw[20]
- ExtraXRaw
- ExtraYRaw
- ExtraWidthRaw
- ExtraHeightRaw
- FlagsRaw
- SuffixRaw[12]
- CandidateMetadataViews[]
```

A later evidence-gated interpretation may expose named fields without removing raw storage.

## 8. Height and ramp boundary

Do not conflate:

- map-cell `Level` in `IsoMapPack5`;
- TMP cell-local height/ramp metadata;
- depth-image values;
- cliff extra-image vertical extent;
- renderer world height.

The TMP parser reports raw candidates. Theater and terrain systems decide how ramp types and levels affect movement and projection.

## 9. Palette and transparency

TMP stores indexed color bytes but no per-file palette. Palette selection belongs to theater/resource binding.

Do not assume:

- color index zero is always transparent in every plane;
- a specific theater palette from the extension alone without content provenance;
- editor display colors are file values;
- depth zero means transparent.

## 10. Cell order and section semantics

Cell-grid order is preserved as `v × templateWidth + u` candidate ordering. Empty cells remain explicit slots.

Template arrangement may encode multi-cell slopes, cliffs, shores, roads or bridges, but those semantics require theater INI and tile-set metadata. The TMP reader only exposes the ordered cells.

## 11. Strict ranges

Without explicit per-cell lengths, a safe reader can derive a candidate upper bound from the next greater distinct cell offset or EOF. As with SHP, this outer bound is not permission to consume arbitrary padding.

Each cell's expected normal/extra plane sizes must fit inside its candidate bound. Duplicate offsets are retained and diagnosed; partial overlap fails closed.

## 12. Forbidden behavior

Do not:

- allocate a full rectangular image before dimension budgets;
- treat offset zero as file start;
- choose metadata names from one editor without preserving raw bytes;
- clamp extra rectangles;
- synthesize missing depth data;
- derive pathfinding from pixels;
- apply a theater palette inside the binary reader;
- replace corrupt cells with clear terrain during parse;
- discard empty template slots;
- construct Unity textures or meshes in Core.
