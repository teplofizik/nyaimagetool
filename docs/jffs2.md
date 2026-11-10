# JFFS2

`NyaFs` reads and writes basic JFFS2 raw MTD images.

## Commands

```text
load mtd.jffs2
export out/
store out.jffs2 ramfs jffs2
```

Compression and geometry:

```text
set ramfs jffs2.compression none|zlib|rtime|lzo|priority
set ramfs jffs2.pagesize 4096
set ramfs jffs2.eraseblock 64K
```

## Reader support

- little- and big-endian JFFS2 node headers;
- dirent and inode nodes with version selection;
- files, directories, symlinks, FIFOs, block/char devices;
- uncompressed, `ZERO`, `RTIME`, `ZLIB` and `LZO` data;
- uid/gid, mode bits and timestamps;
- xattr/xref nodes (user/security/trusted/POSIX ACL prefixes).

## Writer support

- one inode version per object/page chunk, no garbage collection;
- optional `none`, `zlib`, `rtime`, `lzo` and `priority` compression;
- configurable page size and erase-block size;
- cleanmarker nodes at the start of used erase blocks;
- xattrs are emitted as JFFS2 xattr/xref nodes;

## Verification

- Reader tested against `mkfs.jffs2` images with clean and compressed
  (zlib/rtime) file data;
- writer output (including xattrs and cleanmarkers) parsed by `jffs2dump`;
- `Jffs2Tests` covers raw read, compressed read and writer round-trip.
