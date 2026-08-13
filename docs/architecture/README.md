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

## M4 simulation architecture

M4 uses a deterministic data-oriented ECS as the authoritative simulation
boundary. The dependency direction is:

```text
Formats / Content Core → Simulation descriptors → RA2YR.Simulation
                       → Presentation Adapter → Unity
```

`RA2YR.Simulation` must remain Unity-free. Entity identity, logical time,
occupancy, commands, combat results, and state hashes are not derived from
GameObjects, render frames, physics components, or NavMesh. Unity DOTS/Jobs/Burst
may be optional execution backends only.

Authoritative work follows `single logical authority + parallel compute +
deterministic commit`: inputs are canonicalized, a read-only snapshot is
evaluated, proposals are computed sequentially or in parallel, then stably
ordered and committed by one logical authority before the state hash is emitted.
Workers never mutate the world directly.

Unit tactical autonomy and computer AI are capability and interface layers above
the simulation. `Manual | Assisted | Automatic` is a permanent autonomy mode;
player commands and `AutonomyEnvelope` are explicit authority boundaries.
Computer policies receive immutable legal observations and emit validated command
requests. RuleBased remains the deterministic fallback; Neural/Hybrid are future
policy backends and never simulation authorities. Training telemetry is separate
from authoritative replay state.

Content names are also independent of host file-system enumeration. Logical
paths use `/`, `OrdinalIgnoreCase` identity, preserved physical filename case,
explicit stable ordering, and fail-closed priority ambiguity. Source IDs are
identities for provenance reports; they never act as an implicit tie-breaker.

`MonoBehaviour`, Rigidbody, NavMesh, Animator, Renderer, and GameObject state
must never be authoritative simulation state. Unity components consume state;
they do not define it.

M4-C1 supplies the first Unity-free kernel: generation-checked entity handles,
bounded component stores, ordered structural commands, explicit logical time and
scheduler phases, seed-and-stream randomness, immutable read snapshots,
canonical state hashes, and deterministic proposal ordering. These are synthetic
project-enhancement contracts, not original-runtime compatibility.

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

The WP-02G2 typed projection remains inside Core and consumes only a completed,
explicit WP-02G1 resolution. It preserves every selected and overridden source
trace and fails closed for ambiguous/failed inputs. Its current Rules and Art
surface is limited to explicit resource discovery; see
[`../formats/ini-minimal-resource-views.md`](../formats/ini-minimal-resource-views.md)
and ADR 0016.

Legacy visual formats are import adapters rather than canonical runtime
assets. Simulation references logical visual identities and never branches on
`.shp` or `.vxl`; see [`visual-asset-pipeline.md`](visual-asset-pipeline.md) and
ADR 0021. Project-wide implementation and evidence maintenance rules are in
[`engineering-maintainability.md`](engineering-maintainability.md).
