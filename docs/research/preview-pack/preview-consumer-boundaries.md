# Preview consumers, display transforms, and non-authoritative boundaries

> Source notice: researched by **ChatGPT Web** from public sources; no local `ProjectBaseline`; not a Codex Agent product; no code copied, translated, mechanically rewritten, or imported (`code_imported: false`).

## 1. Preview is a presentation artifact

PreviewPack is a stored image used by map-selection, editor, browser, export, or cache consumers. It is not authoritative map state.

A structurally valid preview does not prove that terrain, overlays, bridges, resources, objects, waypoints, houses, or scripts match the image.

## 2. Candidate consumers

### Original game map-selection UI

Potential responsibilities:

- locating map metadata;
- decoding preview bytes;
- fitting into a fixed menu region;
- clipping or filling unused pixels;
- choosing a fallback when unavailable.

No public original runtime source was located. Exact behavior remains unresolved.

### FinalSun / FinalAlert 2

The official editor can render a minimap-like image and replace Preview/PreviewPack. Its writer converts from a Windows DIB representation. This is editor generation, not the format parser itself.

### World-Altering Editor

WAE can write an actual preview from a texture and can inject a fixed dummy preview. Texture readback, row orientation, section movement, and dummy generation are editor concerns.

### CnCNet client

The client extracts PreviewPack for map-selection display. It performs channel conversion, bitmap stride construction, hidden-preview recognition, and failure fallback. Its fast reader scans physical INI text for performance.

### CNCMaps

CNCMaps renders the full map, produces a bitmap, injects a preview, and can extract one. Rendering, crop, startup markers, lighting, and preview insertion are tool behavior.

### MapTool

MapTool exposes bitmap get/set operations around generic map-pack helpers. Conversion helpers and canonical rewriting are tool behavior.

### Future Unity UI

A future adapter may create texture objects, upload pixels, add alpha, flip UVs, scale, crop, cache, or display fallback art. None of those operations belong to Core.

## 3. Consumer descriptor

Recommended research model:

```text
PreviewConsumerDescriptor
- ConsumerId
- ConsumerVersion
- AcceptedMetadataProfiles
- AcceptedChannelProfiles
- AcceptedRowProfiles
- SectionPlacementExpectation
- MissingPreviewBehavior
- ScaleMode
- CropMode
- InterpolationMode
- NativePixelOrder
- NativeOrigin
- CachePolicy
- EvidenceGrade
```

This descriptor is test and compatibility metadata, not a format parser option that silently repairs input.

## 4. Display transforms

Consumer transforms must be recorded separately:

- component swap;
- vertical flip;
- horizontal flip;
- row-stride padding;
- alpha insertion;
- texture upload order;
- UV adjustment;
- nearest/bilinear filtering;
- aspect fit/fill/stretch;
- crop rectangle;
- letterboxing;
- display-size downscaling;
- thumbnail caching.

A transformed display image never replaces `PreviewDecodedStream`.

## 5. Size versus display rectangle

Preview metadata dimensions describe the stored pixel rectangle under the leading interpretation. They do not necessarily equal:

- game menu control dimensions;
- client thumbnail dimensions;
- exported PNG dimensions;
- map full/local dimensions;
- Unity texture dimensions after scaling;
- visible cropped dimensions.

Original-map proportion observations reported by ModEnc are empirical/community constraints, not the binary format's width/height formula or maximum.

## 6. Aspect ratio

No inspected packed-stream structure contains an aspect-ratio field. Consumers may preserve, stretch, crop, or letterbox. A writer may choose dimensions related to `Size` or `LocalSize`, but this is generation policy.

Core does not reject unusual aspect ratios if dimensions and budgets are valid.

## 7. Alpha boundary

Standard decoded data has three components per pixel. A consumer may construct opaque RGBA using alpha 255, but:

- alpha is derived;
- it is not stored in PreviewPack;
- it is not included in decoded hashes;
- premultiplication is not performed in Core;
- transparent fallback UI is unrelated to source pixels.

## 8. Color-space boundary

No source contract found a gamma or sRGB declaration. Consumer APIs may assume sRGB or perform conversion. Those assumptions are captured in the consumer descriptor and cannot alter format evidence.

## 9. Preview versus minimap

Keep separate:

- static PreviewPack image;
- editor-generated minimap rendering;
- runtime minimap/radar state;
- fog-of-war/shroud;
- explored cells;
- player-colored indicators;
- start markers drawn by a tool;
- cached lobby thumbnail.

PreviewPack may be generated from a minimap, but it is not the runtime minimap state.

## 10. Preview versus map validation

Preview pixels cannot be used to:

- infer missing IsoMap cells;
- select an Overlay coordinate profile;
- resolve tile IDs;
- identify theater;
- repair TMP binding;
- validate cliffs, water, resources, walls, or bridges;
- choose RGB/BGR by comparing expected terrain colors;
- select row order by visual plausibility.

Optional discrepancy analysis can report that a regenerated preview differs, but cannot mutate map data or source preview.

## 11. Cache identity

A cache key must include at least:

- source document identity;
- decoded stream hash;
- metadata interpretation profile;
- channel profile;
- row profile;
- consumer transform profile;
- target dimensions;
- interpolation/crop settings;
- adapter version.

Using only map filename or compressed payload hash risks mixing incompatible interpretations.

## 12. Error presentation

Core returns structured diagnostics. Consumers decide whether to:

- show no image;
- show a generic placeholder;
- hide the map;
- offer regeneration;
- display a warning badge;
- log details.

Consumers must not relabel malformed input as successful source parsing.

## 13. Security boundary

UI adapters receive validated dimensions and bounded pixel views. They do not allocate directly from raw metadata, decompress streams, or seek inside MIX files. Image export paths must sanitize filenames and never embed absolute source paths.

## 14. Compatibility reporting

Consumer compatibility is reported per target:

```text
ParsedByCore
DecodedExactly
InterpretedWithProfile
DisplayedByConsumer
ReopenedByEditor
AcceptedByOriginalRuntime
```

Research alone sets none of these to supported status in the formal matrix.

## 15. Non-goals

No texture, bitmap, thumbnail, screenshot, preview generation, UI, export, cache, minimap, or Unity implementation is included.