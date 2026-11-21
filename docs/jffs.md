# JFFS (v1)

`NyaFs` reads and writes JFFS version 1 (Axis "Journalling Flash File System")
raw MTD images.

## Commands

```text
load mtd.jffs
export out/
store out.jffs ramfs jffs
```

Geometry:

```text
set ramfs jffs.pagesize 4096
set ramfs jffs.eraseblock 64K
```

`jffs1` is accepted as an alias for the filesystem format.

## Reader support

- 60-byte raw inodes with magic `0x34383931` ("1984"), little- and big-endian;
- byte-sum header/name/data checksums and 4-byte alignment;
- dirents as raw inode names, version selection (`version 0` = newest);
- data reconstruction with `offset`/`rsize` (overwrite, truncate, holes read as zeros);
- files, directories, symlinks (target stored as data), FIFOs, block/char devices;
- uid/gid, mode bits and timestamps;
- `nlink` and parent inode (`pino`) from the newest inode version.

JFFS v1 has no rdev field, so block/char devices are reported with major/minor 0.

## Writer support

- one inode version per file chunk, no garbage collection or bad-block handling;
- no compression (the format has none);
- configurable `pagesize` and `eraseblock`; the image is padded to eraseblock;
- regular files, directories, symlinks, FIFOs and block/char devices;
- hardlinks and xattrs are not emitted.

## Verification

- reader tested with independently generated little- and big-endian fixtures;
- writer output is parsed back by the reader and by an independent Python parser;
- `JffsTests` covers raw read, big-endian read, large files and writer round-trip.
