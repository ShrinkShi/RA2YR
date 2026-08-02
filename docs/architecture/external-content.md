# External content foundation

WP-01 establishes the read-only boundary for user-supplied game data. WP-02A
adds directory-source logical resolution, deterministic override precedence,
provenance, and repository-external resolved manifests. Neither work package
imports game files into Unity or implements MIX payload reading, format
semantics, maps, rendering, or gameplay behavior.

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
identifiers. Larger numeric priority wins. Only enabled sources participate.
If two different sources share the highest priority for a logical file, the
result is explicitly ambiguous and incomplete; a source ID is never a hidden
tie-breaker. Full precedence semantics are documented in
[`content-resolution.md`](content-resolution.md).

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

## Directory index

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

The directory source is one internal implementation of a source interface
that can later admit MIX, encrypted MIX, map-embedded, mod-overlay, and
synthetic sources. Those providers are not implemented by WP-02A. The
interface does not grant callers a way to mark arbitrary results complete.

## Resolved manifest and public summary

A complete resolved manifest records safe source metadata, each selected
logical file, its actual source-relative case, size, SHA-256, and the complete
ordered provenance chain including overridden candidates. It deliberately
excludes content roots, host absolute paths, timestamps, and file bodies.
Ambiguous, conflicted, unstable, unreadable, or otherwise incomplete results
cannot be serialized.

The full file-level manifest is content-addressed and written atomically below
the configured repository-external cache. Existing bytes at the same content
address must match exactly. A separate public summary may contain totals,
extension aggregates, scan timestamps, manifest SHA-256, diagnostics count,
and a small approved representative set; it may not contain the full file list
or reconstructable content.

Reparse points are checked before and after ordinary cache directory and file
operations, and file failures produce a safe structured diagnostic. These are
portable path-based operations, not handle-relative transactions. The mounted
cache and ignored `TestResults` ancestry must remain under the local operator's
control and stationary during a run; concurrent adversarial junction
replacement is outside the current boundary and is recorded as a limitation.

Production construction is restricted to the default read-only SHA-256
indexer. Digest injection and index-result constructors are internal test
hooks exposed only to the EditMode test assembly. Result completeness is
derived from source results and diagnostics, while every supplied source
fingerprint is recomputed from canonical metadata before acceptance.

## Verification boundary

Public tests create their own small files under the operating-system temporary
directory and remove them afterward. Real YR data is not copied into tests or
test reports. The controlled ProjectBaseline run reads source bytes only to
compute SHA-256, writes its complete manifest outside the repository, and
publishes only a reviewed sanitized summary. Public CI remains synthetic.
