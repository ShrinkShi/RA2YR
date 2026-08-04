# OverlayData raw byte and semantic profiles

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Format-wide fact

The ordinary `OverlayDataPack` storage element is one raw byte aligned with the same storage index in `OverlayPack`.

The only format-wide safe model is:

```text
OverlayDataRaw: uint8
```

No single semantic name is valid for every Overlay family.

## 2. Why “frame index” is insufficient

Several tools name the byte `FrameIndex`, `frame`, or `overlay_value`:

- WAE materializes `Overlay.FrameIndex`;
- CNCMaps creates an Overlay object with ID and value;
- MapTool stores `FrameIndex`;
- ModEnc describes it as the frame index for an Overlay type.

This is strong community/tool evidence for image-frame-oriented handling. It does not prove that every runtime system treats the byte only as a passive frame number.

Hardcoded or type-specific systems may interpret the same byte as or derive from:

- resource stage or density;
- connected-wall visual state;
- bridge piece/frame/state;
- animation phase;
- damage presentation;
- variant/facing;
- editor-only frame choice;
- unknown metadata.

## 3. Required interpretation pipeline

```text
OverlayTypeRaw + OverlayDataRaw
→ Overlay registry binding
→ Overlay family classification
→ candidate semantic profiles
→ evidence-gated selected profile
→ derived values and diagnostics
```

The raw byte survives every stage.

## 4. Candidate semantic profile model

```text
OverlaySemanticProfile
- ProfileId
- ApplicableOverlayFamilies
- ApplicableGameOrExtension
- RawDomain
- DerivedFields
- ValidationRules
- EvidenceGrade
- SourcePins

OverlaySemanticResult
- OverlayTypeBinding
- OverlayDataRaw
- ProfileCandidates
- SelectedProfile
- DerivedValues
- Diagnostics
- RawToDerivedTrace
```

A missing profile is a valid result: `UnknownSemanticProfile`.

## 5. Candidate families

### 5.1 Generic image-frame profile

Candidate interpretation:

```text
RenderedFrameCandidate = OverlayDataRaw
```

This is useful for ordinary decorative overlays and editor rendering, but must validate against the bound Art resource only after binding. An out-of-range image frame does not authorize clamping or changing the raw byte.

Evidence: `CommunityDocumented` and multiple tool implementations.

### 5.2 Resource profile

The byte may participate in a resource stage/density model that also influences displayed frame. It is not automatically the remaining credit value or harvest amount.

Evidence: editor/resource calculations and community behavior; final RA2/YR runtime mapping remains `Unresolved`.

### 5.3 Connected-overlay profile

For walls/fences or editor-defined connected overlay sets, a frame can be selected from neighboring cells. WAE explicitly recomputes a frame when connected overlays are placed or updated.

This proves an editor-derived frame workflow, not that the original runtime always recomputes or always trusts the stored byte.

### 5.4 Bridge profile

The data byte can select a bridge piece/frame or encode a state that participates in hardcoded bridge logic. High and low bridge families do not share one simple rule.

Evidence: editor placement behavior and community reverse engineering; original runtime detail remains partly unresolved.

### 5.5 Unknown or extension profile

An extension may define a custom meaning. Unknown types and values remain raw and are never coerced to the nearest known profile.

## 6. Empty-type combinations

The consistency layer must distinguish:

| Type raw | Data raw | Classification |
|---|---:|---|
| `0xFF` | `0x00` | conventional empty cell candidate |
| `0xFF` | nonzero | empty type with residual/unknown data |
| bound type | `0x00` | valid zero data under type-specific profile |
| bound type | nonzero | typed raw data requiring profile |
| unknown type | any | unknown type/data pair |
| missing type array | any data | unpaired data byte |

`0xFF + nonzero` is not cleaned automatically.

## 7. Validation versus repair

A semantic profile may report:

- raw value is within documented range;
- raw value exceeds available Art frames;
- profile does not apply to the bound type;
- required neighbor/context data is absent;
- multiple profiles remain plausible;
- derived resource/bridge/wall interpretation is incomplete.

It may not:

- clamp to the last image frame;
- replace data with zero;
- erase data when type is empty;
- rewrite neighbors;
- infer a type from the value;
- mark partial interpretation as exact runtime semantics.

## 8. Same raw value, different meaning

The value `3` may mean different things for different types:

- image frame 3;
- resource stage 3;
- one of several connected-wall frames;
- a bridge orientation/piece frame;
- an extension-defined state.

Therefore caches and equality must include the bound type and profile, not only `OverlayDataRaw`.

## 9. Evidence grades for semantic names

Recommended labeling:

- `OverlayDataRaw`: format-level raw fact;
- `FrameIndexCandidate`: `CommunityDocumented` / implementation evidence;
- `ResourceStageCandidate`: family-specific evidence;
- `NeighborMaskCandidate`: unresolved unless pinned per type/profile;
- `BridgeStateCandidate`: family-specific, partly community-documented;
- `RuntimeHealth`: not present as a general raw field;
- `Owner`: not present in the two arrays;
- `Passability`: derived from type/rules/simulation, not the raw data byte alone.

## 10. Roundtrip requirement

Even when a selected semantic profile can interpret the byte, a lossless model retains the original raw byte and does not regenerate it from derived fields unless an explicit writer profile is selected.

Semantic roundtrip and byte-identical roundtrip are separate claims.