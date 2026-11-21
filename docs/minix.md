# Minix filesystem (read-only)

`NyaFs` reads Minix v1, v2 and v3 images. Writing is not supported.

## Commands

```text
load minix.img
export out/
```

The reader is selected automatically by the Minix superblock magic.

## Reader support

- v1 (`0x137F`/`0x138F`), v2 (`0x2468`/`0x2478`) and v3 (`0x4D5A`);
- v1/v2 1 KiB blocks; v3 1/2/4 KiB blocks;
- 32-byte v1 inodes and 64-byte v2/v3 inodes;
- 14/30/60-character names (16/32/64-byte directory entries);
- direct, single, double (v1) and triple (v2/v3) indirect blocks;
- files, directories, symlinks, FIFOs, block/char devices;
- uid/gid, mode bits and file sizes.

## Limitations

- read-only: no `store`/`build` writer;
- timestamps are not exposed by the reader interface;
- non-zero `log_zone_size` is rejected (the Linux kernel also requires zones
  equal to blocks).

## Verification

- fixtures generated with `mkfs.minix` (v1/v2/v3, 14/30-character names) and
  populated with a small independent script;
- `MinixTests` covers detection, all three versions, long names, nested
  directories, symlinks and file data.
