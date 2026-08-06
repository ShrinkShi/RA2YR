# Map Size, LocalSize, and Theater

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. `code_imported: false`.

## Raw rectangles

Preserve four raw tokens for both `Size` and `LocalSize` before interpretation:

```text
Field0Raw
Field1Raw
Field2Raw
Field3Raw
```

The leading profile is `originX,originY,width,height`. Nonzero origins, negative/malformed values, overflow, containment conflicts and duplicate keys remain explicit.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes LocalSize and Theater controls and editor resize constraints | `ConfirmedByOfficialToolSource` | EA FinalSun / FinalAlert 2 | Official editor behavior; UI limits are not runtime format limits. | Named editor profile. | `NotRun` |
| WAE writes `Size=0,0,w,h` and handles all LocalSize fields | `ImplementationSpecificBehavior` | WAE | Named writer/reader behavior. | Do not force Size origins to zero. | `NotRun` |
| `x,y,width,height` is the leading rectangle candidate | `Underconfirmed` | Editors/tools/community | Runtime strictness and all field semantics are unsourced. | Explicit rectangle profile. | `NotRun` |
| A unique runtime interpretation for Size/LocalSize origins and containment | `Unresolved` | No original-runtime source located | Client/editor/map-domain uses differ. | Preserve raw and report candidates. | `NotRun` |
| Editor/client assumptions about visible, playable and camera rectangles | `ConflictingSources` | Official editor, clients and tools | These rectangles serve different layers. | Keep map domain, LocalSize, camera and UI bounds separate. | `NotRun` |
| Checked multiplication, no clamp/repair and raw rectangle preservation | `DefensiveDesign` | Project policy | Safety/preservation. | Reject derived allocation on invalid dimensions without rewriting source. | `NotRun` |

## Theater

Stock-token candidates include Temperate, Snow, Urban, NewUrban, Desert and Lunar. Exact token spelling and unknown values remain raw. Theater binding identifies logical resource/profile candidates only; it does not load TMP/palettes, choose weather/lighting, determine movement, or default unknown values to Temperate.

FinalAlert's theater UI is `ConfirmedByOfficialToolSource`; named tool fallback/extension behavior is `ImplementationSpecificBehavior`; stable stock token usage is `ConfirmedCommunityConvention` or `Underconfirmed` depending the claim; exact runtime fallback is `Unresolved`.

The `.ubn → .urb` fallback remains a named tool/editor compatibility behavior, never a vanilla default.
