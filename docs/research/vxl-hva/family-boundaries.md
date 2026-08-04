# Family and responsibility boundaries

> Prepared by ChatGPT Web from public reference material. No ProjectBaseline or original asset data was read.

## 1. Supported binary family

The target is the Westwood voxel family used by **Tiberian Sun, Red Alert 2 and Yuri's Revenge**:

- VXL files beginning with a 16-byte `Voxel Animation` identifier field;
- one or more named sections in a shared file;
- sparse per-column voxel span data;
- per-section scale, transform, bounds, dimensions and normal-table selector;
- optional/paired HVA files containing named per-frame transforms.

A `.vxl` extension alone is not a format discriminator. Build-engine KVX, Command & Conquer 3 assets, modern voxel editors' native formats, and reverse-engineering notes that describe a different signature/layout are separate families.

## 2. VXL versus HVA

### VXL owns

- file identifier and raw header fields;
- embedded 256-entry color palette and remap-range bytes;
- section header names and raw section metadata;
- section-body offsets and sparse voxel span data;
- color and normal indices per stored voxel;
- section-local scale, 3×4 transform, min/max bounds and grid dimensions;
- the raw normal-table selector byte.

### HVA owns

- a raw 16-byte filename/label field;
- frame count and section count;
- a 16-byte name field for each HVA section;
- one raw 3×4 transform record per declared frame/section pair.

HVA does not contain voxel colors, normals, dimensions or spans. VXL does not contain a frame sequence equivalent to HVA animation.

## 3. Section is not a separate file

A VXL section is an internal named component of one VXL document. It has:

- one section-header record;
- one candidate section-tailer record;
- its own span-directory offsets into the shared body region;
- its own grid dimensions, bounds and transform.

Section order is preserved as file evidence. It may be relevant to pairing corresponding header/tailer records and to fallback behavior in existing tools, but order alone must not silently resolve ambiguous HVA names.

## 4. Art.ini is an upper layer

The binary readers must not decide:

- whether an Art.ini entry is voxel-backed;
- how `Image`, `Voxel`, turret, barrel or alternate-water resources are named;
- whether a body, turret and barrel are separate VXL/HVA pairs;
- whether missing HVA is acceptable for a particular object class;
- animation cadence or which HVA frames are used for movement/firing;
- remapability or theater/resource substitution policy.

Those decisions belong to typed Art/resource resolution and later content-composition services.

## 5. Geometry versus projection

VXL contains sparse indexed voxel geometry and section-local metadata. It does not specify the full runtime renderer.

Outside the format reader:

- isometric camera projection and 3D-to-2D rasterization;
- facing quantization and render caching;
- z-buffer/depth ordering;
- body/turret/barrel draw ordering;
- shadow projection;
- slope tilt and terrain alignment;
- impact, recoil, airborne or physics transforms;
- ambient/diffuse lighting and light direction;
- palette/VPL lookup and final color generation.

A parser may expose raw fields needed by those systems without implementing their semantics.

## 6. HVA versus game-logic rotation

HVA transforms are authored animation data. They are distinct from simulation state such as:

- vehicle facing;
- independent turret facing;
- barrel pitch/recoil;
- locomotor slope orientation;
- airborne roll/pitch;
- interpolation timing.

A renderer may compose simulation transforms with HVA and VXL transforms later. The binary model must keep each source distinguishable.

## 7. Normal tables versus palette and VPL

The voxel record stores a color index and a normal index as separate bytes.

- The VXL header contains an RGB palette and remap range.
- The section tailer contains a normal-table selector.
- The normal vectors themselves are external engine/tool constants.
- VPL maps lighting/normal/color combinations for final rendering and is a separate format/resource.

The parser must not infer a normal vector from the color palette or apply lighting while reading geometry.

## 8. Proposed dependency direction

```text
VXL raw reader ──> validated sparse VXL model
HVA raw reader ──> raw HVA transform model
                       │
                       v
                VXL/HVA binder
                       │
Art/resource view ──> resource composition
                       │
simulation state ──> transform composition
                       │
normal/VPL/palette ─> renderer/projection
```

No reverse dependency from Core binary readers to Art.ini, simulation or Unity is permitted.
