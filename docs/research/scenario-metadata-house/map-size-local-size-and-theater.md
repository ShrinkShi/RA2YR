# Map Size, LocalSize, and Theater binding

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. This is not a Codex artifact. No GPL or unclear-license code was copied, translated, or mechanically ported. `code_imported: false`.

## Geometry identities

The following must remain distinct:

```text
Map Size rectangle
LocalSize rectangle
IsoMap raw coordinate domain
normalized diamond canvas
Overlay 512×512 storage coordinate
ScenarioCellId coordinate
Preview pixel rectangle
screen projection coordinate
simulation/world coordinate
Unity coordinate
```

No rectangle parser produces a Unity `Rect`, `Vector2`, `Vector3`, Tilemap coordinate, or world position.

## Raw four-field model

```text
ScenarioMapRectangleRaw
- Field0Raw
- Field1Raw
- Field2Raw
- Field3Raw
- RawText
- SourceOccurrence
```

Candidate interpretation:

```text
X = field0
Y = field1
Width = field2
Height = field3
```

All four raw fields remain available even when an implementation ignores the first two.

## Evidence comparison

### WAE reader

WAE:

- requires four fields for `Size` pre-check;
- uses fields 2 and 3 as width and height;
- reads all four `LocalSize` fields as rectangle X, Y, width, and height;
- reads `Theater` as a string.

WAE does not use `Size` fields 0 and 1 in its main map model.

Evidence grade: `ConfirmedByIndependentImplementation`.

### WAE writer

WAE writes:

```text
Size=0,0,width,height
LocalSize=x,y,width,height
```

This establishes a canonical WAE writer convention. It does not prove that nonzero `Size` origins are invalid to the original runtime.

### Official FinalSun / FinalAlert 2 editor

The official editor:

- displays map width and height separately;
- exposes `LocalSize` as visible/usable map size;
- accepts direct `LocalSize` editing;
- recalculates its map rectangle after LocalSize changes;
- exposes Theater selection;
- applies editor map-resize constraints such as width/height ranges and a combined limit.

These are `ConfirmedByOfficialEditorSource` editor behaviors. The UI constraints are not automatically format or runtime limits.

## Candidate rectangle semantics

### Candidate A — origin plus dimensions

```text
x, y, width, height
```

This is the leading candidate for `LocalSize` and a plausible raw model for `Size`.

### Candidate B — ignored first two fields plus dimensions

```text
reserved0, reserved1, width, height
```

This reflects WAE's `Size` reader/writer behavior.

### Candidate C — left, top, right, bottom

This must remain available as a comparison candidate only where a source explicitly supports it. No current leading implementation justifies selecting it automatically.

### Candidate D — profile-specific map origin

The first two fields may encode a coordinate origin used by some game/version/editor path. This remains unresolved.

## Prohibited auto-selection

Do not select a rectangle interpretation because:

- it contains more IsoMap records;
- it makes LocalSize fit;
- it matches a preview size;
- it makes Waypoint starts valid;
- its origin is zero;
- WAE writes it that way;
- current maps “usually” use zero.

Profile selection must be external and evidence-gated.

## Numeric contract

Each field should retain:

- raw text;
- signed integer candidate;
- unsigned integer candidate where relevant;
- parse status;
- overflow status;
- evidence grade.

Malformed examples:

- fewer or more than four comma-separated fields;
- empty field;
- whitespace-only field;
- negative width or height;
- zero width or height;
- integer overflow;
- trailing comma;
- extra token.

Default project policy does not repair them.

## Checked arithmetic

Any derived operations use checked arithmetic:

```text
rightCandidate  = checked(x + width)
bottomCandidate = checked(y + height)
areaCandidate   = checked(width × height)
```

No allocation is based on an unchecked rectangle area.

## Size versus LocalSize

Suggested relationship analysis:

```text
ScenarioRectangleRelationship
- SizeParsed
- LocalSizeParsed
- LocalWithinSizeCandidate
- OriginRelationship
- WidthRelationship
- HeightRelationship
- OverflowDetected
- EvidenceGrade
```

Potential states:

- LocalSize fully contained;
- LocalSize equal to Size;
- LocalSize partially outside;
- LocalSize completely outside;
- negative or zero dimensions;
- relationship unavailable due to parse failure;
- interpretation ambiguous.

No LocalSize is clamped into Size.

## Is LocalSize the playable area?

The official editor's label strongly supports “visible/usable area” as an editor concept. Community and client behavior often treat it as playable or visible map bounds.

However, the exact original runtime effects may include:

- camera bounds;
- buildable/playable region;
- starting-position validation;
- map-edge behavior;
- multiplayer preview framing;
- AI constraints.

These effects are not proven by the field name alone.

Recommended derived object:

```text
ScenarioPlayableAreaCandidate
- Rectangle
- ConsumerKindCandidates[]
- EvidenceItems[]
```

## Record count relationship

`Size` does not directly define a required dense IsoMap record count in this research. M3-R4 separately studies dense/sparse IsoMap behavior.

This dossier may report:

- geometry rectangle parsed;
- IsoMap record coordinates later tested against a selected domain;
- missing or out-of-domain records.

It must not infer the record set from rectangle area or create default cells.

## Multiplayer geometry boundary

Potential multiplayer checks:

- declared player count versus start Waypoints;
- starts inside selected Size domain;
- starts inside selected LocalSize domain;
- starts on existing IsoMap cells;
- duplicate start cells;
- House-authored start candidates.

These are consistency analyses, not parser repairs.

## Theater raw identity

```text
ScenarioTheaterRaw
- RawText
- NormalizedCandidate
- SourceOccurrence
```

Normalization may include a case-insensitive comparison candidate, but original casing is retained.

## Stock theater profile candidates

| Logical profile | Common token candidate | TMP extension | ISO palette role | Unit palette role |
|---|---|---|---|---|
| Temperate | `TEMPERATE` | `.tem` | temperate ISO | temperate unit |
| Snow | `SNOW` | `.sno` | snow ISO | snow unit |
| Urban | `URBAN` | `.urb` | urban ISO | urban unit |
| NewUrban | `NEWURBAN` | `.ubn` | new-urban ISO | new-urban unit |
| Desert | `DESERT` | `.des` | desert ISO | desert unit |
| Lunar | `LUNAR` | `.lun` | lunar ISO | lunar unit |

Exact control INI names and fallback behavior belong to a selected theater profile and the M3-R3 dossier.

## Theater binding result

```text
ScenarioTheaterBindingResult
- RawTheater
- LogicalTheaterCandidate
- SelectedProfileId
- ControlIniLogicalNameCandidate
- TmpExtensionCandidate
- IsoPaletteRoleCandidate
- UnitPaletteRoleCandidate
- Diagnostic[]
- EvidenceGrade
```

This result contains logical references only.

## Unknown theater

Unknown values may represent:

- typo;
- unsupported stock token;
- extension theater;
- editor/client alias;
- intentionally custom profile.

Default behavior:

- preserve raw token;
- return unresolved binding;
- do not fall back to Temperate;
- do not search by file existence;
- do not load assets to discover a match;
- do not normalize to the closest known string.

## NewUrban compatibility

The `.ubn → .urb` fallback observed in some editor/tool compatibility paths remains an explicit compatibility profile. It is not promoted here to a vanilla default.

## Theater is not climate simulation

Theater identity does not by itself define:

- weather;
- temperature;
- lighting;
- movement costs;
- resource economy;
- snow accumulation;
- lunar gravity;
- terrain collision;
- rendering pipeline.

Those require separate Rules, theater-control, asset, and simulation inputs.

## Lighting boundary

`[Lighting]` can provide later environment inputs such as ambient and color components. It does not change Theater identity and is not parsed by the theater binder.

Theater and Lighting may both contribute to future rendering, but remain independent raw sources.

## Roundtrip

A lossless writer preserves:

- all four Size fields;
- all four LocalSize fields;
- raw comma and spacing representation where supported;
- Theater casing;
- duplicates;
- unknown Map fields;
- physical order.

A canonical WAE-like writer may emit `Size=0,0,w,h`, but this is a distinct, explicit rewrite profile.

## Recommended policies

```text
MapGeometryPolicy
- RectangleLayoutProfile
- NegativeOriginPolicy
- ZeroDimensionPolicy
- LocalContainmentPolicy
- MaximumDimensionPolicy
- OverflowPolicy

TheaterBindingPolicy
- TokenComparisonProfile
- AllowedTheaterProfiles
- UnknownTheaterPolicy
- EditorCompatibilityFallbacks
```

No policy is implicit.
