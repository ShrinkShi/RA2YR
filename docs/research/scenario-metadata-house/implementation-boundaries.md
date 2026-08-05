# Implementation boundaries

> **Source notice:** ChatGPT Web public-source research. ProjectBaseline was not read. `code_imported: false`.

## Pipeline

```text
LosslessIniDocument
→ ScenarioMetadataRaw
→ Geometry/Theater candidates
→ House/Country identity graph
→ House-property candidates
→ Alliance graph
→ Start/mode candidates
→ future session/simulation adapters
```

Core models retain raw values, duplicates, order, source layers and diagnostic candidates. They do not create players, Houses, alliances, economies, starts, network peers, campaign state or Unity objects.

## Suggested models

- `ScenarioMetadataDocument`
- `BasicMetadataRaw`
- `MapMetadataRaw`
- `ScenarioRectangleRaw`
- `TheaterIdentityCandidate`
- `ScenarioHouseRaw`
- `ScenarioCountryRaw`
- `HouseIdentityGraph`
- `HousePropertyCandidate`
- `AllianceEdgeRaw`
- `StartLocationCandidate`
- `ScenarioModeCandidate`
- `ScenarioLocalCompositionPolicy`
- `ScenarioMetadataDiagnostic`

## Policy classification

```text
PolicyClassification: DefensiveDesign
AuditStatus: NotRun
```

Project policies include raw/duplicate preservation, checked rectangle arithmetic, no registry compression, no missing-House fabrication, no color-to-House inference, no alliance symmetrization, no random start selection, no single-field mode inference, explicit map-local composition and separation of authored metadata from lobby/runtime state.

## Formal grade vocabulary

All evidence-bearing models serialize exactly one of:

```text
ConfirmedByOriginalRuntimeSource
ConfirmedByOfficialToolSource
ConfirmedByMultipleIndependentImplementations
ConfirmedCommunityConvention
ImplementationSpecificBehavior
DefensiveDesign
ConflictingSources
Underconfirmed
Unresolved
```

Source, limitations, policy and audit status are separate fields. No reviewed claim currently has original-runtime-source confirmation.

## Dependency restrictions

Core does not reference Unity, client UI, network/session services, resource loaders, campaign execution, AI, economy, diplomacy or renderer APIs. Adapters consume immutable descriptors and must not rewrite source metadata.

## Roundtrip

A lossless descriptor tracks physical section/key order, raw spelling, duplicates, comments/whitespace through the lossless INI layer, selected semantic profiles and whether any canonical rewrite would lose identity or provenance. Default writing performs no repair, reindexing, symmetry generation or unknown-field deletion.
