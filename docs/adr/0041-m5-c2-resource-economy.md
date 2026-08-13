# ADR-0041 - M5-C2 resource economy foundation

Resource cell raw fields, quantity/value candidates, harvester cargo, and
refinery capability are separate Core contracts. Interpretation profiles are
explicit; checked arithmetic and bounded diagnostics fail closed. No default
resource family, storage behavior, docking policy, or runtime harvest timing
is inferred. M5-C2 is synthetic/project-enhancement evidence only and does
not read ProjectBaseline packed data or claim original-runtime compatibility.
