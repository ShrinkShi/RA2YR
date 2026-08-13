# ADR-0031: M4 deterministic simulation and agent boundaries

Status: Accepted as M4 governance and project-enhancement architecture.

M4 adopts a Unity-free deterministic data-oriented ECS as the authoritative
simulation boundary. Content/format models feed simulation descriptors; a
presentation adapter consumes immutable snapshots and events. Unity frame rate,
GameObject identity, Unity physics, and render state cannot define gameplay.

Execution uses one logical authority with read-only proposal computation and a
deterministic commit barrier. Parallel work is optional and must produce the same
state hash as the sequential reference through stable ordering and explicit
tie-breakers.

Unit autonomy is a first-class enhancement with Manual/Assisted/Automatic modes,
capability resolution, and an explicit AutonomyEnvelope. Computer AI can only
observe legal immutable data and submit validated commands. RuleBased remains a
fallback; Neural/Hybrid are future policy contracts, not simulation authorities.

This decision does not promote M3 evidence. M3 ProjectBaseline remains a patched
development corpus with unresolved terrain runtime binding, and M4 synthetic
simulation evidence cannot prove original YR runtime compatibility.
