# M4-C6 scenario spawn and computer-agent platform

C6 keeps scenario placement families and raw key/value provenance separate from
runtime entities. A bounded `ScenarioSpawner` validates synthetic Unit,
Infantry, Structure, and Aircraft candidates and emits ordered `SpawnRequest`
values; it does not create entities while parsing. Owner binding is explicit and
unresolved owners fail closed.

`AgentObservation` is a sorted, immutable view containing only legal owner,
unit, terrain and objective candidates. Policies receive this view through
`IAgentObservationProvider` and return normal `CommandRequest` values through
`IAgentPolicy`. The sample rule policy is a deterministic fallback. Neural
support is a descriptor/backend contract only; no model or ML runtime is
included. `HeadlessSimulationEnvironment` proves reset/step/observe/state-hash
without Unity, rendering, ProjectBaseline data, or gameplay parity claims.
