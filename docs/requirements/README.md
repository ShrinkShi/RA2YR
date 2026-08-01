# Requirements baseline

`三阶段开发需求分析.md` is the governing requirements baseline for this
project. The repository copy is a mechanical copy of the workspace document;
changes to it require an explicit requirements decision and provenance record.

The current development content source is named
`YR1001_ProjectBaseline`. It maps to the external workspace directory:

`../尤里的复仇-1.001-原版（已加官方地图增补包、音乐包、win10兼容补丁）`

This source includes the official map add-on, music pack, and a Windows
compatibility patch. It is therefore not a clean, unmodified YR 1.001 golden
baseline. The directory remains outside the Git repository, is accessed
read-only, and will be identified by a future content manifest rather than
copied into `Assets` or documentation. Compatibility claims against clean YR
1.001 still require separately recorded hashes, procedures, and evidence.

Original game data, unpacked assets, FinalAlert 2, reference tools, generated
caches, and local golden-test artifacts must remain outside this repository.
