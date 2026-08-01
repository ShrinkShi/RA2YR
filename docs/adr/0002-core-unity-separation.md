# ADR 0002: Core and Unity separation

- Status: Accepted
- Date: 2026-08-01

## Context

Compatibility requires deterministic behavior independent of display frame
rate, scene state, Unity physics, animation, and rendering objects.

## Decision

Core content, format, INI, map, simulation, AI, scenario, persistence, replay,
and network-protocol assemblies use `noEngineReferences: true`. They cannot
reference `UnityEngine` or `UnityEditor`.

The simulation advances through an explicit 15 Hz logical clock. `FixedUpdate`
is not the main loop. Authoritative coordinates, height, time, object IDs,
random state, and consequential rule results are integer or versioned
fixed-point values. One cell is 256 leptons; one height level is 208 leptons.

Unity integration submits input commands and consumes immutable snapshots and
presentation events. Float interpolation is display-only and cannot write back
to core state.

## Consequences

- Rigidbody, NavMesh, Animator, GameObject, and Renderer state are not game
  truth.
- Stable update order and deterministic random-call order are explicit APIs.
- Core logic can run headless in tests and synchronization diagnostics.

## Verification

- Assembly dependency and forbidden-reference tests.
- Headless core execution without scene loading.
- Cross-frame-rate, save/load, and replay state-hash equality.
