# exFAT

`NyaFs` supports exFAT images (typical for SD cards and USB drives).

## Commands

Load is auto-detected:

```text
load card.img
```

Write the loaded filesystem as exFAT. Set the target size explicitly:

```text
set ramfs size 64M
store out.img ramfs exfat
```

Without `set ramfs size`, the writer uses at least 64 MiB (or twice the current
content size plus 16 MiB, rounded to the cluster size).

## Layout

- 512-byte sectors and 4 KiB clusters (8 sectors per cluster).
- One FAT with contiguous cluster chains.
- Allocation bitmap and the recommended compressed up-case table.
- Root directory, volume label, file/directory entries, UTF-16 names and
  DOS timestamps.

## Limitations

- exFAT has no Unix permissions, uid/gid or symlinks. The generic model's
  symlinks/special files are written as ordinary zero/byte files.
- The writer uses contiguous allocation and does not emit fragmented files;
  writing, deleting and re-adding in one pass is fine.
- No exFAT encryption, ACLs, TexFAT or transactional features.
- Volume size must be large enough for metadata + files.

## Verification

- Images are checked with `fsck.exfat` (exfatprogs).
- Generated images mount read/write with Linux `mount -t exfat -o loop`.
- Files, directories and nested paths were verified round-trip through the
  reader/writer and through the mounted filesystem.
