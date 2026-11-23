# Amiga OFS/FFS / .adf (external plugin, read-only)

`NyaFsAncientFs` reads standard 880K Amiga ADF images:

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

## Limitations

- read-only;
- no bitmap/free-space accounting;
- soft links and hard-link semantics are not exposed;
- ADF with non-880K geometry and extended/FFS2 variants are not covered yet.

## Verification

- real Amiga Extras ADF and Kaleidoscope ADF from the markaudacity
  Disk-Archive;
- `AdfTests` checks root traversal, nested `Trashcan` content, `.info` hash and
  pipeline loading.
