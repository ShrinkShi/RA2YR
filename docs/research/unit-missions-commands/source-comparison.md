> **Source notice:** Prepared by **ChatGPT Web** from public sources only. ProjectBaseline was not read. This is not a Codex artifact. GPL and unclear-license implementations are reference-only; no code was copied, translated, mechanically rewritten, or ported (`code_imported: false`).

# Source Comparison and Licensing

## Source classes

| Source | Pin/revision | Category | Product scope | License/boundary |
|---|---|---|---|---|
| EA FinalSun/FinalAlert 2 editor | `6abf0f557469baea73079c6bf6550709e2e3584e` | official editor | TS/RA2/YR map and AI authoring | GPL-3.0-or-later; editor evidence only |
| RA2/YR user manuals | preserved public PDF/manual mirrors | official user documentation | RA2/YR controls and waypoints | behavior documentation; no runtime source |
| OpenRA | `a520984d91eda9de48a62b1d15c1e3bad0d4fb1a` | independent implementation | reimplementation architecture | GPL-3.0-or-later; reference-only |
| Ares docs | versioned 3.0 documentation | extension/runtime patch docs | YR + Ares | extension-only; no vanilla promotion |
| ModEnc Mission Control / ScriptActions | fixed/current revisions where available | community documentation | TS/RA2/YR comparison | community evidence |
| PPM forums | fixed topic URLs | community/tutorial | TS/RA2/YR/Ares | anecdotal and tutorial evidence |
| RA2 DIY | stable tutorial/catalog references where locatable | community | Chinese modding community | reference-only |
| Phobos | versioned docs/source references | extension | YR + Phobos | extension-only |
| Vinifera/TS++ | versioned docs/source references | extension | TS lineage | not transferable to YR by default |
| CnCNet client | pinned client repository where relevant | client/UI | launch/client behavior | not original game runtime |
| Chrono Divide | pinned public SDK where relevant | independent implementation | browser reimplementation | implementation-specific |
| openra2/Vanguard | pinned public source where relevant | reimplementation/shared lineage | RA2-oriented | avoid double-counting lineage |

## Load-bearing findings

### Official manual

Supports:

- S stop movement;
- G guard current area;
- Z waypoint creation/release;
- patrol loops;
- D deploy/eject;
- control groups and type selection;
- H camera-center-on-base.

It does not specify internal enum ordinals, savegame representation, target scan algorithm, chase distance, queue serialization, or network ordering.

### Official mission editor

`ScriptTypes.cpp` exposes editor Team Script actions including Attack, Move, Guard area with duration, Unload, Deploy, Load onto Transport, Patrol, Scatter, and mission assignment. This confirms editor-facing script concepts, not the original unit mission executor or numeric Mission Control enum.

### ModEnc/community

Provides the strongest public cross-game mission-name/ordinal comparison and descriptions of Guard, Sticky, Area Guard, Stop, Ambush, Hunt, Enter, Capture, Unload, Construction, Selling, Repair, Patrol, and later RA2/YR additions. These remain `CommunityDocumented`.

### Ares

Provides explicit extension evidence separating:

- manual targeting from auto-acquire and retaliation;
- Guard versus Area Guard passive acquisition;
- cursor visibility from AI/script permissions;
- manual enter/unload from transport capability;
- passenger filters and size policies;
- passenger survival;
- garrison and deploy cursor conflicts.

No Ares extension is treated as stock YR behavior.

### OpenRA and other reimplementations

Useful for architectural proof that orders, activities, pathing, targeting, cargo, selection, and presentation can be separated. Their class layouts, algorithms, tick rates, RNG, queue semantics, and UI behavior are not Westwood facts.

## License rule

No GPL or unclear-license production logic, enum table, switch, test fixture, or algorithm was copied, translated, or mechanically ported. `code_imported: false`.
