# Legal and licensing boundaries

> This is engineering license research, not legal advice. Final dependency and patent decisions require project-owner/legal review.

## 1. LZO and miniLZO

The official LZO project distributes LZO 2.10 under GPL version 2 or later, with commercial licensing available. miniLZO is a generated/small subset of the same project and carries the same licensing route.

Consequences:

- do not copy miniLZO into a non-GPL Core;
- do not mechanically translate it to C#;
- do not preserve its control-flow structure under renamed variables;
- dynamic/native linking implications require legal review rather than assumption;
- commercial licensing is a possible but separate procurement decision.

## 2. Reference implementations in reviewed projects

| Source | Boundary |
|---|---|
| OpenRA `LCWCompression.cs` | GPL-3.0-or-later |
| OpenRA `LZOCompression.cs` | GPL-3.0-or-later plus miniLZO GPL lineage |
| WAE `Format80.cs` / `Format5.cs` | GPL-3.0-or-later |
| WAE `MiniLZO.cs` | explicitly a mechanically converted miniLZO 2.06 port; GPL |
| XCC Utilities | GPL-2.0 lineage |
| EA FinalAlert/FinalSun release | GPL-3.0; integrates bundled third-party/XCC sources |

These are behavior references only.

## 3. Allowed use of GPL references in this research

Allowed:

- command masks and byte-field facts;
- documented input/output contracts;
- observed diagnostics and permissive behavior;
- interoperability test outcomes;
- independent fixture specifications;
- high-level state invariants.

Not allowed:

- copied code;
- line-by-line translation;
- near-identical branching/control flow;
- mechanically generated C#;
- comments or identifier sequences that reproduce the implementation;
- using a GPL encoder as the fixture oracle inside the repository without explicit license acceptance.

## 4. Permissive candidate backends

`AxioDL/lzokay` is an MIT-licensed C++ LZO1X implementation pinned for review at `db2df1fcbebc2ed06c10f727f72567d40f06a2be`. `encounter/lzokay-rs` provides an MIT pure-Rust LZO1X implementation.

These are candidates, not approved dependencies. A later review must pin:

- exact commit/package version;
- complete license text and notices;
- transitive dependencies;
- unsafe/native code surface;
- supported platforms and Unity build targets;
- exact-consumption/error API;
- maintenance/security history;
- deterministic test results.

A native plugin may be architecturally acceptable but is not selected in this research.

## 5. Independent implementation route

An independent decoder is potentially feasible because the required output can be described through:

- public command/bitstream facts;
- strict state invariants;
- independently authored synthetic fixtures;
- differential aggregate results;
- no GPL source access during implementation, where practical.

Recommended clean-room discipline:

1. research dossier freezes facts and tests;
2. implementer works from dossier/fixtures, not GPL source;
3. reviewer checks behavior, not structural similarity;
4. provenance records all consulted materials;
5. no copied comments or control-flow skeletons.

## 6. Format80 licensing

The command format itself is publicly described by multiple sources. Historical implementations are mostly GPL or unclear-license. The project should independently implement the small state machine from the documented command table and independent fixtures rather than porting XCC/OpenRA/WAE.

## 7. Patent status

No authoritative patent-clearance conclusion was established in this research. Do not state that all LZO patents have expired or that the algorithm is patent-free. Dependency selection must include legal review appropriate to distribution jurisdictions and project goals.

## 8. Documentation licenses

Wiki/forum text is paraphrased. URLs and revision identifiers are recorded. No substantial source prose is reproduced.

## 9. Required later decision record

Before implementation merges, record:

- selected backend or independent implementation route;
- SPDX identifiers;
- exact versions;
- notice obligations;
- native binary distribution plan;
- security ownership;
- patent/legal review outcome;
- fallback/removal plan.
