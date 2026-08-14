# ADR-0045 - M5-C6 integrated headless skirmish foundation

The first integrated M5 loop remains a bounded synthetic reference. Economy
transactions use `EconomyAuthority`; manual and computer actions use the
existing `CommandRequest` contract; combat resolves simultaneous structure
damage and explicit winner/defeat state. Determinism is checked through a
canonical state hash. No stock YR victory semantics, renderer, map loader,
network/replay format, or ProjectBaseline packed data is introduced.
