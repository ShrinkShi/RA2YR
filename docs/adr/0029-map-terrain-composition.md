# ADR-0029: Read-only map terrain composition remains profile-explicit

Status: Accepted for synthetic/configured implementation.

M3-C7 composes existing IsoMapPack5, OverlayPack/DataPack, PreviewPack,
theater registry, and TMP raw results into an immutable candidate document.
Tile identity, Overlay storage order, ramp and terrain interpretation remain
explicit policy choices. Missing resources and incomplete bindings are retained
as structured status; they are not repaired or promoted to original-runtime
truth. The model remains UnityEngine-free and does not render, write, or run
gameplay.
