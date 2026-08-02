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

Controlled XCC Mixer 1.47 output established that the extended flags word may
be zero. XCC then emits the count/data-size header at offset 4. The parser uses
an explicit ambiguity rule: six zero bytes are the classic empty archive;
seven through nine zero bytes are malformed classic input; ten or more bytes
with a zero first word select the XCC extended form. This rule cannot prove the
intent of an artificial zero-count classic archive that also carries unused
data, so that construction remains an acknowledged ambiguity rather than a
hidden heuristic.

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

The vectors pass independent C# tests. The seven target IDs resolve entries in
the controlled baseline, and the XCC-created synthetic archive resolves its
three supplied names plus `local mix database.dat` to the same IDs. The latter
database has ID `0x366E051F`.

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

## Controlled baseline audit

The read-only `YR1001_ProjectBaseline` audit found eight root MIX files totaling
664,471,054 bytes. Seven parsed and one zero-byte `movmd03.mix` failed with a
truncated-header diagnostic. Recursive bounded mounting observed 55 archives:
2 classic, 53 extended, 23 with encrypted directories, 46 with checksums, and
48 nested archives at maximum depth 1. The mounted catalogs contain 13,281
entries; five IDs remained unnamed by the controlled candidate catalog.

All seven required target names were located. `isotem.pal`, `temperat.pal`,
and `unittem.pal` resolve through `ra2.mix/cache.mix`; `ai.ini` resolves through
`ra2.mix/local.mix`; `artmd.ini` resolves through `ra2md.mix/localmd.mix`;
`ra2md.csf` is in root `langmd.mix`. `rulesmd.ini` has two distinct candidates,
one in `expandmd01.mix` and one in `ra2md.mix/localmd.mix`. The audit reports
that ambiguity and does not invent unverified archive-layer precedence.

The complete 7,683,713-byte audit manifest remains in the repository-external
cache. Its SHA-256 is
`d2ca24651d68fa1ae1df90b366cd20f07d67889d5f0b9f5ccc7f9278ba8321d4`.
Only aggregates, target IDs, sizes, hashes, container chains, and diagnostics
are published. This patched baseline audit is not a clean YR 1.001 original
comparison and does not interpret any payload format.

## Controlled XCC observations

XCC operations used only a copied tool, a copied baseline archive, or
autonomous synthetic payloads outside the repository.

- XCC opened a copy of baseline `langmd.mix`, displayed 10 entries, and
  extracted `ra2md.csf` with the same length and SHA-256 as the bounded mount.
- XCC created a zero-flag extended archive from three nonempty synthetic files
  and added `local mix database.dat`. The project parsed all four entries.
- `PreserveEntryOrder` retained the observed four-entry order and payload
  hashes. Its bytes differed from the XCC archive, so byte identity is false.
- XCC opened and extracted project-generated classic, checksum, encrypted-
  directory, inner, and outer nested archives. Thirteen extracted files,
  including three zero-byte entries, matched the autonomous inputs byte for
  byte.
- XCC ignored a zero-byte file during archive creation. The controlled create
  contract therefore uses a one-byte file, while project writer and XCC
  extraction coverage for zero-byte entries remain separate.

These observations establish semantic interoperability for the tested
synthetic contracts. They do not establish byte-for-byte XCC writer cloning,
clean original-game behavior, or general acceptance of malformed archives.

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
