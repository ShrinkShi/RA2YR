# Producible type and factory binding

> **Source notice:** Public-source research only. ProjectBaseline was not read. `code_imported: false`.

## Registry and factory split

```text
RegistryEntryRaw
TypeDefinitionRaw
ProducibleTypeDescriptor
FactoryCapabilityDescriptor
FactoryRuntimeInstance
ProductionCategory
```

Listed types with missing sections, unlisted definitions, gaps, duplicate keys/values, case collisions and map-local contributions remain explicit. Missing Art/cameo does not remove logical type identity or renumber registries.

## Evidence

| Claim | Grade | Source | Notes | Policy | AuditStatus |
|---|---|---|---|---|---|
| FinalAlert exposes type registries and factory/type fields | `ConfirmedByOfficialToolSource` | EA editor | Official tool only. | Named editor profile. | `NotRun` |
| OpenRA/Ares/Phobos implement factory/category/cloning profiles | `ImplementationSpecificBehavior` | Named implementations | Target/extension-specific. | Keep separate. | `NotRun` |
| Common Unit/Infantry/Aircraft/Building registries and factory categories | `ConfirmedCommunityConvention` | ModEnc/PPM/community docs | Convention only. | Preserve registry provenance. | `NotRun` |
| Registered definition plus matching factory as availability candidates | `Underconfirmed` | Tools/community | Runtime registration/defaults and lineage independence unproven. | Explicit binding policy. | `NotRun` |
| Multiple factory roles, cloning, upgrades, naval/air/building category and captured factory behavior | `ConflictingSources` | Engines/extensions/community | Models differ directly. | Preserve capability sets. | `NotRun` |
| Exact runtime factory selection, registration and queue ownership | `Unresolved` | No runtime source | No complete contract. | Future simulation adapter. | `NotRun` |
| No registry compression/fabrication and missing-resource independence | `DefensiveDesign` | Project policy | Preservation/architecture. | Fail closed. | `NotRun` |

A factory descriptor states product categories/capabilities, not current operability, ownership, power, queue or placement. Type definitions preserve authored Owners, prerequisites, TechLevel, BuildLimit, Cost/time, deploy/upgrade and unknown fields without deciding availability.
