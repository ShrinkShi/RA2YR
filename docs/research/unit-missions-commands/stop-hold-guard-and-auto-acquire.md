> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Stop, Hold, Guard, and Autonomous Acquisition

## Stock public evidence

The RA2/YR manuals document:

- `S`: stop selected unit movement;
- `G`: guard current area;
- ordinary units may attack approaching enemies;
- Guard units attack approaching enemies and return to their original position;
- `H`: center the tactical view on the base, not Hold Position.

Community Mission Control documentation distinguishes Guard, Sticky, Area Guard, Stop, Ambush, Hunt, passive acquisition, and pursuit behavior, but does not constitute official runtime source.

## Project S policy

`S` is `ConfiguredForProjectPolicy`:

```text
interrupt current explicit command candidate
clear explicit movement destination
cancel current path request
clear explicit attack target candidate
clear queued orders according to StopQueuePolicy
retain autonomous-targeting eligibility
retain retaliation eligibility
permit legal in-range fire after reevaluation
```

It does not mean:

- disable weapons;
- cease fire permanently;
- Hold Position;
- disable passive acquire;
- delete actor;
- reset map placement Mission.

## Project H policy

`H` is `ConfiguredForProjectPolicy` and intentionally replaces the stock camera binding in the project control scheme:

```text
HoldPositionActive = true
LeashOrigin = current authoritative position
AutonomousMovementAllowed = false
AutonomousChaseAllowed = false
Explicit movement accepted later clears hold
InPlaceTurnAllowed = policy input
InPlaceAimAllowed = policy input
InPlaceFireAllowed = policy input
PassiveAcquireAllowed = policy input
RetaliationAllowed = policy input
```

Hold Position is not cease-fire. It is closest to a Sticky-like no-pursuit policy, but must not be called the stock Sticky mission without evidence.

## Project G policy

`G` opens an autonomous-behavior configuration GUI:

```text
GuardEnabled
AutoAttackEnabled
RetaliationEnabled
PursuitEnabled
LeashEnabled
LeashRadius
TargetCategories
TargetPersistence
ThreatPreference
ReturnToOriginPolicy
```

Opening or changing the GUI does not itself create a stock Guard command unless a future explicit adapter translates a chosen profile into simulation commands.

## Autonomous behavior model

```text
AutonomousBehaviorProfile
AcquisitionPolicy
RetaliationPolicy
PursuitPolicy
LeashPolicy
HoldPositionPolicy
TargetPersistencePolicy
```

Do not collapse these into `Aggressive=true/false`.

## Passive acquisition and retaliation

Ares documents that manual target selection, autonomous acquisition, and retaliation are distinct. Its `NoManualFire` can disable manual targeting while retaining autonomous targeting and retaliation. Ares also separates passive acquisition behavior in Guard and Area Guard. This is extension evidence supporting the architecture, not stock defaults.

## Decision order candidate

```text
explicit command authority
→ current hold/mission restrictions
→ target eligibility
→ retaliation candidate
→ passive acquisition candidate
→ target persistence candidate
→ pursuit/leash candidate
→ movement intent
→ firing intent
```

The order remains a future simulation policy.
