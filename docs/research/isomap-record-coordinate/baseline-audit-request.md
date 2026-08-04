> **Source notice:** This research was prepared by **ChatGPT Web** from public sources. It did not read the local ProjectBaseline and is not the product of the local Codex Agent. No GPL or unclear-license code was copied, translated, mechanically rewritten, or imported (`code_imported: false`).

# Future ProjectBaseline sanitized golden audit request

ChatGPT Web did not access the local ProjectBaseline. This document specifies a later read-only audit for local Codex.

## Objective

Collect aggregate evidence that can:

- classify actual decoded IsoMapPack5 record widths and trailers;
- distinguish dense and sparse candidate streams;
- quantify tile-field high-half use without exposing tile sequences;
- validate coordinate-domain formulas without publishing coordinates;
- measure SubTile, Level, and final-byte ranges;
- test theater registry binding while protecting asset identities;
- verify deterministic Memory/Stream/MIX-window behavior;
- keep ProjectBaseline observations separate from original-runtime claims.

The audit does not decide stock runtime behavior by itself.

## Read-only constraints

The audit must:

- use the already configured authoritative ProjectBaseline scope;
- apply the project's existing exclusions before enumeration;
- avoid FinalAlert, tutorial/reference, unpacked/export, cache, and unrelated-installation directories;
- open content read-only;
- not launch RA2/YR, Unity, FinalAlert, XCC, or any GUI;
- not modify timestamps, archives, maps, INIs, or indexes;
- not write beside original content;
- store private detailed results only in the approved local audit workspace;
- publish only the aggregate schema below.

## Selection matrix

Select by logical characteristics, not by publishing map names.

### Theater coverage

At least one suitable map candidate for each available profile:

- Temperate;
- Snow;
- Urban;
- NewUrban;
- Desert;
- Lunar.

If a profile has no qualifying sample, report `NoSampleAvailable` without broadening to excluded directories.

### Map origin and role

Where locally available and permitted:

- official base-game map candidate;
- official expansion map candidate;
- official map-pack/additional-map candidate;
- user/mod map candidate only as a separate group;
- small, medium, and large full dimensions;
- low and high packed compression ratios;
- flat-heavy and elevation-heavy candidates;
- candidates containing terrain near ramp, cliff, water, shore, and bridge roles, reported only as selection categories.

### Record characteristics

Seek aggregate coverage for:

- dense candidate;
- sparse candidate;
- full32 high16 all-zero candidate;
- nonzero high16 candidate, if any;
- SubTile zero and nonzero;
- multi-cell TMP binding candidate;
- Level zero and multiple nonzero levels;
- final byte zero and nonzero;
- decoded length divisible by 11;
- decoded length remainder 4;
- any other remainder, if encountered;
- missing TMP or reserved registry-range candidate;
- multiple LZO blocks and single-block streams.

Do not force a sample into a category by editing it.

## Private audit pipeline

```text
selected logical map source
→ lossless [IsoMapPack5] fragment occurrences
→ strict Base64 result
→ bounded chunk-envelope descriptors
→ exact raw LZO1X-compatible decoded stream
→ 11-byte raw record slices plus trailer
→ coordinate aggregate analysis
→ tile-field aggregate views
→ frozen theater registry binding aggregates
→ input-mode equivalence hashes
```

The private implementation may retain detailed records locally for computation, but the public report must never emit them.

## Publicly allowed output

### Selection and provenance

- `SelectionBasis` category;
- theater profile category;
- provider/archive logical provenance without absolute path;
- official/additional/user classification;
- logical section name;
- sample-group identifier generated for the audit;
- tool and audit version;
- policy/profile IDs.

### Packed and decoded structure

- fragment count;
- aggregate Base64 character count;
- Base64-decoded length and SHA-256;
- chunk count;
- compressed-size min/max/sum;
- declared uncompressed-size min/max/sum;
- compressed/uncompressed ratio range;
- decoded stream length and SHA-256;
- full 11-byte record count;
- remainder length;
- trailer classification and all-zero/nonzero category;
- whether trailer lies wholly in final chunk output;
- no payload bytes.

### Coordinate aggregates

- X raw unsigned min/max;
- Y raw unsigned min/max;
- signed-view negative counts;
- in-domain record count;
- out-of-domain count;
- parity-invalid count;
- distinct in-domain coordinate count;
- duplicate group count;
- byte-identical and conflicting duplicate counts;
- missing-coordinate count;
- dense/sparse/ambiguous classification;
- source-order classification category;
- canonical aggregate hashes of domain classifications.

No coordinate pair or sequence may be published.

### Tile-field aggregates

- low16 min/max;
- high16 min/max;
- count with high16 zero/nonzero;
- full32 signed-negative count;
- count where full32 differs from low16;
- count where candidate interpretations bind to zero, one, or multiple registry ranges;
- aggregate hash of interpretation classifications;
- no tile sequence or per-record values.

### Final bytes and binding aggregates

- SubTile min/max and bounded histogram summary;
- Level min/max and bounded histogram summary;
- final-byte zero/nonzero count and bounded frequency summary;
- registry binding success/failure counts;
- out-of-range/reserved/missing-TileSet/missing-TMP counts;
- SubTile valid/out-of-range/empty-slot counts;
- variation-candidate count categories;
- binding-result aggregate hash;
- diagnostics by stable identifier and severity;
- Memory/Stream/short-read/MIX-window equivalence result and hashes.

## Forbidden public output

Never publish:

- map name or filename;
- absolute or user-relative path;
- username;
- map INI text;
- Base64 fragment text;
- compressed bytes;
- decoded bytes;
- record bytes;
- per-record tuple;
- coordinate pair or ordered coordinate sequence;
- tile ID or ordered tile sequence;
- GlobalTileId-to-TileSet/TMP/filename mapping;
- complete TileSet or TMP filename list;
- TMP data, offsets, pixels, depth, or cell tuples;
- OverlayPack or OverlayDataPack arrays;
- Preview pixels;
- screenshots or rendered images;
- meshes or geometry;
- per-record or per-resource hashes;
- hex/Base64 dumps;
- information sufficient to reconstruct a map.

## Audit result schema

Suggested public record per sample group:

```text
SelectionBasis
TheaterProfile
LogicalProvenanceClass
PackedSummary
  FragmentCount
  Base64DecodedLength
  Base64DecodedSha256
  ChunkCount
  CompressedLengthRange
  UncompressedLengthRange
DecodedSummary
  Length
  Sha256
  RecordCount
  RemainderLength
  TrailerClass
CoordinateSummary
  RawRanges
  InDomainCount
  OutOfDomainCount
  DuplicateCounts
  MissingCount
  DensityClass
TileFieldSummary
  Low16Range
  High16Range
  NonZeroHigh16Count
  InterpretationBindingCounts
ByteSummary
  SubTileRangeAndHistogram
  LevelRangeAndHistogram
  FinalByteFrequencySummary
BindingSummary
  StatusCounts
  AggregateHash
DiagnosticsSummary
InputModeEquivalence
EvidenceGrade = ObservedByFutureProjectBaselineAudit
```

## Determinism checks

For every selected sample:

1. parse from a contiguous memory window;
2. parse from a seekable stream;
3. parse from a deterministic short-read stream;
4. parse from the original bounded MIX window where applicable;
5. randomize only external enumeration order, not source record order;
6. repeat coordinate indexing and registry binding;
7. compare canonical aggregate hashes and diagnostics.

All modes must produce identical raw-document, coordinate-analysis, and binding aggregate hashes. A mismatch is an implementation defect, not evidence of multiple formats.

## Candidate-profile comparisons

Without publishing records, run:

- unsigned and signed coordinate views;
- full32 tile view;
- low16 plus retained-high view;
- explicit axis-swap experimental view;
- exact-record-only and four-zero-trailer policies;
- dense expectation and sparse analysis;
- vanilla resource profile and explicit editor-compatibility resource profile.

Report aggregate outcomes side by side. Do not automatically choose the profile with the highest binding count.

## Budgets

The audit must configure and publish the limits used:

- maximum map candidates inspected;
- maximum decoded bytes per sample;
- maximum chunk count;
- maximum record count;
- maximum duplicate groups retained privately;
- maximum missing-coordinate materialization;
- maximum registry candidates;
- maximum diagnostics;
- maximum execution time per sample.

Budget failures are reported and the sample is not retried with unbounded settings.

## Required conclusions format

For each research question, output separately:

- `ObservedByFutureProjectBaselineAudit` aggregate observation;
- current `ConfiguredForProjectPolicy`;
- whether public source conflict remains;
- whether further original-runtime evidence is required.

Never relabel a ProjectBaseline observation as `ConfirmedByOfficialRuntimeSource`.

## Safety declaration for the future audit

The requested audit is read-only and aggregate-only. It does not start a game or editor, modify original content, extract or publish resources, create maps, or change repository compatibility status.
