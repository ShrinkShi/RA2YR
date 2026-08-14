# ADR-0046 - M5-C7 bounded performance and correctness closeout

M5-C7 uses a bounded synthetic harness to exercise the M5 authority and
proposal contracts at 500, 1000, and 2000 entities. Read-only observations are
collected before deterministic ordered commits. The harness records operation
budgets, descriptor-cache use, direct factory/prerequisite counters, spatial
queries, targeting candidates, autonomy proposals, aggregate snapshots, and a
canonical state hash.

The harness is evidence for deterministic bounded project behavior only. It is
not a stock YR performance result, map simulation, renderer, replay format, or
M6 implementation. No ProjectBaseline packed data is read.
