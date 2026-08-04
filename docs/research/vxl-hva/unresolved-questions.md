# Unresolved questions and decision gates

> These questions are intentionally not answered by choosing the most convenient public implementation.

## 1. VXL header and section metadata

### Q1. Are zero-section VXL files legal stock inputs?

- Structurally representable.
- Common readers assume at least one section.
- Needs synthetic safety coverage and local occurrence count.

### Q2. Must section-header and tailer counts always match?

- XCC validates equality.
- The format carries two independent counts.
- Until local evidence, mismatch is a structured failure without discarding either raw count.

### Q3. What fixed values, if any, are required in section-header metadata?

- `Unknown1` is commonly 1.
- VSE/vengi write `Unknown2` as 2.
- older names/comments sometimes call the last field zero.
- Local aggregate categories are required before imposing constants.

### Q4. Is the embedded palette used by stock TS/RA2/YR rendering?

- Presence is confirmed.
- Runtime may use external palette/VPL data.
- Parser retains it; renderer policy remains separate.

## 2. VXL span contract

### Q5. Must the start and end empty sentinels always both be `-1`?

Convergent writers say yes. Local audit should count inconsistent pairs before compatibility promotion.

### Q6. Is span end always inclusive?

XCC and vengi writers plus XCC's size helper strongly support inclusive end. Require local exact-consumption validation across sections.

### Q7. Does stock decoding require exact end-range exhaustion, exact logical Z completion, or only one?

Historical readers often ignore the end table and loop on Z. Proposed strict Core behavior requires both, but this is a golden-gated defensive contract.

### Q8. Must the trailing duplicate count equal the leading voxel count?

Writers duplicate it; readers frequently skip it. Its stock validation role is unknown.

### Q9. Is a final `skip>0,count=0,duplicate=0` chunk universal/legal?

Writers use it for trailing empty Z. Audit should report occurrence and exact position without publishing bytes.

### Q10. Are duplicate or overlapping nonempty column ranges ever intentional sharing?

No confirmed reference/dependency semantics were found. Initial behavior rejects partial overlap and leaves exact aliases unresolved.

### Q11. Can column ranges be nonmonotonic while remaining valid?

Explicit offsets permit it. Disjoint nonmonotonic placement should remain decodable; local evidence should determine whether a warning is useful.

## 3. Scale, transform and bounds

### Q12. What is the semantic meaning of the VXL tailer `scale/det` float?

Sources call it scale, determinant or a constant. Values and composition behavior require local/runtime evidence.

### Q13. How is the VXL tailer 3×4 transform used relative to HVA?

Possibilities include base transform, pivot transform, override or composition. This dossier retains both raw records and chooses none.

### Q14. What units and inclusivity rules apply to min/max bounds?

Editor descriptions mix voxel extents, positioning and canvas scaling. No parser-level rewrite is permitted.

### Q15. Are zero, negative, singular or non-orthonormal transforms accepted by stock runtime?

OpenRA rejects noninvertible HVA expansion, while raw tools may accept it. Treat as semantic diagnostics until evidence.

## 4. Normal tables

### Q16. Are normal selector values other than 2 and 4 used by TS/RA2/YR assets?

Community material mentions other generations/table sizes. The target family must not automatically reinterpret them.

### Q17. What approved source and canonical hash should define the 36 and 244 vector tables?

A later implementation needs licensing/provenance approval and independent verification. This PR records only counts and source blobs.

### Q18. What coordinate basis do the normal vectors use?

Axis conversion belongs to an adapter, but the table definition must state its original basis before transformation.

## 5. HVA structure and ordering

### Q19. Is the first 16-byte HVA field ever a required signature?

Evidence includes blank, filename and `NONE`; treat as raw label unless contrary stock evidence appears.

### Q20. Are zero-frame or zero-section HVA files legal?

XCC rejects them; VSE creates a blank frame after initialization. Structural safety is distinct from stock legality.

### Q21. Is transform-record order frame-major or section-major?

- Frame-major: OpenRA, VSE, vengi, `cnc-formats`.
- Section-major: XCC accessor/CSV writer.
- Required evidence: a multi-frame, multi-section local sample whose candidate mappings differ, plus independent consumer/model consistency.

This is the highest-priority unresolved question.

### Q22. Does the raw matrix use row-vector or column-vector multiplication?

The file only fixes sequential values. Multiplication convention belongs to the consuming engine/adapter.

### Q23. Is translation the fourth value of each raw row and what units does it use?

Strong community/editor evidence says yes, but scale/composition units remain unresolved.

### Q24. What trailing-data policy did stock runtime use?

XCC requires exact size. Initial reader should be strict; any extension requires explicit evidence.

## 6. Section binding

### Q25. Is name comparison byte-exact, ASCII case-insensitive, or engine-specific?

Strict design uses unique exact names. Case folding remains experimental.

### Q26. Does stock runtime fall back to section ordinal when names do not match?

vengi does for import convenience. This is not sufficient evidence for the game.

### Q27. What happens with duplicate section names?

No winner should be selected without evidence. Local audit should count duplicates and binding ambiguity.

### Q28. How are HVA/VXL section-count mismatches handled?

Preserve unmatched records; resource/runtime consequence unresolved.

### Q29. Is HVA mandatory for every voxel role?

Community documentation says stock voxel resources require HVA, while modern importers tolerate absence. This belongs to resource policy, not file parsing.

## 7. Runtime/rendering boundaries

### Q30. How are VXL transform, HVA transform and simulation facing composed?

Requires a separate transform-composition dossier or implementation evidence.

### Q31. What is the exact Westwood-to-engine axis/handedness mapping?

OpenRA, VSE and vengi use different target conventions. A future adapter must validate transformed basis points without publishing geometry.

### Q32. How do bounds, depth ordering, projection and slope tilt interact?

These are renderer/simulation questions and must not delay binary parsing, but they must be resolved before visual compatibility claims.

## 8. Source gaps

### Q33. Which exact XCC SourceForge SVN revision corresponds to the pinned GitHub code?

SourceForge release 1.46 and GPLv2 project metadata are locatable; no byte-equivalent revision mapping is claimed.

### Q34. What is the repository-wide license for the public Voxel Section Editor III mirror?

No clear root license was located. Keep all code reference-only.

### Q35. Is a public Chrono Divide VXL/HVA parser available?

Only the public mod SDK was located. Do not infer engine behavior from closed/publicly absent components.

### Q36. Does ModdingWiki have a dedicated stable page for this Westwood VXL/HVA family?

No dedicated page was located during this research. Record absence rather than cite unrelated KVX/VXL formats.

## 9. Decision levels

### Level A — confirmed implementation default

Requires:

- at least two meaningfully independent public sources;
- distinguishing synthetic fixtures;
- multiple sanitized local samples across roles;
- no dependency on clipping, padding, ignored bytes or hidden fallback;
- stable Memory/Stream/MIX-window results.

### Level B — explicit experiment

One strong public implementation plus consistent local samples may justify a named, default-off strategy with separate hashes/diagnostics.

### Level C — unresolved

If success requires permissive repair, multiple candidates remain equally plausible, or only indistinguishable samples exist, retain raw data and do not promote compatibility.

### Level D — family/classification issue

If samples require incompatible layouts, first investigate wrong file family, resource misrouting, section misclassification or corruption. Do not add role-specific parsing hacks.

## 10. Prohibited shortcuts

Do not:

- use 804-byte VXL parsing because one modern library does;
- ignore span end tables because OpenRA displays files;
- accept duplicate counts without recording them;
- clamp Z runs or pad missing voxels;
- choose frame-major solely by source count;
- choose section-major solely because XCC displays assets;
- bind by index after a name miss without an explicit strategy;
- transpose into Unity and call the resulting orientation a file fact;
- synthesize HVA identity transforms;
- use object role to select a different binary decoder;
- expose original geometry to settle a public research question.
