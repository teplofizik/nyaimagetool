# TODO

## Filesystems
CramFS Big Endian (select+detection)
VFAT
EXT3 BASE
EXT4 BASE

Detection of filesystem limitations before writing! Uid/Gid, mode.
Scan filesystem: detect elf arch, etc...

## Not supported (later)

### Filesystems
VFAT/exFAT, EXT3, EXT4 (only EXT2 reader/writer), Btrfs, XFS, F2FS, EROFS,
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