# ADR-0040 - M5-C1 economy authority

Credits are authoritative session state and may change only through checked,
stable `EconomyTransaction` values. Authored house/alliance metadata remains
separate from runtime player state; directed alliances are not symmetrized.
No ProjectBaseline data or original-runtime economy claim is added.
