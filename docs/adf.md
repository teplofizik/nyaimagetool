# Amiga OFS/FFS / .adf (external plugin, read/write)

`NyaFsAncientFs` reads and rebuilds standard 880K Amiga ADF images:

```text
load extras.adf
ls /
export out/
```

## Reader support

- `DOS\0` (OFS) and `DOS\1` (FFS) boot blocks;
- root block (`T_HEADER`), root hash table and `nextSameHash` chains;
- directory data-block lists, file header blocks and `ST_FILE`/`ST_DIR`;
- FFS data-block lists and file extension blocks;
- OFS 24-byte data-block headers with `nextData` chains;
- file sizes and access bits mapped to POSIX modes.

## Writer support

`store` creates a fresh 880K FFS image with root block 880, a root hash table
and flat files of up to 72 data blocks each. The allocation bitmap is emitted
for compatibility; subdirectories are not written.

## Limitations

- writer supports only FFS, flat root files and files up to 72 blocks;
- reader does not expose soft links/hard-link semantics;
- ADF with non-880K geometry and extended/FFS2 variants are not covered yet.

## Verification

- real Amiga Extras ADF and Kaleidoscope ADF from the markaudacity
  Disk-Archive;
- `AdfTests` checks root traversal, nested `Trashcan` content, `.info` hash and
  pipeline loading;
- `Adf_RebuildsImage` verifies the fresh FFS writer and reader round-trip.
