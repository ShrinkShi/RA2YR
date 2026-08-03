# Nested MIX boundaries

## 1. Six distinct concepts

1. root archive discovery;
2. explicit fixed child mounting;
3. virtual-file precedence;
4. an entry whose bytes happen to parse as MIX;
5. a child archive the game/profile knows by name;
6. generic recursive discovery offered by a tool or this project.

Only items 2 and 5 authorize a legacy runtime mount by default.

## 2. No arbitrary recursion

The project may already be able to parse nested MIX windows. That is a capability, not evidence that original RA2/YR recursively mounted every nested archive.

Default rule:

- an arbitrary `.mix` entry remains an opaque file candidate;
- a family descriptor may create an explicit child-mount edge;
- recursion follows only declared edges and remains depth/count bounded.

## 3. Explicit mount graph

A mount graph node records:

```text
MountNode
- PhysicalContentIdentity
- ProviderIdentity
- ArchiveFamily
- LogicalArchiveName
- ParentNode?
- EntryIdentity?
- ExplicitMountRole
- PriorityKey
- DiscoveryEvidence
```

Examples of explicit child roles include local, cache, conquer, generic, maps, theater, UI side packages, movie, and theme packages.

## 4. `localmd.mix` and `cachemd.mix`

These should enter the virtual view through a configured edge from the appropriate YR base archive or other documented parent. The MIX reader does not search the whole tree for those names.

If the same logical child name appears under multiple higher-level providers, each produces a separate candidate chain tied to its parent layer. Parent precedence is compared before child-role order.

## 5. Priority versus depth

Nested depth never grants priority by itself.

Recommended comparison:

1. top-level provider/layer precedence;
2. explicit child-role precedence within that parent;
3. declared mount ordinal;
4. ambiguity handling.

Thus a deeper child under a higher `expandmd` provider can outrank a shallower child under `ra2md`, but only because its root layer is higher, not because it is deeper.

## 6. Multiple child archives in one parent

When a parent contains several known child archives, order comes from the parent's family descriptor, not archive-entry enumeration.

If public evidence does not establish an original order:

- preserve all mounts;
- use an explicit project order when needed;
- label it `ConfiguredProjectPolicy`;
- retain an unresolved-original diagnostic.

## 7. Duplicate physical content

The same physical bytes may become reachable through more than one path.

Deduplication uses a stable physical mount identity, not SHA alone:

```text
(provider, parent mount identity, entry identity, bounded range)
```

If two paths refer to exactly the same mount identity:

- avoid opening/mounting it twice;
- preserve both discovery paths in provenance;
- prevent recursion cycles.

Two distinct entries with equal SHA are not automatically the same mount.

## 8. Cycle and depth control

Although well-formed MIX archives are finite byte windows, malicious or synthetic provider graphs can create logical cycles.

Limits include:

- maximum nested depth;
- maximum mounted archives;
- maximum outgoing child edges;
- maximum repeated physical identity;
- maximum diagnostics.

On cycle detection, record the complete safe logical chain and stop that edge.

## 9. Root archive containing arbitrary user MIX

A user archive embedded inside another user archive is not automatically mounted. A future `UserMod` provider may opt into a manifest-driven child list. That manifest behavior is modern provider semantics, not legacy evidence.

## 10. Tool behavior

XCC and editors can browse or extract nested MIX files. Browsability is not runtime auto-mount evidence.

FinalAlert/FinalSun may open known packages for map-editing resources. Its tool mount order must not be imported as game-runtime truth.

## 11. Nested provenance

Every candidate records:

```text
root provider
→ root archive
→ explicit child role/name
→ optional further declared child
→ entry
```

The public audit may report this logical chain for selected candidate names, but not complete archive entry listings.
