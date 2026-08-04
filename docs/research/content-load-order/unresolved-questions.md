# Unresolved questions and decision gates

## 1. Numbered archive grammar

1. Does original YR 1.001 recognize `expandmd00.mix`?
2. Are one-digit or three-digit variants ignored or treated as another family?
3. Is matching strictly ASCII case-insensitive, filesystem-dependent, or hash-based?
4. How are two case variants handled on a case-sensitive filesystem?
5. Did the executable actively probe numbers or enumerate then sort?
6. Do gaps always continue in every original/version profile?

The project answers these only through `ConfiguredProjectPolicy`; original status remains separate.

## 2. Cache/local extension families

7. What exact suffix grammar is used by `ecache*` and `elocal*` in original RA2/YR?
8. Is their order numeric, lexical, directory enumeration, or fixed-load-call order?
9. Are `md` and non-`md` variants both loaded by YR 1.001?
10. Can the same sequence/name occur in both families, and which wins?
11. Are these families root-only?

Until resolved, do not reuse the expansion sorter.

## 3. Loose files

12. Which original file types are searched loose?
13. Are loose INIs, maps, SHPs, VXLs, audio, CSF, and theater files uniform?
14. Are development switches or command-line modes required?
15. Does filename hashing/case normalization differ for loose and MIX candidates?
16. Are language/cache resources exceptions?
17. How exactly are maps written by FinalAlert discovered?

Project loose-provider behavior is configured and scope-limited.

## 4. Nested mounting

18. Which child archive names are explicitly mounted by RA2 and by YR?
19. Is any arbitrary nested MIX recursively mounted?
20. What is the original order of multiple child archives inside one parent?
21. Does parent layer always outrank child role/depth?
22. How are duplicate physical mounts handled?
23. Are maps/movies/theme packages root-discovered, parent-mounted, or installer-specific by edition?

Generic recursion remains disabled.

## 5. INI composition

24. Does vanilla YR automatically compose every same-named INI found across all mounted MIX layers?
25. Which logical documents use section/key overlay versus one selected file plus explicit overlays?
26. Do Rules, Art, Sound, AI, UI, theater, game-mode, and map INIs share one path?
27. What comparer is used for section/key identity?
28. What is the exact same-document duplicate-key rule?
29. Does an empty high-layer value override, delete, reset, or fail?
30. Do numbered-list sections inherit by numeric key, replace a list, append, or stop at gaps?
31. How are explicit mode/map overlays ordered relative to archive layers?
32. Does `rulesmd.ini` in `expandmd01` function as a complete base, patch layer, or both by content/version?
33. Does `soundmd.ini` use specialized list/reset behavior?

The target project policy is ordered multi-document composition regardless of unresolved vanilla proof.

## 6. Ares/Phobos

34. Which Ares version changes language versus expansion precedence?
35. How are include cycles and duplicate includes handled?
36. Which Phobos `<default>`/clear markers apply to which typed lists?
37. Can extension profiles safely compose with vanilla archive layers without changing base provenance?
38. Do extensions introduce additional root MIX patterns?

Extension behavior requires explicit profiles.

## 7. Official editor/tool boundary

39. Which FinalAlert loading behaviors are editor-only?
40. Does FinalAlert write loose maps directly into a game-discovered location or maintain its own catalog?
41. Which bundled XCC package choices are inherited rather than independently authored?

Official tool source does not automatically prove runtime behavior.

## 8. Language and localization

42. Exact precedence among `language.mix`, `langmd.mix`, expansion CSF, loose CSF, and extension packages remains unresolved.
43. Whether language packages participate in ordinary generic lookup or a specialized localization loader remains unresolved.

## 9. Modern provider boundary

44. How should signed/manifest modern packages authenticate and declare scope?
45. Can modern packages override legacy INIs, or must that require explicit user policy?
46. How are provider-version migrations reflected in canonical priority keys?
47. Should generated caches ever be resolvable content or only implementation artifacts?

## 10. Decision levels

### A — original/official confirmed

Requires original executable/editor source directly covering the behavior, official documentation with unambiguous semantics, or approved black-box evidence that distinguishes alternatives.

### B — multiple independent reimplementations

Supports a strong candidate but remains `ConfirmedByMultipleIndependentImplementations`, not original.

### C — configured policy

May be implemented deterministically when needed by the project, with full provenance and an explicit policy/version label.

### D — implementation-specific extension

Ares, Phobos, Chrono Divide, OpenRA, and CnCNet behaviors are opt-in profiles or references only.

### E — unresolved

Preserve candidates and diagnostics; do not guess.

## 11. Prohibited shortcuts

- choose files by SHA, length, timestamp, or local path;
- stop expansion scanning at a gap;
- apply one numeric rule to all wildcard families;
- recurse every nested MIX;
- call FinalAlert/XCC browsing runtime behavior;
- produce a whole-file winner for configured composable INIs;
- concatenate INI bytes;
- globally delete on empty value;
- globally append/renumber numeric lists;
- conflate same-document duplicates with cross-layer overrides;
- claim configured behavior is original source fact;
- publish original entry or INI content.
