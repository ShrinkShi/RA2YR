# Third-party sources

`docs/third-party/sources.yml` is the authoritative, machine-readable ledger.
This file summarizes the review rules and the currently known sources.

No source code from the external reference projects listed below has been
copied into this repository. A `reference-only` entry permits observation of
public formats or behavior; it does not permit code import.

| Source | License | Use | Code in repository | Approval |
|---|---|---|---:|---|
| actions/checkout 11bd719 (v4.2.2) | MIT | CI dependency pinned to a full commit | Referenced action; not vendored | Approved CI dependency |
| Unity 2022.3 package set | Unity Companion License expected; per-package verification pending | Build dependency | No vendored source | Pending per-package audit |
| RedAlertCSF2JSON | GPL-3.0-only | External format/behavior reference only | No | Reference only |
| Ra2-Map-TriggerNetwork | MIT | External trigger-structure reference only | No | Reference only |
| VoxelShop | Apache-2.0 | External voxel behavior/tool reference only | No | Reference only |
| FinalAlert 2 YR 1.01 | Proprietary | External interoperability baseline only | No | Reference only |
| XCC SourceForge original source and SVN r1201 | GPL-2.0-only | MIX, PAL, and CSF format/behavior reference | No | Reference only |
| OmniBlade/xcc encoding commit `62bb770` | GPL-2.0-only | Pinned MIX, PAL, and CSF behavior comparison | No | Reference only |
| XCC Mixer toolbox redistribution | GPL-2.0-only | External black-box interoperability tool | No | Reference only |
| Bruce Schneier Blowfish definition | License-free | Standard initial-state constants and public vectors; independent implementation | No code; constants only | Approved data |
| OpenRA commit `a520984` | GPL-3.0-or-later | Independent MIX/encryption and PAL conversion cross-check | No | Reference only |
| iron-curtain-engine/cnc-formats commit `77da596` | MIT OR Apache-2.0 | Independent PAL and CSF layout cross-check | No | Reference only |
| LewisXY CSF tool revision `ba6046f` | MIT | Independent CSF marker, length, and text-representation cross-check | No | Reference only |
| RA2 DIY 2025 tutorial bundle | License unverified | `CommunitySemanticReference` for later stock/extension/unresolved Rules, Art, and AI hypotheses | No | Reference only; never a runtime content source |

The pinned OpenRA commit contains no CSF implementation or CSF format
documentation and is not used as WP-02E CSF evidence.

## Import gate

Before any external code, generated source, binary library, or substantial
documentation enters the repository, add or update a ledger entry with:

- the canonical upstream source;
- the exact release, commit, and SHA-256 where applicable;
- the governing license and required notices;
- whether the source contains code;
- the intended use and integration method;
- an explicit approval decision.

GPL-licensed code is not eligible for import under the current Apache-2.0
repository policy. Publicly documented facts and independently observed
behavior must be reimplemented without copying protected implementation text.
