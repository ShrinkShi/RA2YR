> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Layer and Domain Boundaries

## Frozen layers

```text
1. ordered Rules/map/AI documents
2. raw placement and script references
3. mission token and command identity views
4. product/extension profile binding
5. immutable command-capability descriptors
6. runtime snapshots supplied by simulation
7. command validation result
8. deterministic command queue
9. mission transition and autonomous-policy candidates
10. movement/targeting/firing/occupancy intents
11. future simulation systems
12. future UI and presentation adapters
```

## Forbidden layer leaks

- Map parser does not create an actor or set a live mission.
- Rules binder does not scan the world for targets.
- Mission binder does not request paths.
- Command validator does not mutate actor state.
- Queue does not execute movement or weapon fire.
- Autonomous policy does not render target lines.
- Renderer animations do not authorize mission transitions.
- Selection UI does not determine command ownership.
- Transport UI pips do not determine capacity.
- Core does not instantiate `GameObject`, `NavMeshAgent`, `Collider`, `LineRenderer`, `Button`, or cursor objects.

## Coordinate and identity domains

Keep separate:

- raw map placement identity;
- actor type identity;
- runtime actor stable identity;
- player stable identity;
- target actor identity;
- target cell identity;
- scenario waypoint identity;
- UI waypoint-node identity;
- path node identity;
- transport occupancy slot;
- garrison occupant slot;
- control-group slot;
- render selection marker.

## State ownership

| State | Owner |
|---|---|
| raw Mission token | map/Rules document |
| command capability | Core descriptor |
| issued command | input/session layer |
| accepted command | deterministic simulation |
| current mission | actor simulation |
| path request/result | movement subsystem |
| target acquisition | targeting subsystem |
| firing permission | combat subsystem |
| passenger list | transport simulation |
| selection | local UI/session |
| control groups | local or synchronized policy |
| animation | presentation |

## Raw versus derived

Raw strings, numeric spelling, duplicates, case, unknown tokens, extension keys, and source provenance are preserved. Derived mission families, command categories, validation outcomes, and autonomous policies are separate serializable views.

## Input contracts

Memory, seekable Stream, short-read Stream, and exact MIX-window inputs must feed one logical reader behavior. Budgets cover mission tokens, command definitions, waypoint nodes, queue entries, transport slots, occupants, diagnostics, and graph edges. Checked arithmetic and no-progress guards are mandatory.
