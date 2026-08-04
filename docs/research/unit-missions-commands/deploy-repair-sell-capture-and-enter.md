> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Deploy, Repair, Sell, Capture, and Enter

## Common command contract

Each special command should produce:

```text
RawCommand
→ capability query
→ relationship query
→ target-state query
→ path/approach candidate
→ reservation candidate
→ accepted command
→ mission transition candidate
→ future subsystem command
```

The unit-command layer does not implement the subsystem effect.

## Deploy and undeploy

Candidate inputs:

- `DeploysInto`;
- `UndeploysInto`;
- `IsSimpleDeployer`;
- deploy direction;
- terrain/layer suitability;
- occupancy;
- build limit and transformation equivalence;
- health/cargo/ammo/veterancy transfer;
- owner and current mission;
- extension deployment fields.

Recommended:

```text
DeployCommandDescriptor
TypeTransformationCandidate
TransferPolicyCandidate
PlacementCandidate
```

No actor is deleted or created by the binder.

## Repair

Separate:

- repair cursor;
- repair command;
- repairable target;
- repair facility entry;
- repair weapon;
- credits transaction;
- current Repair mission;
- animation/presentation.

An Enter-like approach to a service depot is not proof that all repair is transport entry.

## Sell

Separate:

- UI sell mode;
- sell command;
- sellable capability;
- ownership;
- current Selling mission;
- deconstruction progress;
- refund;
- survivor spawning;
- deploy/undeploy interaction.

Ares documents stock YR selling/deploy edge cases and fixes; these are extension/bug-fix evidence, not a reason to copy the implementation.

## Capture and infiltration

```text
CaptureCommand
EngineerEntryCandidate
SpyInfiltrationCandidate
SabotageCandidate
TargetOwnershipChangeCandidate
```

Engineer capture, spy infiltration, C4/sabotage, mind-control, and ordinary transport entry are different commands even if they share cursor or approach logic.

## Enter

`EnterCommandDescriptor` must identify target domain:

- transport;
- garrisonable building;
- repair facility;
- grinder;
- bunker;
- scripted structure;
- extension target.

Validation includes relationship, capacity, passenger eligibility, occupancy reservation, path reachability, target status, and command cancellation.

## Cancellation cases

- target destroyed;
- target captured;
- target fills;
- target moves;
- actor becomes unable to enter;
- path fails;
- issuing owner changes;
- Stop/Hold/new explicit command;
- deploy begins;
- save/load restoration conflict.
