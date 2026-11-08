# EROFS

`NyaFs` can read EROFS images (read-only filesystem).

## Commands

Load is auto-detected:

```text
load system.erofs
export out/
```

EROFS is read-only; `store ... erofs` is not implemented.

## Supported layout

- Superblock at 1024 bytes, 4 KiB blocks (block size from the superblock).
- Compact and extended inodes.
- Uncompressed `FLAT_PLAIN` and `FLAT_INLINE` data, including inline tails.
- Chunk-based inodes:
  - 4-byte block map and 8-byte chunk indexes;
  - 48-bit block addresses;
  - holes (`0xFFFFFFFF`) and chunks on the primary device.
- Compressed inodes:
  - full/legacy indexes and compact 4B/2B indexes;
  - LZ4/LZ4HC, LZMA (MicroLZMA), DEFLATE and Zstd streams;
  - big pclusters (`-C`);
  - `ztailpacking`;
  - whole-file fragments stored in the packed inode.
- Directories, regular files, symlinks, device nodes, modes/uid/gid.
- Inline filesystem xattrs are not exposed in the generic model yet.

## Not supported yet

- external-device chunk indexes (`device_id != 0`);
- partial tail-fragment mappings (some layouts need the packed-inode tail);
- shared xattr prefixes;
- writing/rebuilding EROFS.

Unsupported compressed data is skipped with a warning; the directory tree is
still shown wherever the metadata can be decoded.

## Verification

- Generated flat, flat-inline, compact, full-index, big-pcluster, ztailpacking,
  chunk-based, fragment-packed, LZ4, LZMA, DEFLATE and Zstd images with
  `mkfs.erofs` (erofs-utils 1.9).
- Checked file contents against the source tree and extent maps with
  `dump.erofs -e`.
- Automated fixtures cover all of the above layouts.
