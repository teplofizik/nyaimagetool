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
EXT3/EXT4, XFS, Minix, NTFS and HFS are implemented; CP/M and MFS are external plugins; Btrfs is not implemented.

UBIFS: reader + writer.

### Compression
- LZO write (lzop container) — implemented (LZO1X-1 + Adler32).
- Brotli, lzip (.lz), compress (.Z), 7z/LZMA2-container, zpaq, rzip — not implemented.
- XZ write via NyaXZ is LZMA2-only: no BCJ/delta filters, full-reset chunks
  (lower ratio than native xz). Read side handles LZMA2, BCJ, Delta and verifies
  CRC32/CRC64/SHA-256.
- XZ decompression: NyaXZ uses its own stateful LZMA2/XZ decoder (no SharpCompress)
  — multi-block, LZMA2 continuation and uncompressed chunks, BCJ/Delta filters,
  verified CRC32/CRC64/SHA-256. Concatenated `.xz` streams after the first one are
  ignored.
- NyaFs no longer references SharpCompress: SquashFS/CramFS zlib uses SharpZipLib
  (already required for BZip2/ZIP) and SquashFS xz uses NyaXZ.
- Keenetic/ZNBG writer: SquashFS compression and block size from the source image
  are preserved in NyaFs 1.4.6+ (xz + 512 KiB in the tested 2.04 image), so a
  dirty rootfs can be rebuilt in place. TLV.NDM signatures are still not verified
  (read-only).

### Composite / vendor images
OpenWRT.
D-Link SHRS (DIR-3040): private decryptor done (`NyaFsDLinkEncrypted`, SHRS/AES-128-CBC, SHA-512 checks; key not public). Remaining: signature verification, safe re-encrypt/re-sign (dlink-decrypt has an encrypt mode but signatures are fake), and corpus samples.
D-Link: reader for WRGG/HDR0/cr6b/uImage done. Remaining: WRGG/HDR0 payload decryption (keys), RU signature verification/regeneration (MD5 + salts), and write support for uImage+rootfs images.
Keenetic: legacy ZNBG reader/writer done; `TLV.NDM` reader (marker, 0x20000 block,
tags 1/0x12/17, kernel/tlv/rootfs) implemented from the SDK `unpack.sh` and
validated on real KN-1010 3.09/4.03/5.02, KN-1811 5.02, KN-3710/KN-2310/KN-2110 5.02. Signature (`ndmfw`)
is not verified; TLV images are read-only. Remaining: test other models/platforms
and long-term signature handling.

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

## Filesystem backlog (reviewed; samples/targets needed)
Rule: do not add a format without a real sample or a concrete device; detection-only
is acceptable when the magic/extension is clear.

Already covered / not a new format:
- DOS-FAT: supported as FAT12/16/32.
- SmartMedia DOS-FAT: FAT data area; raw SmartMedia needs SSFDC OOB/ECC mapping.
- FileX (Azure RTOS): FAT/exFAT-compatible; LevelX is a NAND wear-leveling layer,
  not a filesystem format.
- LogFS: removed from Linux 4.10 (2016), complex journal/inode-file design, no
  samples; defer.
- FLogFS (b.nahill): tiny append-only NAND logging FS for Cortex-M, no dirs,
  needs raw OOB dump; defer.

Open source/spec exists, read-only feasible (high effort unless noted):
- Btrfs: generateable/mountable in WSL, but reader scope is very large (CoW/checksums/trees); deferred.
- minix fs: done (dev, read-only) — v1/v2/v3, 14/30/60-char names, fixtures via mkfs.minix.
- ReiserFS: read-only, obsolete/complex; low priority.
- NTFS: done (dev, read-only) - MFT/FILE_NAME tree, resident/non-resident DATA runlists, mkntfs fixture.
- HFS (classic, Mac OS Standard): done (dev, read-only) - MDB, catalog B-tree, extents overflow, hfsutils fixture.
- CP/M 2.2/3: done (dev, external `NyaFsAncientFs` plugin) - read-only reader (8.3 names, user areas, 8/16-bit pointers, skew, extents) plus fresh `ibm-3740` repack writer; cpmtools fixtures.
- TRS-80 DMK (CP/M): done (dev, external `NyaFsAncientFs` plugin, read-only) - DMK IDAM extraction, 40T/18S/256B Model 4 CP/M geometry, skew 2; real `CPM QC.dmk` validated against cpmtools `trsg`.
- TRS-80 TRSDOS/LDOS (DMK): done (dev, external `NyaFsAncientFs` plugin, read-only) - track 20/17 directory, 32-byte entries, extent list, 3-sector granules; real `CHURCH DATA` sample0/sample1 validated. FXDE chains and date/protection metadata remain.
- Tandy CoCo Disk BASIC (`.dsk`): done (dev, external `NyaFsAncientFs` plugin, read-only) - track 17 directory, granule-map chains, 9-sector granules; real `Robert Rhythm` 35T image validated against `cocofs`.
- ImageDisk (`.imd`): done (dev, external `NyaFsAncientFs` plugin) - uniform tracks copied, mixed-sector tracks packed into the largest sector size and padded; real `DISKUTIL IMPORTANT.imd` loads through CoCo, `CPM.imd` mixed geometry parsed.
- WOZ Apple II 5.25" (`.woz`): done (dev, external `NyaFsAncientFs` plugin) - WOZ1/WOZ2, TMAP/TRKS, 6-and-2 GCR, ProDOS logical order; real ProDOS WOZ validated. DOS 3.3/13-sector remain.
- SCP Apple II flux (`.scp`): done (dev, external `NyaFsAncientFs` plugin) - SCP flux/TDH, zero gaps, PLL, 6-and-2 GCR, ProDOS order; validated with a clean Greaseweazle-generated SCP. Copy-protected/error SCPs remain unsupported.
- MFS (Macintosh File System): done (dev, external `NyaFsAncientFs` plugin, read-only) - MDB, packed 12-bit VABM, flat directory, data forks; synthetic 400 KiB fixture.
- Disk Copy 4.2 wrapper: done (dev, external `NyaFsAncientFs` plugin) - 84-byte header/tag unwrap; validated on real Finder 1.0 System Disk.image.
- StuffIt 1.x/3.x: done (dev, external `NyaFsAncientFs` plugin) - SIT!/rLau parser, method 0 and 13 LZ+Huffman; real System 1.1 archive validated.
- StuffIt remaining: static-table method 13, methods 1/2/3/5/8/14/15, StuffIt 5 (`SIT5`), encrypted entries, resource forks (both forks currently only data fork is exposed).
- Early Mac CD archives: `cd.html` is an article/history page with no direct CD downloads; the referenced 400K/800K disk archives are already covered through MFS/HFS + StuffIt/Disk Copy 4.2.
- UNIX V6 filesystem (PDP-11 RX50): done (dev, external `NyaFsAncientFs` plugin, read-only) - superblock, 32-byte inodes, 16-byte dirs, direct + ILARG files, device nodes; real `ra1boot.rx50` validated.
- Commodore DOS (`.d64`): done (dev, external `NyaFsAncientFs` plugin, read-only) - BAM, directory/file chains, PETSCII, 35-track; real C128/C64 images validated.
- Amiga OFS/FFS (`.adf`): done (dev, external `NyaFsAncientFs` plugin) - root hash, dirs/files, FFS block lists, OFS nextData chains; real ADFs validated; fresh 880K FFS repack writer (flat root, max 72 blocks/file).
- Atari DOS 2.x (`.atr`): done (dev, external `NyaFsAncientFs` plugin) - ATR header, sector 360 directory, contiguous files; real DOS 2.5 images validated; fresh 720x128 repack writer.
- Apple ProDOS 8 (`.po`): done (dev, external `NyaFsAncientFs` plugin) - volume/directory blocks, seedling/sapling/tree, subdirs; real AppleWorks images validated; fresh 140K flat-root repack writer.
- AXFS: compressed XIP read-only; project is dead; low priority.
- MnemoFS (NuttX): NAND log FS, paper/source; no samples.
- STORfs: MCU log FS, open source; no samples.
- Reliance Edge: commercial, source under license, no public on-disk spec.
- EFS: ambiguous (SGI EFS has docs; vendor "Embedded FS" is proprietary).

Vendor/closed, no public on-disk spec (skip unless a sample/NDA appears):
- Cypress FFS (source gated), EFTS (QNX), EdgeFS (Tuxera), TargetFFS-NAND,
  SafeFLASH (HCC), emFile (SEGGER), rl-flashfs (Keil), TrueFFS/NTFL (DiskOnChip).
- MNFS: research FS (mobile multimedia NAND, 2006), paper only.