# ADR-0054: M6-C8 integrated playable presentation closeout

## Status

Accepted for synthetic/configured implementation.

## Decision

Use a bounded `PlayablePresentationCloseoutHarness` to run authoritative
Simulation ticks alongside provider-neutral presentation snapshots. Cadence is
an explicit rational profile (30, 60, or 144 presentation frames per second),
and the harness compares Simulation state hashes before and after presentation
assembly and at the final tick. Unity owns only the central presentation world
and interactive client adapter; it never advances or authors Simulation state.

## Consequences

The project has a repeatable integrated client/performance contract over the
500/1000/2000 entity tiers, deterministic cadence behavior, and a Unity smoke
path without introducing a map loader, renderer rewrite, writer, or gameplay
loop. Wall-clock FPS, GPU performance, original runtime equivalence, network
play, and M7 content remain outside this closeout.
