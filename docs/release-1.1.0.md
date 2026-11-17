# NyaImageTool 1.1.0

## Highlights

- Declarative layout/build plans: `build`, `mkimage`, `layout export`/`layout validate`
  (YAML/JSON, schema 1) for NAND, raw disk (MBR/GPT), standalone filesystems,
  UBI, ISO9660 and vendor containers.
- Filesystems as first-class layout targets: `squashfs`, `ext2/3/4`, `fat`,
  `ubifs`, `jffs2`, `yaffs2`, `f2fs`, `erofs`, `cramfs`, `romfs`, `iso9660`,
  `udf`, `cpio`, `tar`, with `size` (region) and `fs-size` (volume) semantics.
- Multi-volume UBI in one node: UBIFS (dynamic) and raw/static volumes.
- El Torito bootable ISO9660 (no-emulation; selectable media/load parameters).
- RKFW and SWUpdate layouts by template (`options.template`): replace/add
  entries, refresh sizes/hashes/tails.
- `content.compression` for files, blobs and directory-built filesystems
  (cpio ramdisk and similar).
- `mkimage` for a standalone filesystem; save with `store ... ramfs <format>`.
- `layout export` keeps filesystems editable (`<schema>.files/`, `content.dir`)
  while kernels/containers stay immutable blobs.
- JSON/YAML layout examples in `docs/examples/layout/`; plugin contract in
  `docs/fs_plugin.md`.

## Compatibility

- `NyaFs` 1.1.0 package; targets netstandard2.0, net8.0, net9.0, net10.0.
- `NyaImageTool` and bundled plugins: 1.1.0, net10.0.
- Layout schema remains `schema: 1`; unknown keys are preserved.

## Verification

- Development suite: 403/403.
- Repository suite: 319/319.
- Golden regression corpus (`NYAIMAGETOOL_IMAGES`) matches; UBI/UBIFS detection
  chain for single-volume images is unchanged (`ramfs/ubifs`).

## Upgrade notes

- `size` in a layout always means the region/output length (`auto` = natural);
  use `fs-size` for a fixed-size filesystem volume (ext/fat/f2fs/MCU).
- `content.compression` means "compress the resolved bytes"; supply already
  compressed files without it.
- `layout export` now emits `content.dir` for filesystem leaves; device/fifo/
  socket nodes fall back to an immutable blob.

## Known limitations

- Raw NAND OOB/ECC and bad-block handling are not written (linear images only).
- U-Boot `mkimage -T` bootloader formats (kwbimage, imx8image, sunxi, rksd, ...)
  are not implemented.
- Secure boot/AVB/vbmeta and encrypted vendor containers are not a generic layer;
  FIT/SWUpdate signing is available, vendor encryption is plugin-specific.
- UDF boot (El Torito is ISO9660-only); OpenWrt composite, Btrfs/XFS, LZO write
  container are not implemented.
- Builders are in-memory; very large disk/NAND images are limited by RAM.
