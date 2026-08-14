# M5-C6 integrated headless skirmish foundation

M5-C6 composes the M5 economy, resource, production, structure, and existing
command contracts in a bounded synthetic two-player headless loop. Each side
has a resource harvest, income transaction, power/factory construction,
production, rally, and combat candidate. Combat commits damage
simultaneously; structure destruction produces explicit defeat and winner
state.

Computer-agent commands and pre-authored human/script `CommandRequest` values
share the existing command stream. The loop records authoritative economy and
command state in a canonical hash, so repeated runs with the same seed,
configuration, and commands are stable.

This is a project-enhancement harness only. It does not implement stock YR
victory rules, map loading, terrain semantics, rendering, UI, networking,
replay serialization, or ProjectBaseline packed data access.
