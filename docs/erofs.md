# EROFS

`NyaFs` can read EROFS images and write uncompressed EROFS images.

## Commands

Load is auto-detected:

```text
load system.erofs
export out/
```

Write an LZ4-compressed image (incompressible extents fall back to plain):

```text
store out.erofs ramfs erofs
```

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
  - whole-file and tail fragments stored in the packed inode.
- Directories, regular files, symlinks, device nodes, modes/uid/gid.
- Inline, shared and long-prefix filesystem xattrs are read and written (`user.*`, `trusted.*`, `security.*`, `lustre.*`, POSIX ACLs).

## Writer limits

- LZ4, using compact 4B/2B indexes for normal pclusters and full indexes for big pclusters;
- ztailpacking when the final compressed pcluster fits into the inode metadata block;
- whole-file fragments in the packed inode (enable with `EroFsBuilder.Fragments`);
- 4 KiB blocks, 64-byte extended inodes;
- partial tail fragments are reader-only;
- none of the standard EROFS features remain unimplemented for the writer;
- external devices are reader-only for now.

## Not supported yet

- external-device chunk indexes (`device_id != 0`);
- legacy full-index fragment offsets above 4 GiB;
- shared xattr prefixes;
- LZMA/DEFLATE/Zstd writing.

Unsupported compressed data is skipped with a warning; the directory tree is
still shown wherever the metadata can be decoded.

## Verification

- Generated flat, flat-inline, compact, full-index, big-pcluster, ztailpacking,
  chunk-based, fragment-packed, LZ4, LZMA, DEFLATE and Zstd images with
  `mkfs.erofs` (erofs-utils 1.9).
- Checked file contents against the source tree and extent maps with
  `dump.erofs -e`.
- Automated fixtures cover all of the above layouts.
