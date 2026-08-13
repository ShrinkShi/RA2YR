# M5-C4 structures and placement foundation

M5-C4 adds Unity-free raw structure definitions, explicit rectangular
footprint placement candidates, checked power projections, and diagnostic
repair/sell/capture/deploy interaction candidates. Placement preserves the
requested origin and fails on bounds/overlap; it never clamps, repairs,
occupies a map, creates an actor, or mutates credits.

Power, health, ownership, capture, deployment, and interaction policies remain
explicit candidates. No structure renderer, Unity object, pathfinding,
scenario trigger, ProjectBaseline packed data, or original-runtime building
compatibility is claimed.
