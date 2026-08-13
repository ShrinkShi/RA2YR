# M5-C2 resource economy foundation

M5-C2 adds Unity-free, bounded resource contracts for raw resource cells,
explicit quantity/value candidates, harvester capacity/cargo snapshots, and
refinery capability/docking descriptors. Raw fields remain separate from
derived candidates and future runtime state.

The only quantity/value formula exposed by Core is an explicit synthetic
`OverlayDataPlusOne` / `RulesResourceValue` profile. It is an editor-style
candidate, not a claim about stock Yuri's Revenge depletion, harvest timing,
rounding, or credit settlement. Cargo and refinery validation is bounded,
checked, deterministic, and diagnostic-driven; no movement, queue, docking
state machine, Unity actor, UI, renderer, or ProjectBaseline packed data is
read.

M5-C1 remains the authority for credit transactions. M5-C2 does not mutate
credits and does not implement harvester/refinery runtime behavior.
