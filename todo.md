# TODO

## Streaming (done, NyaFs 1.3.0)
All built-in filesystem writers implement `IFilesystemStreamBuilder`; NAND/disk
slots and multi-volume UBI stream via `PlanSlot.WriteData`/`RegionStream`/
`PebStore`. Remaining: FIT/Android/CIMG and vendor containers as large slots
still use the `byte[]` fallback (see `docs/build_plan.md`, «Ограничения»).

## Filesystems
CPIO: (done)
* `CpioFsBuilder` now supports `070702` and raw-record preservation
  (`Raw(CpioNode)`); `NyaFsSwupdate/SwuWriter` uses the shared builder.
  Byte-identical unchanged SWU and dirty-payload rebuilds are covered by tests.
* `SwuWriter` also refreshes `filename`/`sha256` pairs in `sw-description`
  for changed payloads and can re-sign `sw-description.sig` via
  `set container swu.sign-key <profile>`; without a profile it emits a stale-signature warning.
CramFS Big Endian (done)
VFAT, EXT3/EXT4 base (done)
ISO9660 (done)
exFAT (done)
EROFS (reader: uncompressed, LZ4/LZMA/DEFLATE/Zstd, big pcluster, ztailpacking, chunks, fragments; writer: LZ4 compact 4B/2B + big pcluster + ztailpacking + whole-file/tail fragments + inline/shared/long-prefix xattrs with plain fallback; CLI options)

F2FS writer (reader+writer ready; NyaFs 1.0.4x):
* compression: ZSTD/LZ4/LZO/LZORLE done (`set ramfs f2fs.compression zstd|lz4|lzo|lzorle`).
  LZORLE now uses a real LZO1X-RLE stream (`LZO1xRleCompressor`), including zero runs; the decoder handles the `17,1` RLE bitstream header.
* compression supports inode + 2 direct nodes + 2 indirect + double-indirect;
  cluster size configurable: `set ramfs f2fs.compression.cluster 16K|32K|...|1M`
  (log 2..8, fixed previously at log=2/16 KiB).
* unified raw-stream API in `ImageFormat.Compressors` — **optional refactor, not planned**:
  implementations exist (`Lz4Raw`, NyaLZO raw LZO/LZO-RLE, `ZStd` frame) and F2FS
  selects them directly. Revisit only if adding a new F2FS algorithm or
  plugin-based compressors for F2FS.

Detection of filesystem limitations before writing! Uid/Gid, mode.
Scan filesystem: detect elf arch, etc...

## Packages (done; NyaFs 1.3.1)
One package = one file: gzip/zstd(tar) with `PACKAGE` control (key: value, unknown
keys preserved — extensible) and a `data/` tree. Commands:
`pkg info`, `pkg add`, `pkg create` (fs subtree), `pkg createdir` (local dir),
`pkg adddir` (dependency/conflict resolve from a directory), `pkg meta`
(set/remove metadata and repack).
Done:
* xattr via PAX `SCHILY.xattr.*`; long paths (GNU long name); special files
  (char/block/fifo) supported.
* richer metadata: `depends`/`conflicts`/`provides`, version operators
  `>= <= = > <`, circular-dependency detection, `provides` in both installed-DB
  checks and candidate resolution.
* create from a local directory (CLI) and compression choice `gzip|zstd`.
Remaining (not planned): package repository/index; single `pkg add` does not
auto-resolve dependencies by design — use `pkg adddir`.

## Not supported (later)

### Filesystems
EXT3/EXT4 are implemented; Btrfs, XFS are not implemented.

UBIFS: reader + writer.

### Compression
- LZO write (lzop container) — implemented (LZO1X-1 + Adler32).
- Brotli, lzip (.lz), compress (.Z), 7z/LZMA2-container, zpaq, rzip — not implemented.
- XZ write via NyaXZ is LZMA2-only: no BCJ/delta filters, full-reset chunks
  (lower ratio than native xz). Read side (SharpCompress) handles BCJ.
- XZ decompression now uses SharpCompress.XZStream as the primary path; the small
  managed LZMA2 decoder is retained as fallback (it does not maintain history
  across interleaved uncompressed chunks 0x01/0x02 or all reset states). A proper
  stateful LZMA2 decoder remains a separate task.

### Composite / vendor images
OpenWRT.

### Bootloader mkimage images (deferred, samples needed)
Not implemented yet (only enum names exist in `ImageType`/`FitHelper`):
`kwbimage`, `imximage`, `imx8image`, `imx8mimage`, `ublimage`, `omapimage`,
`aisimage`, `pblimage`, `mxsimage`, `gpimage`, `atmelimage`, `socfpgaimage(_v1)`,
`x86_setup`, `lpc32xximage`, `rkimage`, `rksd`, `rkspi`, `zynqimage`,
`zynqmpimage`, `zynqmpbif`, `vybridimage`, `tee`, `pmmc`, `stm32image`,
`mtk_image`, `copro`, `sunxi_egon`, `sunxi_toc0`.

Where to get samples/specs:
- `mkimage -T <type>` reference images (header/layout only; payloads are vendor-specific).
- Vendor GitHub releases, OpenWrt firmware selector, Armbian, board vendor update files.
- These are mostly SPL/U-Boot/bootloader images, not kernel/rootfs; a reasonable first
  step is detect + opaque preserve + extract, then editing if a format is understood.
- Already covered but different formats: Rockchip RKFW/RKnano, Allwinner WTY
  (PhoenixSuit), Android boot/vendor_boot, U-Boot FIT/legacy/env.

Done: bare arm64/riscv `Image`, all legacy uImage header types, U-Boot environment
(`env` first-class + commands in `NyaFsUboot`), Android boot v4 (signature preserved),
Android vendor_boot v3/v4, FIT script, TVIP.

### TAR specifics
Hard links (stored as regular files), sparse files, pax extended headers, GNU sparse.

## Bugfix
Root node mode!..

## Boot sectors / arbitrary image elements

- ISO9660: сохранять загрузочный сектор / El Torito boot image как отдельный
  элемент образа, а не терять при `load`/`store`.
- Зависит от общего хранения произвольных (свободных) типов элементов образа.

## Composite Images
Android boot image: v0-4 reader/writer (v4 boot signature preserved)
Android vendor_boot: v3/v4 (fragments/table/dtb/bootconfig; RW)
TVIP firmware
OpenWRT
Legacy uImage: kernel/kernel_noload/ramdisk/filesystem/flat_dt/multi (RW);
standalone/firmware/script/vendor are opaque payloads (extract/import/passthrough)
FIT: kernel/ramdisk/fdt/script (script is an opaque element; config script link preserved)
U-Boot environment: NyaFsUboot plugin (first-class `load`/`store` + env load/save/list/get/set/unset/import/export/new/size/layout/redundant)
Raw MBR/GPT disk images (done: `DiskImagePlugin` + `DiskImageWriter`, primary + extended MBR + GPT)\nIceRiver `.bgz` (hidden `NyaFsIceRiver` plugin: AES-256-CBC + gzip/tar; read/write; direct `.bgz` extension or `load ... all bgz`; ICM168 3.2.15 / custom KS0 Pro/KS5L/KS5M keys unknown)\nDetectionCapabilities (`Magic`/`Extension`/`ExplicitFormat`) + `ImageData.SourceName` + forced `load ... all <format>`

## Encryption
FIT encryption (AES)

## Plugins
Plugin system
* Change all fs readers/writers to plugin-base system, provided by NyaPlugin class: Provide reader, provide writer
* Change all compressors as plugins
* Change all image formats as plugins
* Change all commands to plugin
Editing filesystem: change users/groups/rights (partial: FsIdentityDatabase, named chown/chgrp, user/group list/add/del)
Lua scripts as commands: open file, edit, save

Filesystem i/o
  [optional] Filesystem compression wrapper [cramfs, squashfs has internal compression, additional compression is not needed]
    [optional] Image


##  Additional images processing
Uboot (TVIP)
Secondary bootloader (Android)
Recovery DTB (Android)
Different resources (TVIP splash)

## Commands
User/Group id change for all system: from -> to


## Remote access
Access, adding and editing files from exist commanders like mc, WinSCP and other:
SCP
SFTP
FTP
NFS

TFTP: return different images based on requested filename

## Links
1. [filesystems info](https://gitlab.arm.com/linux-arm/linux-ae/-/tree/WIP-pmu-nmi/fs)
- Merged BMU writer: done (BmuMergeWriter, inner re-sign, offsets/sizes, whole-file CRC32 with zeroed CRC field).

## Legacy vendor containers
- ABV4 относится к Actions Semiconductor, не к Allwinner LiveSuit; отдельный формат, нужен образец.
- Allwinner LiveSuit / PhoenixSuit / zdisk: старые форматы без доступных образцов; не путать с PhoenixCard (PHOENIX_CARD_IMG) и WTY.
