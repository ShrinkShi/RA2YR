# External content foundation

WP-01 establishes a read-only boundary for user-supplied game data. It does
not import files into Unity and does not implement MIX archives, override
resolution, cache writes, or any gameplay behavior.

## Configuration

`Config/ExternalContent.example.xml` is the tracked schema example. A local
operator copies its structure to `Config/ExternalContent.local.xml`; that
local file is ignored by Git because it can contain machine-specific paths.

Paths are resolved relative to the configuration file. Source and cache paths
must be outside the formal repository, must not contain the repository, and
must not traverse an existing reparse point. The supplied formal repository
root must already exist as a directory; a missing or file-valued root fails
before configuration content is read. Existing path ancestors are inspected
one by one, and metadata access failures are rejected rather than treated as
missing paths. Source IDs are stable provenance
identifiers. Priority records the intended future override order; WP-01 sorts
sources deterministically but does not yet resolve duplicate logical files.

On Windows, lexical checks are supplemented by DOS-device identity comparison;
device-namespace, SUBST, possible 8.3 short-name, and UNC paths fail closed.
Non-Windows identity currently falls back to normalized lexical comparison and
is a known WP-01 limitation until a platform realpath/device implementation is
added. This does not permit repository-internal content on those platforms.

The current development content source is named `YR1001_ProjectBaseline` and
retains `ContentSourceKind.Patched`. It maps to the external workspace directory
`../../尤里的复仇-1.001-原版（已加官方地图增补包、音乐包、win10兼容补丁）`
when referenced from a configuration under `RA2YR/Config`. The path is local
configuration data, not repository content. Because this source contains the
official map add-on, music pack, and Windows compatibility patch, it is not a
clean YR 1.001 golden baseline and cannot by itself support a clean-original
compatibility claim.

## Index and manifest

The index recursively discovers regular files without following reparse
points. It records only source ID, normalized relative path, length, and
lowercase SHA-256. Sources and files use explicit ordinal ordering. Hashing
opens files for read access and verifies length and last-write time before and
after the read. A second tree-wide metadata snapshot detects ordinary file
addition, deletion, rename, length change, and timestamp change during the
scan. An unstable or unreadable source produces a structured error and cannot
produce a canonical manifest. Operators must still keep sources stationary:
portable file APIs cannot provide an atomic snapshot against adversarial
same-size, timestamp-preserving concurrent mutation.

Canonical manifest JSON contains source kind, priority, declared version,
source fingerprint, and per-file metadata. It deliberately excludes absolute
content roots, timestamps, and file bodies. A manifest with indexing errors
must not be accepted as compatibility evidence.

Production construction is restricted to the default read-only SHA-256
indexer. Digest injection and index-result constructors are internal test
hooks exposed only to the EditMode test assembly. Result completeness is
derived from source results and diagnostics, while every supplied source
fingerprint is recomputed from canonical metadata before acceptance.

## Verification boundary

Public tests create their own small files under the operating-system temporary
directory and remove them afterward. Real YR data is not copied into tests or
test reports. Local golden runs may record hashes and derived counts only and
remain outside public CI.
