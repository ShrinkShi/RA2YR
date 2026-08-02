# Architecture boundaries

The engine is a data-driven compatibility implementation. Unity hosts input,
presentation, audio, editor tooling, and platform integration; it is not the
authority for simulation behavior.

## Dependency direction

```text
External read-only content
  -> Content and source tracking
  -> Formats / INI / rules / maps
  -> Deterministic simulation / AI / scenario
  -> Save / replay / network protocol
  -> immutable render snapshots and presentation events
  -> Unity integration / rendering / audio / UI / platform
```

Editor tools and tests may depend on the public core APIs. Core assemblies may
not depend on Unity integration, scenes, GameObjects, or rendering objects.

## Core boundary

Core assemblies use `noEngineReferences: true` and must not reference
`UnityEngine` or `UnityEditor`. The boundary includes:

- content discovery, hashing, source precedence, and cache metadata;
- binary formats, lossless INI documents, rules, and map models;
- deterministic simulation, pathfinding, AI, triggers, and scenarios;
- save-state serialization, replay, state hashing, and network protocol data.

The rule is enforced by assembly definitions, dependency tests, and a
headless test path that can execute core behavior without loading a scene.

Content names are also independent of host file-system enumeration. Logical
paths use `/`, `OrdinalIgnoreCase` identity, preserved physical filename case,
explicit stable ordering, and fail-closed priority ambiguity. Source IDs are
identities for provenance reports; they never act as an implicit tie-breaker.

`MonoBehaviour`, Rigidbody, NavMesh, Animator, Renderer, and GameObject state
must never be authoritative simulation state. Unity components consume state;
they do not define it.

## Logical time and authoritative data

- Simulation advances only through an explicit `AdvanceOneTick()` boundary.
- The authoritative rate is exactly 15 ticks per second.
- `FixedUpdate()` is not the YR main loop. Frame timing may request ordered
  ticks but may not skip, merge, reorder, or change their random call order.
- One cell is 256 leptons and one height level is 208 leptons.
- Coordinates, ticks, object IDs, random state, funds, and consequential rule
  results use integers or explicitly versioned fixed-point representations.
- Iteration and tie-breaking order are specified and stable.

## Snapshot boundary

At a completed tick, the core publishes immutable render snapshots and
presentation events. Unity may interpolate snapshots with floating-point
values for display. Interpolated or rendered values cannot flow back into the
authoritative state. Audio and effects are driven by versioned events and may
be replayed or suppressed without changing simulation results.

## Verification gates

1. Core assembly definitions use `noEngineReferences`.
2. Core tests run without a scene and without frame-time APIs.
3. The same content identity, seed, and command stream produce identical
   per-tick hashes at different display frame rates and in headless runs.
4. Save/load continuation and replay produce the same hashes as uninterrupted
   execution.
5. Rendering, audio, and editor tools cannot mutate simulation objects.

See the ADR directory for accepted scope and boundary decisions.

Implementation notes for the WP-01/WP-02A content foundation are documented in
[`external-content.md`](external-content.md) and
[`content-resolution.md`](content-resolution.md). The format-neutral WP-02B
input, budget, diagnostic, and tail boundary is documented in
[`bounded-binary-reading.md`](bounded-binary-reading.md). The bounded archive,
mount, provenance, and rebuilding boundaries are documented in
[`mix-content.md`](mix-content.md).
