# Macintosh File System (MFS, external plugin, read-only)

`NyaFsAncientFs` adds read-only support for MFS, the flat 400K floppy
filesystem introduced with the original Macintosh in 1984. No core `NyaFs`
change or version bump is required.

## Commands

```text
load mfs_sample.img
ls /
export out/
```

The plugin detects the `D2D7` master directory block signature at block 2.

## Reader support

- 512-byte container blocks;
- the MDB at block 2 and the packed 12-bit VABM that follows it;
- variable-length directory records (51-byte fixed part + Pascal name);
- allocation-block chains for data forks;
- exact logical file lengths.

## Limitations

- read-only: no `store`/`build` writer;
- MFS is flat; Finder folders are metadata only, all files are at the root;
- resource forks are not exposed;
- no POSIX permissions or symlinks;
- only 512-byte container blocks are supported (the only real-world layout);
- VABM/checksum validation is intentionally light.

## Verification

- a synthetic 400 KiB fixture generated from the MFS structures used by Apple's
  `MFSLives` sample (MDB, VABM, directory block, multi-block data fork);
- `MfsTests` covers detection, flat directory listing, exact sizes and data hash;
- CLI smoke test confirmed plugin auto-loading from `plugins/`.
