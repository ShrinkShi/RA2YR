# Map-local Rules and Art override layers

## 1. Required pipeline

Map-local configuration must reuse the ordered semantic-composition boundary established by the content-load-order research:

```text
global ordered INI layers
→ effective global Rules/Art documents
→ lossless map document
→ recognized map-local override sections
→ semantic key-level composition
→ typed map scenario views
```

The map file reader does not scan global archives and the global INI resolver does not parse packed map sections.

## 2. Identity

For ordinary local Rules overrides, identity is the same semantic pair used by the effective INI system:

```text
SectionName + KeyName
```

A map-local occurrence replaces only the same effective identity. Omitted keys remain inherited and new keys/sections are added.

Every effective key retains:

- winning map/global occurrence;
- complete suppressed candidate chain;
- comparison policy;
- map source/provenance;
- diagnostic history.

## 3. Art namespace

Chrono Divide documents an `ART.<SECTION>` map extension for Art overrides. Other engines/editors may use different conventions.

Treat this as a profile feature:

```text
MapLocalArtNamespacePolicy
- PrefixArtDot
- EngineSpecific
- Disabled
- Unresolved
```

Do not strip `ART.` globally unless the selected runtime profile explicitly enables it.

## 4. Rules versus scenario sections

Many map sections share names with Rules-defined type sections. Classification cannot be based only on “name not in a hardcoded map-section list”.

Use an explicit registry of structural map sections plus a semantic policy that can classify remaining sections as:

- scenario definition;
- map-local Rules override;
- map-local Art override;
- editor metadata;
- extension/unknown.

Ambiguous sections remain raw and unresolved.

## 5. Type lists and numbered registries

Map-local overrides can affect lists such as countries, types, overlays or other registries. Generic key-level composition does not by itself define list semantics.

A typed consumer must explicitly decide whether a numbered section means:

- key identity by numeric key;
- ordered sequence;
- append-only registry;
- replacement list;
- engine-specific reset/delete operation.

No automatic renumbering or CSV merging occurs in the generic composer.

## 6. Empty values and deletion

An empty map-local value can mean an empty string, a typed default/reset, deletion under an extension, or invalid input.

Default research status: `Unresolved`.

The lossless composer records the empty winning occurrence. A typed profile may later interpret deletion/reset only with explicit source evidence.

## 7. Same-document duplicates

Separate:

- duplicate keys/sections inside the map document;
- a map-local occurrence overriding the global effective value;
- map-local extension include/inheritance behavior.

Do not collapse these into one suppressed chain without marking the relationship type.

## 8. Rules/Art validation timing

Object and terrain binding should occur only after composition finishes.

Examples:

- an object type may be introduced by a map-local section;
- an existing overlay type may be changed locally;
- theater/TMP binding may depend on effective theater data;
- an object placement should not be rejected using only the global Rules view.

## 9. Provenance model

```text
MapEffectiveEntry
- SemanticIdentity
- EffectiveRawValue
- WinnerOccurrence
- SuppressedOccurrences[]
- Origin: GlobalBase | GlobalExpansion | Loose | MapLocal
- ExtensionPolicyUsed?
- Diagnostics[]
```

The typed view references this entry rather than copying an untraceable scalar.

## 10. Round-trip boundary

A lossless map rewrite should preserve local override sections even when the current typed engine does not understand them.

A normalization writer may regenerate known sections only if it reports:

- sections rewritten;
- keys reordered/removed;
- extension semantics applied;
- provenance lost;
- references revalidated.

## 11. Forbidden behavior

Do not:

- treat the map as a complete replacement for global Rules/Art;
- merge files as raw text;
- discard global inherited values;
- scan MIX archives from the map parser;
- resolve object types before composition;
- assume every unknown section is a Rules type;
- apply Ares/Phobos/Chrono Divide syntax to vanilla profiles automatically;
- use SHA or file length to decide semantic winners.
