> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Power, ownership, capture and availability

## Ownership identities

```text
TypeOwnerTokenRaw
CountryDefinition
SideDefinition
HouseInstance
PlayerSlot
Controller
CurrentOwner
CapturedOwner
InitialFactoryOwner
AvailabilityRelation
```

`Owner` is not the current player. Side is not Country. A map-authored Player House does not establish all production relationships.

## Ownership-related candidates

- `Owner`;
- `RequiredHouses`;
- `ForbiddenHouses`;
- country-specific products;
- side-level factory categories;
- map-local overrides;
- multiplayer random-country selection;
- campaign House;
- neutral/civilian ownership;
- Secret Lab results;
- stolen technology;
- factory-owner plans;
- capture;
- reverse engineering;
- extension-specific plans.

Unknown owner tokens remain dangling and are not broadened to every House.

## Power separation

```text
AuthoredPowerProduction
AuthoredPowerDrain
RuntimePowerSnapshot
LowPowerState
FactoryOperationalState
AvailabilityState
ProductionProgressPolicy
PresentationState
```

Rules parsing does not calculate current power. Low power may affect progress, availability or selected categories only according to an explicit product policy.

Ares `BuildTime.LowPower*` and powered-unit extensions remain extension profiles.

## Availability query

```text
ProductionAvailabilityQuery
- ProducibleTypeDescriptor
- PlayerTechnologySnapshot
- OwnedTypeCounts
- FactoryCapabilitySnapshot
- SessionTechLevel
- RuntimePowerSnapshot
- CreditsSnapshot
- BuildLimitSnapshot
- GameModeProfile
- ScenarioRestrictions
- ExtensionState
```

Output retains all blocker reasons:

```text
Available
VisibleButUnavailable
Hidden
BlockedByPrerequisite
BlockedByTechLevel
BlockedByOwnership
BlockedByFactory
BlockedByBuildLimit
BlockedByCredits
BlockedByPower
BlockedByScenario
Unknown
```

## Capture policy questions

- Does the queue remain with the factory, old owner or new owner?
- Who owns already-paid progress?
- Does progress persist?
- Is the queued product valid for the new owner?
- Which country modifier applies after capture?
- Does the factory grant the initial owner's plans?
- Does it grant categories or specific products?
- What happens to completed buildings awaiting placement?
- What if capture and completion occur in one tick?
- What happens on sale, destruction or player defeat?

These belong to future simulation policy, not the type binder.

Ares documents factory-owner plans and distinguishes initial owner, current owner and permanent plans. That supports the identity split but is not vanilla proof.

## Mind control and disguise

Mind control changes runtime control, not authored type ownership. Disguise is presentation/targeting state and does not automatically grant production. Neither is queried by the Rules parser.

## House/Side dependency

This dossier consumes PR #33's House, Country, Side, player and alliance graph. It does not modify PR #33 or recreate its parser.

## Source anchors

- Ares prerequisite factory-owner, powered-unit and build-time documentation.
- OpenRA power manager and production queue architecture, independent implementation.
- ModEnc/PPM capture and production observations, community evidence.

No power or capture simulation was implemented; `code_imported: false`.
