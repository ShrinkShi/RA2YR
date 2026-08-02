# Westwood MIX container research

## Scope and evidence boundary

WP-02C implements an independent Apache-2.0 C# reader, rebuilding writer,
encrypted-directory compatibility, checksum verification, bounded nesting,
and content-source mounting. It does not implement PAL interpretation or any
other payload format.

The format conclusions below were cross-checked against:

- XCC Utilities SourceForge `XCC_Source.zip` and SVN r1201, both GPLv2 and
  retained outside the repository for reference only;
- OmniBlade/xcc `encoding` commit
  `62bb77080f13bdf65c79c84837b7cc264bdd432d`, also reference only;
- the statically identified local XCC Mixer and later black-box tests that use
  only copies or independently generated payloads;
- read-only structural observations from `YR1001_ProjectBaseline`;
- independently generated synthetic archives and public Blowfish vectors.

No XCC source, translation, mechanical rewrite, binary, or original game body
is included in this repository. Source identities and license conclusions are
recorded in `docs/third-party/sources.yml`.

## Container layouts

All numeric fields below are little-endian.

### Classic header

The classic form starts with a 6-byte header:

| Field | Type | Meaning |
|---|---|---|
| file count | `u16` | Number of 12-byte directory entries |
| data size | `u32` | Exact byte length of the payload region |

The directory immediately follows. Each entry contains a `u32` file ID, a
`u32` payload-relative offset, and a `u32` length. The payload begins after all
directory entries. A zero-count classic archive cannot be distinguished from
the extended marker by the normal header discriminator; the Core parser must
therefore report the chosen empty-archive policy explicitly.

### Extended header

The extended form starts with a 32-bit flags word. Confirmed flags are:

- `0x00010000`: a 20-byte checksum trailer is present;
- `0x00020000`: the header and directory are encrypted.

Every other flag bit is unsupported and must fail closed. An unencrypted
extended archive stores the same 6-byte count/data-size header and 12-byte
entries immediately after the flags word. Payload-relative offsets remain
relative to the first payload byte.

An encrypted extended archive stores an 80-byte key-source block after the
flags. The encrypted header/directory span is
`roundUp(6 + 12 * fileCount, 8)`. Only that span is encrypted; entry payloads
are not. The payload starts at
`4 + 80 + roundUp(6 + 12 * fileCount, 8)`.

Eight-byte padding is required only to complete encrypted Blowfish blocks.
Some XCC code observes or emits 16-byte-aligned payload offsets, but the
evidence shows this is an identification heuristic or writer choice, not a
universal format requirement.

## Filename IDs

YR/TS/RA2 IDs use a reflected CRC-32 over an ASCII filename after these
transformations:

1. uppercase ASCII without using the current culture;
2. replace `/` with `\\`;
3. apply the XCC-observed four-byte completion rule;
4. calculate the CRC over the resulting bytes.

The hash function consumes the entire supplied name. It does not select a
basename. Callers that want basename semantics must make that choice before
calling the ID function. Unsupported non-ASCII input is rejected rather than
being converted through a host code page.

Candidate fixed vectors include:

| Logical name | ID |
|---|---:|
| `isotem.pal` | `0x5F9D97B9` |
| `temperat.pal` | `0x9C58DE40` |
| `unittem.pal` | `0x63DA7359` |
| `rulesmd.ini` | `0x8218F9F4` |
| `artmd.ini` | `0x5B47D8D5` |
| `ai.ini` | `0x9E11E49A` |
| `ra2md.csf` | `0xBD835079` |
| `foo/bar.bin` | `0x153A4115` |
| `foo\\bar.bin` | `0x153A4115` |

The `ra2md.csf` vector is present in the unencrypted `langmd.mix` directory in
the controlled baseline. All vectors still require synthetic tests and XCC
black-box confirmation before `format.mix-filename-id` is promoted.

The pinned OmniBlade `encoding` commit contains a relevant evidence conflict:
its uppercase helper returns a new string, while the MIX call site does not
capture that return value. SourceForge r1201 performs the intended in-place
uppercase operation and slash normalization. The independent implementation
uses the cross-confirmed intended behavior and records the pinned commit's
regression rather than reproducing it.

## Encrypted directory

The 80-byte key source is processed as two little-endian modular-exponentiation
blocks using the published Westwood modulus and exponent 65537. Each block
produces a fixed-width little-endian result; the first 56 combined bytes form
the Blowfish key. This Westwood key-source envelope is separate from generic
Blowfish.

The directory uses standard 16-round Blowfish with 64-bit blocks and no IV.
Generic Blowfish is implemented independently from the public, license-free
algorithm definition and verified with its public vectors. Westwood word and
byte ordering receives separate tests against encrypted synthetic data and
the baseline.

XCC can rebuild an encrypted directory when an existing 80-byte key source is
available. No evidence was found that it generates a new valid key source.
WP-02C may therefore support encrypted writing only with an explicitly
supplied/reused key source; it must not claim arbitrary key-source generation.

## Checksum

The checksum flag adds exactly 20 bytes after the declared payload region. A
read-only check of the unencrypted baseline archive `langmd.mix` confirmed
that its trailer exactly equals SHA-1 over the payload region alone. The
header, directory, flags, and trailer are excluded.

The inspected XCC reader accounts for the trailer length but does not validate
the digest, and the inspected XCC editor disables checksum output on save.
These XCC behaviors cannot be used as checksum correctness evidence. The Core
implementation must calculate and compare SHA-1 itself and the writer must
calculate a fresh trailer from bytes it emitted.

## Baseline pre-implementation observation

The root of `YR1001_ProjectBaseline` contains eight `.mix` files totaling
664,471,054 bytes. Seven use the extended form: one declares checksum only,
two declare encrypted directories only, and four declare both. The eighth is
a zero-byte file and is classified as truncated/placeholder input, not a legal
empty archive. No classic root archive was observed.

This is a root-level structural observation, not a completed MIX parse. Entry
counts, target locations, nested archives, checksum results, and compatibility
status remain pending the bounded implementation and local audit.

## Required strictness

The parser must reject or diagnose truncated headers/directories/data,
unsupported flags, count or layout overflow, negative or out-of-range entry
values, duplicate IDs, overlapping entries, failed decryption, failed
checksums, and unexpected trailing bytes. A corrupt archive cannot produce a
trusted partial success.

Unknown IDs remain numeric IDs. A separate candidate-name catalog may resolve
them, but no filename is invented. Nested archives are parent-window bounded,
budgeted by depth/archive/entry totals, and retain every container hop in
provenance.

The writer always performs a complete rebuild to an approved external test or
cache location. It never modifies `YR1001_ProjectBaseline` in place.

