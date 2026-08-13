# M4-C4 commands, missions, targeting, and autonomy

M4-C4 adds declarative command, runtime mission, perception, target, and
autonomy arbitration contracts above C1-C3. It is a deterministic project
enhancement and does not claim stock YR mission or AI parity.

All Human, ComputerAI, Script, Trigger, and Internal sources produce a typed
`CommandRequest`; none writes the world directly. Queue replace/append, stable
command IDs, source identity, bounded capacity, and canonical actor/ID ordering
are explicit. Authored Mission text is retained verbatim in
`RuntimeMissionSnapshot` rather than rewritten into a runtime enum.

Perception uses the C2 spatial index and bounded radius queries. Target scoring
is profile-driven and deterministic, with current/last target memory and
hysteresis to avoid per-tick switching. `ActionArbitrationSystem` resolves one
stable proposal per actor; player/forced commands have explicit priority.

Manual autonomy disables autonomous acquisition, kite, and autonomous movement;
Assisted may acquire but does not kite; StrictHold and TacticalHold remain
separate policies. No combat, damage, harvest, capture, transport, renderer,
UI, or Unity authority is introduced.
