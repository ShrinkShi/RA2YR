# ADR 0004: FinalAlert 2 interoperability baseline

- Status: Accepted
- Date: 2026-08-01

## Context

Map compatibility requires a fixed editor build and a reproducible round-trip
procedure. The editor and maps are proprietary external data.

## Decision

The primary interoperability baseline is the external executable:

- File: `FinalAlert2.exe`
- Observed version: `SZL 1.01` localized build
- SHA-256: `BE939988780428271377C7592E0552E405C5982BA6BB7F468DE76CE5117F619D`

The separate script-localized executable is auxiliary-only. Every test uses
isolated copies and never overwrites an original map. Launching an executable
is a deliberate manual test action, not an automated setup step.

The round trip is: copy source A; save with baseline FA2 as B; read and write B
with the engine as C; reopen, check, and save C with FA2 as D; compare decoded
packs, object/reference graphs, unknown data, and editor diagnostics across
B/C/D.

## Consequences

- A successful file open is not a round-trip pass.
- Unknown sections and opaque byte regions are retained or writing is refused.
- FA2 binaries, configuration, and map copies remain outside Git.

## Verification

- Confirm the executable hash before every certified run.
- Record input/output map hashes and the exact editor build.
- Require no new FA2 map-check issue and no unexplained semantic difference.
