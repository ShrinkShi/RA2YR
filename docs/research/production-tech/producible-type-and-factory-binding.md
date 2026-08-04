> **Source notice:** This document was prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Producible type and factory binding

## Registry families

Leading stock-oriented candidates:

```text
[BuildingTypes]
[VehicleTypes]
[InfantryTypes]
[AircraftTypes]
```

Extensions may add registries or dynamic providers. A section name alone does not prove registry membership.

## Raw registry record

```text
TypeRegistryEntryRaw
- RegistryFamily
- KeyRaw
- OrdinalCandidate
- ValueRaw
- SourceOccurrence
- CompositionLayer
- DuplicateKeyCandidates
- DuplicateValueCandidates
- CaseCollisionCandidates
- EvidenceGrade
```

The binder creates `TypeIdentity` candidates without renumbering gaps.

## Required diagnostics

- numeric key gap;
- duplicate raw key;
- normalized-key collision;
- duplicate type value;
- case-only type collision;
- listed type with missing section;
- unlisted section resembling a type;
- map-local contribution;
- competing composition-layer definitions;
- unknown registry family;
- invalid or overflowing ordinal;
- registry and field budget exceeded.

No first-wins or last-wins policy is implicit.

## Production categories

```text
Building
Infantry
Vehicle
Aircraft
Naval
Repair
Upgrade
Defense
Wall
SpecialExtension
Unknown
```

Community use of `Unit` can mean VehicleType or a broader product family. Profiles resolve this explicitly.

## Factory capability descriptor

```text
FactoryCapabilityDescriptor
- FactoryTypeIdentity
- ProductionCategories[]
- NavalCandidate
- NumberOfDocksCandidate
- PadAircraftCandidate
- ExitReferenceCandidates[]
- ExplicitProductCandidates[]
- PrimaryFactoryCandidate
- FactoryPlantCandidate
- CloningCandidate
- OperationalRequirements[]
- ProviderProfile
- EvidenceGrade
```

Candidate source fields include `Factory`, `WeaponsFactory`, barracks flags, `ConstructionYard`, `Helipad`, `Naval`, `NumberOfDocks`, `PadAircraft`, `Hospital`, `Armory`, `Cloning`, `FactoryPlant`, `Primary` and extension fields.

## Independent questions

```text
CanProduceCategory
CanAcceptRequest
IsPrimaryCandidate
ContributesSpeedModifier
SharesQueue
OwnsQueue
ProvidesExit
ProvidesDock
ClonesProduct
```

They must not be collapsed.

## Binding prohibitions

The binder does not:

- infer factories from names containing `FACT`, `WEAP`, `BARRACKS` or `YARD`;
- infer a category from Art;
- create a queue;
- select the runtime primary factory;
- assume every compatible factory provides a valid exit;
- treat `WeaponsFactory=yes` and `Factory=` as synonyms;
- treat Ares `BuiltAt` or `Factory.ExplicitOnly` as stock YR.

Ares documents explicit `BuiltAt` / `Factory.ExplicitOnly` relations and separately notes legacy exit flags. This supports keeping production capability and exit behavior distinct, but remains extension evidence.

## Multiple factories

PPM discussions report shared queues between factories of one category in stock-style RA2/YR use. Ares documents parallel-AI and load-sharing changes. Queue ownership and speed contribution therefore remain explicit product-profile policies.

## Source anchors

- Ares 3.0 `Prerequisites` and `Factories and Cloning` documentation.
- ModEnc registry and 100-units-bug documentation.
- OpenRA `Buildable`, `Production`, `ProductionQueue` and `Exit` files at `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a`.
- EA editor source at `6abf0f557469baea73079c6bf6550709e2e3584e`.

All implementations are reference-only; `code_imported: false`.
