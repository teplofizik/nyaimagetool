# Tandy CoCo Disk BASIC / .dsk (external plugin, read-only)

`NyaFsAncientFs` reads raw Tandy Color Computer Disk BASIC (DECB) images:

```text
load "Robert Rhythm_SS.35T.18S.dsk"
ls /
export out/
```

## Reader support

- 35- or 40-track raw images, 18 sectors of 256 bytes;
- directory track 17, directory sectors 3-11, 32-byte entries;
- granule map in sector 2 as a next-granule chain;
- 9-sector granules, directory track skipped in the chain;
- last-sector byte count stored big-endian in the directory entry;
- BASIC / data / machine-code / text file types, flat namespace.

## Limitations

- read-only;
- only raw `.dsk` images; DMK/IMD are handled by their container readers;
- no os9/FLEX filesystems and no subdirectories;
- ASCII/binary encoding is exposed only as file content.

## Verification

- real `Robert Rhythm_SS.35T.18S.dsk` from Disk-Archive;
- names/sizes/hashes cross-checked with `cocofs` (`MENU.BAS`, `GFX.BIN`);
- `CocoDosTests` covers 12 files, sizes, SHA-256 and pipeline loading.
