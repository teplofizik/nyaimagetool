# EROFS

`NyaFs` can read EROFS images (read-only filesystem).

## Commands

Load is auto-detected:

```text
load system.erofs
export out/
```

EROFS is read-only; `store ... erofs` is not implemented yet.

## Supported layout

- Superblock at 1024 bytes, 4 KiB blocks.
- Compact and extended inodes.
- `FLAT_PLAIN` and `FLAT_INLINE` data layouts, including inline tails.
- Directories, regular files, symlinks, device nodes and file modes/uid/gid.
- Inline xattrs are not exposed in the generic model yet.

## Not supported yet

- compressed inodes (LZ4/LZMA/DEFLATE/Zstd `z_erofs` maps);
- chunk-based inodes;
- shared xattrs and xattr prefixes;
- writing/rebuilding EROFS.

Compressed files are shown in the tree but their content is empty (a warning is
printed). Real Android/OpenWrt EROFS images are often compressed, so this is the
next step for full compatibility.

## Verification

- Generated uncompressed images with `mkfs.erofs` (erofs-utils) load correctly.
- Flat-inline files spanning multiple blocks read correctly.
- Directory names, symlinks and metadata were checked against `dump.erofs`.
