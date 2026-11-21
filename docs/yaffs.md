# YAFFS (v1)

`NyaFs` reads and writes classic YAFFS version 1 raw NAND images.

## Commands

```text
load nand.yaffs
export out/
store out.yaffs ramfs yaffs
```

`yaffs1` is accepted as an alias for the filesystem format.

YAFFS1 has no superblock magic. The reader auto-detects the common page/OOB
pairs (512/16, 2048/64, 4096/128, 8192/256, 16384/512) from the classic tags
stored in the spare area (bytes 0-3, 6-7 and 11-12).

## Reader support

- classic 16-byte spare layout: `page_status`, block status, 8 tag bytes, ECC1/ECC2;
- object headers with the same field offsets as YAFFS2 (name, mode, uid/gid,
  times, `file_size_low`, alias, rdev);
- duplicate object headers/data chunks resolved by the 2-bit serial number,
  as the kernel does;
- files, directories, symlinks, FIFOs and block/char devices;
- deleted chunks (`page_status`) and unlinked/deleted parents are excluded;
- file size from `file_size_low`, falling back to the highest data chunk.

## Writer support

- 512-byte data chunks / 16-byte spare by default (page/OOB are configurable);
- classic tags with serial 0, SmartMedia ECC over 256-byte halves and tags ECC;
- regular files, directories, symlinks, FIFOs and block/char devices;
- no garbage collection, bad-block relocation or checkpoint data;
- hardlinks and xattrs are not emitted; output is little-endian.

## Verification

- reader tested against an image produced by upstream `mkyaffsimage`;
- writer OOB (tags + ECC) is byte-identical to `mkyaffsimage` for the same data;
- `YaffsTests` covers read, detection, large files and writer round-trip.
