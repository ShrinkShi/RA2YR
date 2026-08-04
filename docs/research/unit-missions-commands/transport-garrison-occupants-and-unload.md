> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Transport, Garrison, Occupants, and Unload

## Separate storage domains

```text
TransportPassengerCapacity
PassengerSize
PassengerSizeLimit
PassengerEligibility
TransportCargoRuntimeState
GarrisonOccupantCapacity
GarrisonOccupancyRuntimeState
FiringPortCapability
UIOccupancyPips
```

Image size, Foundation, pip count, and rendered doors do not define capacity.

## Transport models

```text
TransportOccupancyDescriptor
- TransportType
- CapacityCandidates
- SizeLimitCandidate
- BySizePolicyCandidate
- AllowedPassengerCandidates
- DisallowedPassengerCandidates
- ManualEnterPolicy
- ManualUnloadPolicy
- SurvivorPolicyCandidates
- ProductProfile
```

Ares-specific passenger allow/disallow lists, `Passengers.BySize`, initial payload, manual-enter suppression, and survivor controls remain extension profiles.

## Embark sequence candidate

```text
Enter command
→ passenger/transport validation
→ occupancy reservation
→ approach/path request
→ alignment candidate
→ embark transition
→ remove passenger from world occupancy
→ add cargo entry
→ mission completion
```

No UI cursor or animation is authoritative.

## Unload sequence candidate

```text
Unload command
→ unload-location candidate
→ layer and terrain validation
→ deterministic passenger order
→ per-passenger exit-cell reservation
→ spawn/restore world occupancy
→ cargo removal
→ blocked/pass/fail result
```

Unloading all passengers, ejecting one passenger, paradrop, naval landing craft unload, IFV gunner behavior, and garrison evacuation require separate profiles.

## Blocked unload

Explicit policies:

- wait and retry;
- alternate-cell search;
- partial unload;
- cancel remaining;
- keep passengers;
- kill or remove passengers;
- move transport;
- fail command.

No policy is selected in this research.

## Garrison

Separate:

- `CanBeOccupied`;
- `MaxNumberOccupants`;
- `Occupier`;
- `CanOccupyFire`;
- occupant weapon references;
- entry command;
- occupant slot;
- building ownership;
- evacuation;
- capture;
- building destruction;
- occupant survival.

Community documentation indicates RA2 and YR differ in occupant weapon handling. Product profiles must remain separate.

## Ownership and destruction

Transport owner changes, garrison capture, transporter destruction, passenger survival, airborne destruction, and blocked survivor cells are future simulation policies. Savegames must serialize stable passenger/occupant identities and ordering.
