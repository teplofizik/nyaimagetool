# YAFFS2

`NyaFs` reads and writes basic YAFFS2 raw NAND images.

## Commands

```text
load nand.yaffs2
export out/
store out.yaffs2 ramfs yaffs2
```

YAFFS2 has no superblock magic; the reader auto-detects the common
page/OOB pairs (512/16, 2048/64, 4096/128, 8192/224, 16384/448) and endianness
from the YAFFS2 tags.

## Reader support

- object headers, data chunks, sequence selection and latest-version reconstruction;
- files, directories, symlinks, FIFOs and block/char devices;
- uid/gid, mode bits and timestamps;
- deleted/unlinked objects are excluded;
- orphaned data chunks without headers are ignored.

## Writer limits

- one version of each object; no garbage collection, bad-block relocation or OOB ECC layout;
- default geometry is 2048-byte pages / 64-byte OOB;
- regular files, directories, symlinks, FIFOs and block/char devices;
- hardlinks and xattrs are not emitted.

## Verification

- Reader tested against `simul_1/snapshot_13_truncate_lorem_ORPHAN.bin`
  from `hashment/yaffs2-forensic-tool`;
- writer output parsed and restored by the same forensic tool;
- `Yaffs2Tests` covers read, detection and writer round-trip.
