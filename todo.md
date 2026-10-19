# TODO

## Filesystems
CramFS Big Endian (select+detection)
VFAT
EXT3 BASE
EXT4 BASE

F2FS writer (reader+writer ready; NyaFs 1.0.4x):
* compression: ZSTD/LZ4/LZO/LZORLE done (`set ramfs f2fs.compression zstd|lz4|lzo|lzorle`).
  LZORLE currently emits a plain LZO1X stream (kernel decompresses it with
  `lzo1x_decompress_safe`), i.e. valid but without the RLE ratio gain; add a real
  `lzorle1x_1` compressor later if desired.
* compression supports inode + 2 direct nodes + 2 indirect + double-indirect;
  cluster size configurable: `set ramfs f2fs.compression.cluster 16K|32K|...|1M`
  (log 2..8, fixed previously at log=2/16 KiB).
* unified raw-stream API in `ImageFormat.Compressors` (no container headers) for
  LZ4/LZO/LZORLE — partially done (`Lz4Raw`, NyaLZO raw).

Detection of filesystem limitations before writing! Uid/Gid, mode.
Scan filesystem: detect elf arch, etc...

## Packages (MVP, NyaFs 1.0.47)
One package = one file: gzip(tar) with `PACKAGE` control (key: value, unknown keys
preserved — extensible) and a `data/` tree. Commands: `pkg info <file>`,
`pkg add <file>` (arch/os filter), `pkg create <file> <fs-path> <name> <version>`.
Examples in `common/`: `pkg_scp.nyam` -> `scp.nyapkg`, `pkg_root.nyam` ->
`root.nyapkg` (settings shipped as a package). Documented in `readme.md`
(Packages, xattr, F2FS compression).
TODO:
* xattr: DONE — preserved via PAX `SCHILY.xattr.*` (TarFsBuilder emits PAX,
  TarFsReader reads it, installed by `pkg add`).
* richer metadata: depends/conflicts/provides, version constraints, matching policy.
* create from a local directory (CLI), compression choice (gzip now, zstd later).
* long paths are handled (GNU long name); special files (char/block/fifo) supported.

## Not supported (later)

### Filesystems
VFAT/exFAT, EXT3, EXT4 (only EXT2 reader/writer), Btrfs, XFS, EROFS,
JFFS2, YAFFS, UDF, ISO9660.
UBIFS: reader only (partial), no writer.

### Compression
- LZO write (lzop container) — read-only.
- Brotli, lzip (.lz), compress (.Z), 7z/LZMA2-container, zpaq, rzip — not implemented.
- XZ write via NyaXZ is LZMA2-only: no BCJ/delta filters, full-reset chunks
  (lower ratio than native xz). Read side (SharpCompress) handles BCJ.

### Composite / vendor images
Android boot v3-v4 reader, TVIP firmware, OpenWRT, legacy multiimage, and most
vendor image types present in `ImageType` (imx, stm32, sunxi, rk*, zynq*, ...).

### TAR specifics
Hard links (stored as regular files), sparse files, pax extended headers, GNU sparse.

## Bugfix
Root node mode!..

## Composite Images
Android boot image: v3-4 reader v0-4 writer
TVIP firmware
OpenWRT
Legacy multiimage

## Encryption
FIT encryption (AES)

## Plugins
Plugin system
* Change all fs readers/writers to plugin-base system, provided by NyaPlugin class: Provide reader, provide writer
* Change all compressors as plugins
* Change all image formats as plugins
* Change all commands to plugin
Editing filesystem: change users, groups, rights
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