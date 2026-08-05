# Missing Preview sections, hidden placeholders, and fabrication boundaries

> Source notice: researched by **ChatGPT Web** from public sources; no local `ProjectBaseline`; not a Codex Agent product; no code copied, translated, mechanically rewritten, or imported (`code_imported: false`).

## 1. Missing-state matrix

Core reports source state before any consumer fallback:

| `[Preview]` | `[PreviewPack]` | Core status |
|---|---|---|
| absent | absent | `BothSectionsMissing` |
| present | absent | `MetadataOnly` |
| absent | present | `PayloadOnly` |
| empty | empty | `BothSectionsEmpty` |
| valid | empty | `PayloadEmpty` |
| invalid | present | `MetadataInvalid` |
| valid | invalid | `PayloadInvalid` |
| valid | valid and exact | `SourcePreviewAvailable` |

No status is silently converted into a source preview.

## 2. WAE dummy preview behavior

WAE's writer states that Steam TS and RA2/YR can crash when a map has no preview and that the preview sections need to be first. If either section is missing, it injects:

```text
[Preview]
Size=0,0,106,61
```

plus a fixed two-line `PreviewPack` payload.

This proves a WAE editor compatibility strategy. It does not prove the exact behavior of every original executable, every patch, every launcher, or every map format variant.

The inserted data is not the map's rendered preview. It is a fabricated placeholder/hidden preview.

## 3. CnCNet hidden-preview recognition

CnCNet's extractor recognizes the first fixed fragment used by the WAE placeholder and returns no preview image. This demonstrates a consumer-level convention around the placeholder.

Core may expose a diagnostic candidate such as:

```text
KnownHiddenPreviewPayloadCandidate
```

but must still preserve and decode the original bytes according to policy. Recognition must not silently replace the source with “missing.”

## 4. Other public-tool behavior

- MapTool returns no bitmap when PreviewPack keys are absent or dimensions are invalid.
- CnCNet returns null for missing payload or invalid metadata.
- CNCMaps can generate and inject a preview from a rendered bitmap.
- FinalAlert can generate a preview from the map editor's minimap representation.
- PPM discussions and tool release notes describe preview injection as a separate operation.
- MapResize explicitly states that it does not update the preview and directs users to an editor or renderer.

These are different consumer/editor policies, not one format rule.

## 5. Required architecture split

### Core parser

Can:

- report missing sections;
- report inconsistent layers;
- parse a present source preview;
- identify known placeholder candidates without changing bytes;
- preserve all source material.

Cannot:

- render a map;
- create a black image;
- reuse an old preview;
- copy a preview from another file;
- inject hidden bytes;
- claim a fabricated preview existed in the source.

### Editor adapter

May, under explicit user action or save policy:

- regenerate from current map rendering;
- create a placeholder;
- remove a preview;
- keep the existing compressed bytes;
- refuse to save for a target compatibility profile.

The action is recorded as generated content with tool/version/provenance.

### Runtime UI adapter

May:

- show a generic icon;
- show “preview unavailable”;
- omit a map entry;
- asynchronously generate a cache thumbnail;
- display a recognized hidden preview as blank.

None of these alter the parsed document.

## 6. Metadata/payload mismatch

A valid metadata tuple with missing or failed payload is not an empty image. A valid payload with absent dimensions is not automatically decoded using guessed dimensions.

Forbidden guesses include:

- fixed 106×61;
- common game menu size;
- dimensions derived from `Size` or `LocalSize`;
- square-rooting `decodedLength / 3`;
- selecting dimensions that produce a plausible aspect ratio;
- reading dimensions from a consumer cache.

## 7. Stale and misleading previews

A structurally valid PreviewPack can depict:

- an older version of the map;
- a different map;
- instructions or advertising;
- a deliberately hidden image;
- a manually edited image;
- a rendered map with missing overlays or objects.

This is not a parse failure. Discrepancy checking belongs to optional later validation and cannot repair map content.

## 8. Source identity and generated identity

A future system must distinguish:

- `SourcePreviewDocument`;
- `GeneratedPreviewArtifact`;
- `CachedThumbnail`;
- `FallbackPlaceholder`;
- `ConsumerDisplayImage`.

Each has separate hashes and provenance. A generated artifact never inherits the source PreviewPack identity.

## 9. Save policy

Default lossless save policy:

- if source sections are unchanged, preserve them exactly;
- if no source preview exists, do not add one automatically;
- if an explicit target profile requires a preview, fail with a requirement diagnostic unless the caller supplies a generation policy;
- never reuse decoded bytes with altered dimensions;
- never label placeholder bytes as a generated map rendering.

## 10. Compatibility claims

The following remain separate:

- parser accepts missing preview;
- FinalAlert reopens the file;
- CnCNet lists the map;
- original TS/RA2/YR menu accepts the map;
- gameplay can start;
- preview is displayed correctly.

No one result implies the others.

## 11. Normalized evidence status

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| WAE injects a fixed 106×61 dummy preview when sections are missing | `ImplementationSpecificBehavior` | World-Altering Editor | Named editor compatibility behavior only. | Treat the inserted payload as generated content with explicit provenance. | `NotRun` |
| CnCNet recognizes the WAE payload and suppresses visible preview output | `ImplementationSpecificBehavior` | CnCNet XNA client | Named consumer behavior only. | Preserve source bytes while reporting a hidden-placeholder candidate. | `NotRun` |
| Missing Preview causes every original executable to crash or requires the fixed dummy payload | `Underconfirmed` | WAE comment and community reports | Candidate compatibility concern without original-runtime source. Exact affected versions and launch paths are unresolved. | Do not promote the dummy payload to a standard runtime representation. | `NotRun` |
| The fixed dummy payload is the original-runtime standard missing-preview value | `Unresolved` | No original-runtime source located | Cooperation between WAE and CnCNet establishes a tool convention, not a runtime standard. | Keep source absence, generated placeholder, and consumer fallback as separate states. | `NotRun` |
| Core reports missing/inconsistent source and never fabricates source bytes | `DefensiveDesign` | Project policy | Preservation and fail-closed architecture rule. | Generation remains an explicit editor/UI adapter action. | `NotRun` |

## 12. Explicit exclusion

This dossier generates no image, placeholder, Texture, bitmap, preview, or map content.
