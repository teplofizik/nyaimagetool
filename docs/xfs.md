# XFS (read-only)

`NyaFs` reads XFS v4 and v5 images. Writing is not supported.

## Commands

```text
load disk.img
export out/
```

The reader is used automatically when `XFSB` is detected. It is also available
inside disk images and other containers.

## Reader support

- v4 and v5 superblocks (`XFSB`), 512-byte sectors, 512..65536-byte blocks;
- inode v1/v2/v3, `NREXT64` extent counters, data forks: local, extents, btree;
- AG-encoded fsblock numbers (`agno << agblklog | agbno`);
- shortform directories, single-block directories and data/leaf/node directories;
- files, directories, symlinks (inline and remote), FIFOs and devices;
- sparse files (holes read as zeros) and unwritten extents;
- `users`/`groups`/mode/size.

## Limitations

- read-only: `store`/`build` with `format: xfs` are not available;
- xattrs/attr fork are not exposed;
- v5 metadata CRCs are not verified;
- project ids and timestamps are not exposed by the reader interface.

## Verification

- fixtures generated with `xfsprogs 6.18` (`mkfs.xfs -p`): v4 300 MiB,
  v5 300 MiB with block/node directories, a 118-extent btree file and sparse files;
- a 72 KiB synthetic image (`xfs_min_sample.img`) for a fast detection/read smoke test;
- `XfsTests` covers v4/v5 detection, shortform/block/node dirs, extents/btree,
  sparse files, symlinks and data hashes.
