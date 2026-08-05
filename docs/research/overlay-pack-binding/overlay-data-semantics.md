# OverlayData raw byte and semantic profiles

> **Research provenance:** This document was produced by ChatGPT Web from public sources. It did not read ProjectBaseline, is not a local Codex Agent artifact, and imports no code. GPL or unclear-license sources are reference-only; no source was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

## 1. Format-wide storage boundary

The ordinary `OverlayDataPack` storage element is one raw byte aligned with the same storage index in `OverlayPack`.

The safe raw model is:

```text
OverlayDataRaw: uint8
```

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert/FinalSun stores one OverlayData byte per ordinary-profile storage cell | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Confirms official-editor storage behavior, not every runtime semantic. | Preserve the byte exactly. | `NotRun` |
| Several public tools use the same one-byte ordinary storage model | `Underconfirmed` | OpenRA, WAE, CNCMaps, MapTool | Convergence is strong, but implementation independence and original-runtime applicability are not proven. | Keep the one-byte model behind the ordinary storage profile. | `NotRun` |
| One semantic name is valid for every Overlay family | `ConflictingSources` | Generic frame-oriented tools versus resource, connected-wall, bridge, and extension-specific behavior | Sources and families assign different or coupled meanings. | Require a type-specific semantic profile. | `NotRun` |
| Unknown bytes remain raw and are not normalized away | `DefensiveDesign` | Project policy | This is a lossless/fail-closed design decision. | Retain `OverlayDataRaw` through every stage. | `NotRun` |

## 2. Why “frame index” is insufficient

Several tools name the byte `FrameIndex`, `frame`, or `overlay_value`:

- WAE materializes `Overlay.FrameIndex`;
- CNCMaps creates an Overlay object with ID and value;
- MapTool stores `FrameIndex`;
- ModEnc describes it as the frame index for an Overlay type.

This supports a stable community/tool interpretation for image-frame-oriented handling, but it does not prove that every runtime system treats the byte only as a passive frame number.

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

## 5. Candidate families and normalized grades

### 5.1 Generic image-frame profile

Candidate interpretation:

```text
RenderedFrameCandidate = OverlayDataRaw
```

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| `OverlayDataRaw` is commonly exposed as a frame index candidate | `ConfirmedCommunityConvention` | ModEnc, WAE, CNCMaps, MapTool | Confirms stable community/tool naming and rendering convention, not universal runtime semantics. | Apply only through a named generic-frame profile. | `NotRun` |
| A named tool directly uses the byte as a frame/value | `ImplementationSpecificBehavior` | WAE, CNCMaps, or MapTool, considered separately | Each row establishes that tool's behavior only. | Retain source pins in the profile. | `NotRun` |
| Every Overlay type in stock RA2/YR treats the byte only as an image frame | `ConflictingSources` | Generic frame model versus resource/wall/bridge-specific evidence | Type-specific systems conflict with a universal frame-only interpretation. | Do not select globally. | `NotRun` |

An out-of-range image frame does not authorize clamping or changing the raw byte.

### 5.2 Resource profile

The byte may participate in a resource stage/density model that also influences displayed frame. It is not automatically the remaining credit value or harvest amount.

- public editor/tool behavior supplies `ImplementationSpecificBehavior` for the named implementation;
- the broader `ResourceStageCandidate` is `Underconfirmed` for stock RA2/YR;
- exact economy, remaining harvest value, and depletion behavior remain `Unresolved` without runtime evidence.

### 5.3 Connected-overlay profile

For walls/fences or editor-defined connected overlay sets, a frame can be selected from neighboring cells. WAE explicitly recomputes a frame when connected overlays are placed or updated.

```text
EvidenceGrade: ImplementationSpecificBehavior
Source: World-Altering Editor
AuditStatus: NotRun
```

This proves an editor-derived frame workflow, not that the original runtime always recomputes or always trusts the stored byte. A universal runtime neighbor-mask contract remains `Unresolved`.

### 5.4 Bridge profile

The data byte can select a bridge piece/frame or participate in a type-specific bridge state model. High and low bridge families do not share one demonstrated universal rule.

- WAE placement behavior is `ImplementationSpecificBehavior`;
- stable community bridge descriptions are `ConfirmedCommunityConvention` for the documented convention only;
- exact stock-runtime bridge-state semantics remain `Underconfirmed` or `Unresolved`;
- conflicting claims that the byte is always a passive frame or always a complete bridge state are `ConflictingSources`.

### 5.5 Unknown or extension profile

A named extension may define a custom meaning. That behavior is `ImplementationSpecificBehavior` for the extension profile unless stronger evidence exists.

Unknown types and values remain raw and are never coerced to the nearest known profile. This preservation is `DefensiveDesign`.

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

`0xFF + nonzero` is not cleaned automatically. That rule is `DefensiveDesign`.

## 7. Validation versus repair

A semantic profile may report:

- raw value is within documented range;
- raw value exceeds available Art frames;
- profile does not apply to the bound type;
- required neighbor/context data is absent;
- multiple profiles remain plausible;
- derived resource/bridge/wall interpretation is incomplete.

The following are `DefensiveDesign` prohibitions. A profile may not:

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

## 9. Evidence summary for semantic names

| Semantic claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| `OverlayDataRaw` is an ordinary-profile raw byte | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Storage fact for the official editor, not universal semantic meaning. | Preserve raw. | `NotRun` |
| `FrameIndexCandidate` is a stable tool/community interpretation | `ConfirmedCommunityConvention` | ModEnc and public tools | Runtime universality not established. | Named opt-in profile. | `NotRun` |
| `ResourceStageCandidate` | `Underconfirmed` | Editor/tool resource behavior and community knowledge | Stored stage, rendered frame, value, and harvest amount must remain separate. | Type-specific profile only. | `NotRun` |
| `NeighborMaskCandidate` is the universal stored wall representation | `Unresolved` | No original-runtime source located | WAE demonstrates derived frame recomputation, not a universal raw bitmask. | Preserve raw and derive connectivity separately. | `NotRun` |
| `BridgeStateCandidate` is fully defined by one byte | `Underconfirmed` | Community and editor evidence | Bridge object/state, TMP art, occupancy, damage, and pathfinding remain separate. | Type-specific profile only. | `NotRun` |
| `RuntimeHealth`, `Owner`, or `Passability` is a general raw field | `Unresolved` | No supporting format-wide evidence | These are not established as general fields in the two arrays. | Do not expose them from the raw parser. | `NotRun` |

## 10. Roundtrip requirement

Even when a selected semantic profile can interpret the byte, a lossless model retains the original raw byte and does not regenerate it from derived fields unless an explicit writer profile is selected.

Semantic roundtrip and byte-identical roundtrip are separate claims.

```text
AuditStatus: NotRun
FutureEvidenceSource: ProjectBaselineAggregateAudit
```
