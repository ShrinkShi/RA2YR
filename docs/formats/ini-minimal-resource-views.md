# Minimal INI Rules and Art resource views

WP-02G2 adds a UnityEngine-free typed projection above a completed WP-02G1
resolution. It does not parse INI bytes independently and cannot accept an
ambiguous or failed resolution.

## Typed scalar boundary

| Kind | Accepted input | Deliberately absent behavior |
|---|---|---|
| Raw bytes | bounded effective winner bytes | decoding and normalization |
| ASCII identifier | explicit printable ASCII token | code-page guesses, spelling repair |
| Boolean | `yes` or `no` under a named case policy | numeric or truthy aliases |
| Non-negative integer | checked decimal `Int32` | signs, clamping, overflow wrapping |
| Identifier list | comma-separated explicit identifiers | empty-item removal and implicit trim |

Every present or invalid scalar retains its raw bytes, winner, overridden
candidates, source ID, logical MIX chain, section physical-line ID, and key
physical-line ID. Budgets limit scalar bytes, list items, source candidates,
registry entries, Art records, and diagnostics.

## Minimal Rules projection

Only the explicit registries `AircraftTypes`, `BuildingTypes`,
`InfantryTypes`, `VehicleTypes`, and `Animations` are projected. Each entry
stores its registry kind, original ordinal-key spelling, checked ordinal,
explicit identifier result, and source trace. Duplicate identifiers are
preserved and make the typed result incomplete; stock duplicate behavior is
not selected. Ordinal spelling is not identity: entries such as `0` and `00`
both parse to ordinal zero. Such entries remain in source order, receive a
`DuplicateRegistryOrdinal` diagnostic within that registry, and never acquire
an implicit first/last winner. Equal ordinals in different registries do not
conflict.

## Minimal Art projection

The fields are `Image`, `Cameo`, `AltCameo`, `Voxel`, `Remapable`,
`NewTheater`, `Palette`, `CustomPalette`, `Buildup`, and `ShadowIndex`.
`CustomPalette` remains raw because its stock semantic grammar is not yet
established. Missing fields remain missing and invalid fields retain raw bytes.

Name comparison is an explicit typed-view policy. If that policy matches more
than one G1-resolved field (for example exact `Image` and `image` values under
an ASCII case-insensitive G2 projection), the field is `Ambiguous`: it has no
single parsed winner, preserves every parsed candidate and source trace in a
canonical order, contributes no resource reference, and cannot determine a
route candidate. The normalized model hash includes the complete ordered
candidate set.

Explicit file extensions are counted but never appended. An explicit valid
`Voxel=yes` produces a VXL route candidate and `Voxel=no` produces an SHP
route candidate; missing or invalid values remain unknown. These are research
candidates, not proof that an asset exists or can be decoded.

## ProjectBaseline boundary

The fixed `expandmd01.mix` and `ra2md.mix/localmd.mix` Rules candidates are
resolved separately under `ConfiguredForTesting`. `artmd.ini` is processed the
same way. Public evidence contains aggregate counts, complete-source coverage,
diagnostic counts, and one-way normalized model hashes only. Object names,
section lists, resource names, values, raw bytes, and host paths are excluded.

Stock archive precedence, full Rules/Art semantics, defaults, fallbacks,
inheritance, SHP/VXL decoding, palette selection, rendering, and gameplay are
not implemented.
