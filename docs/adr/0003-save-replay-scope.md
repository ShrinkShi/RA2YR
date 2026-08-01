# ADR 0003: Save and replay scope

- Status: Accepted
- Date: 2026-08-01

## Context

The first phase requires complete deterministic persistence and replay, but
binary interoperability with the original game's `.sav` format is a separate
reverse-engineering problem.

## Decision

Phase one implements the engine's own complete, versioned save format and
deterministic replay format. They include all authoritative state needed for
continuation: random state, AI, triggers, scenario variables, visibility,
pending commands, and content identity.

Reading or writing the original YR binary `.sav` format is not required in
phase one. It is recorded as a later research item and must not be implied by
generic save/load compatibility claims.

## Consequences

- Save schemas are chunked, explicitly versioned, and migration-aware.
- Replays contain deterministic input plus versioned checkpoints and hashes.
- Compatibility documentation distinguishes engine persistence from original
  binary save interoperability.

## Verification

- Save at arbitrary ticks, load, and continue with hashes identical to an
  uninterrupted run.
- Replay the same command stream at multiple render frame rates with identical
  hashes.
- Reject incompatible content or schema versions with actionable diagnostics.
