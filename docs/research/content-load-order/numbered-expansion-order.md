# Numbered expansion order

## 1. Configured YR policy

The project profile defines low to high:

```text
ra2.mix
ra2md.mix
expandmd01.mix
expandmd02.mix
...
expandmd99.mix
loose files
```

Therefore:

- larger valid `expandmdNN` overrides smaller numbers for ordinary whole-file lookup;
- larger valid `expandmdNN` is applied later for INI composition;
- every `expandmdNN` overlays `ra2md.mix`;
- `ra2md.mix` overlays `ra2.mix`;
- missing sequence values are harmless;
- normalized results are independent of enumeration order.

This is `ConfiguredProjectPolicy`.

## 2. Evidence conflict on range

Community and reimplementation sources often recognize two-digit `00..99`. Other modern documentation and the frozen project requirement use `01..99`.

Status:

| Question | Status |
|---|---|
| exactly two digits | strong community/reimplementation support |
| `01..99` project range | configured |
| `00` original YR legality | conflicting/underconfirmed |
| one digit | underconfirmed; rejected by strict project grammar |
| three digits | underconfirmed; rejected by strict project grammar |
| leading zero required | configured and supported by exact two-digit grammar |
| extra suffix/prefix characters | not a sequence-family match |
| gaps stop scanning | rejected by project policy |
| numeric versus lexical order | numeric sequence is the canonical project key |

A future profile can opt into `00` without changing the parser or duplicating sorter logic.

## 3. Active probing versus enumeration

Two implementation strategies can produce the same configured result:

- actively probe each allowed sequence number;
- bounded enumeration, strict parse, then numeric sort.

The design prefers bounded enumeration plus normalization because it can diagnose malformed and duplicate names. The result must be identical to a canonical active-probe model for all accepted names.

Synthetic tests randomize enumeration order and compare a canonical hash.

## 4. Duplicate logical sequence

Two files may map to the same sequence because of case variants or filesystem anomalies.

The sequence layer must not select a winner. It returns an ambiguous layer with all provenance and prevents deterministic resolution until policy resolves it.

SHA, file length, modified time, directory order, and path casing are never winner keys.

## 5. `expand`, `ecache`, and `elocal` are separate

Do not apply expansion ordering to other families.

### `expand*`

Numbered patch/mod layer. Strong evidence for descending lookup/highest number first; project stores low-to-high order for composition.

### `ecache*`

Community sources describe wildcard names and, for RA2/YR, behavior influenced by filesystem/alphabetical enumeration. A universal numeric 99→00 rule is not confirmed for the original.

Project options:

- exclude unconfigured `ecache*` from the initial profile;
- or configure an explicit deterministic family order and label it `ConfiguredProjectPolicy`.

Never call a deterministic replacement “confirmed original” without evidence.

### `elocal*`

Likewise commonly wildcard-discovered rather than proven to use expansion numeric semantics. It needs its own descriptor and evidence label.

## 6. `md` versus non-`md`

The YR 1.001 profile scans `expandmdNN`. It does not mix `expandNN` into the same sequence.

If both families are enabled in another profile, the profile must specify:

- separate family ranks;
- allowed version/game scope;
- duplicate logical-name behavior;
- whether they compose or remain isolated.

No “strip `md` and merge sequences” behavior is allowed.

## 7. Sorting key

A normalized expansion priority key can be expressed as:

```text
ContentPriorityKey(
  PolicyProfileId,
  ProviderScope,
  ArchiveFamilyRank,
  SequenceNumber,
  ExplicitMountOrdinal,
  StableLogicalName
)
```

The fields are serialized and traceable. Named family descriptors compile into this key in one component; magic integers must not be scattered through readers and resolvers.

## 8. Diagnostic examples

- `ExpansionSequenceOutsideConfiguredRange`
- `ExpansionSequenceWrongWidth`
- `ExpansionSequenceNonDecimal`
- `ExpansionSequenceDuplicate`
- `ExpansionNameCaseCollision`
- `ExpansionFamilyWrongGameScope`
- `UnconfiguredCacheFamily`
- `UnconfiguredLocalExtensionFamily`
- `EnumerationOrderNormalized`
