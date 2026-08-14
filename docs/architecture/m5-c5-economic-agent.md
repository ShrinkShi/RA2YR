# M5-C5 economic computer-agent foundation

M5-C5 composes the existing Unity-free C1 house/economy, C2 resource, C3
production/technology, and C4 structure contracts into an explicit economic
agent observation and proposal layer. The evaluator is deterministic,
bounded, and read-only: it never mutates credits, cargo, queues, occupancy,
power, or structures.

The observation contains only supplied authoritative snapshots and legal
agent-facing candidates. Hidden enemy money, hidden queues, fog truth,
renderer state, Unity objects, and model-training telemetry are not exposed.
`EconomicAgentStrategyProfile` changes proposal priority only; it is not an
attempt to reproduce `ai.ini` or stock YR strategy semantics.

Implemented project-enhancement profile:

- `ConservativeDeterministic` policy;
- explicit `AllIn`, `Rush`, `Pressure`, `Balanced`, `Macro`, and `Turtle`
  strategy labels;
- bounded harvest, production, repair/sell/capture/deploy proposal candidates;
- fail-closed child execution and diagnostic-budget state;
- immutable proposal snapshots and canonical proposal hash.

No neural runtime, ONNX model, renderer, gameplay loop, payment/queue
mutation, pathfinding, or ProjectBaseline packed data is included.
