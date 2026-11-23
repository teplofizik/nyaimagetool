# NyaFsAncientFs

External plugin for legacy filesystems and wrappers. Supported formats:
CP/M 2.2/3, TRS-80 DMK with CP/M or TRSDOS/LDOS, Tandy CoCo Disk BASIC,
ImageDisk (`.imd`), WOZ/SCP Apple II 5.25", Commodore DOS (`.d64`), Atari DOS 2.x
(`.atr`), Apple ProDOS (`.po`), Amiga OFS/FFS (`.adf`), Macintosh File System
(MFS, 1984), UNIX V6 (PDP-11), Disk Copy 4.2 images and StuffIt 1.x/3.x
archives.

## TRS-80 DMK (CP/M)

```text
load "CPM QC.dmk"
ls /
```

Reads the standard 40-track, 18-sector, 256-byte TRS-80 Model 4 CP/M layout:
DMK track headers/IDAMs are unpacked into a CP/M sector image (2 reserved
tracks, 2 KiB blocks, 128 directory entries, skew 2).

## TRS-80 DMK (TRSDOS/LDOS)

```text
load "CHURCH DATA.dmk"
ls /
```

Scans the Model III/4 directory at track 20 (falling back to track 17) using
32-byte entries, and follows 3-sector granules from the extent list. System
files such as `BOOT.SYS` and `DIR.SYS` are exposed like ordinary files.

## Tandy CoCo Disk BASIC

```text
load "Robert Rhythm_SS.35T.18S.dsk"
ls /
```

Reads raw 35/40-track CoCo `.dsk` images: directory track 17, 32-byte entries,
granule-map chains and 9-sector granules.

## ImageDisk (`.imd`)

```text
load "DISKUTIL IMPORTANT.imd"
ls /
```

Unpacks IMD tracks into a raw `disk.dsk` child; mixed-sector tracks are
packed into the largest sector size and padded. Normal CoCo, CP/M, ProDOS and
FAT detection then runs on that child.

## WOZ Apple II 5.25"

```text
load "M&M Software - RT1225 Convert It.woz"
ls /
```

Decodes 6-and-2 GCR tracks into ProDOS logical-sector order and exposes a
`disk.po` child. Only 5.25" 16-sector ProDOS-order output is supported.

## SCP Apple II flux

```text
load clean_prodos.scp
ls /
```

Applies a 4 µs PLL to the flux timings, decodes 6-and-2 GCR and exposes the
same `disk.po` child. Standard 300 rpm single-sided 35-track images only.


## Apple ProDOS

```text
load samples.po
ls /
```

Reads ProDOS 8 `.po` images: volume directory at block 2, 39-byte directory
entries, seedling/sapling/tree file storage and subdirectories. ProDOS index
blocks use the split low/high block-pointer tables.

## Atari DOS 2.x

```text
load dos25.atr
ls /
```

Reads Atari DOS 2.0/2.5 ATR images: 16-byte ATR header, sector size from the
header, flat directory at sector 360 and contiguous file sectors.

`store` rebuilds a fresh 720x128-byte ATR with files contiguous from sector 4;
only flat root files are supported.

## Amiga OFS/FFS

```text
load extras.adf
ls /
```

Reads 880K ADF images with OFS or FFS data blocks: root header/hash table,
directory data blocks, file header data-block lists, file extension blocks and
OFS `nextData` chains.

## Commodore DOS

```text
load cool.d64
ls /
```

Reads standard 35-track 1541 `.d64` images: BAM, directory chain and file
chains. CBM DOS is flat; SEQ/PRG/USR/REL files are exposed at the root.
PETSCII names are converted to readable ASCII where possible.

## UNIX V6

```text
load ra1boot.rx50
ls /
```

Reads the V6 filesystem used on RX50/RD52 media: 512-byte blocks, superblock
at block 1, 32-byte inodes from block 2, 16-byte directory entries and `ILARG`
single-indirect block mapping. Device nodes are exposed as char/block entries.

## StuffIt

```text
load "1.1 System Disk.sit"
ls /
```

StuffIt data forks are extracted as child files and then passed through Disk
Copy/MFS/HFS detection. The built-in decoder handles the LZ+Huffman method 13
used by early Macintosh System disks; stored (method 0) entries are also
handled. Resource forks, encrypted entries and newer StuffIt 5 methods are not
exposed.

## Disk Copy 4.2

```text
load "System Disk.image"
ls /
```

Disk Copy 4.2 images have an 84-byte header, the raw volume and an optional tag
area. The wrapper is exposed as a child volume, so MFS/HFS detection runs on the
raw data automatically. Real System 1.1 `System Disk.image` has been validated.

## MFS

```text
load mfs_sample.img
ls /
export out/
```

MFS is the flat 400K floppy filesystem from the original Macintosh. The reader
checks the `D2D7` signature at block 2 and supports:

- 512-byte container blocks;
- variable-length directory records in the directory blocks;
- allocation block chains from the packed 12-bit VABM;
- data forks and exact logical lengths.

Finder folders are metadata only: MFS is flat, so all files are exposed at the
root. Resource forks are not exposed and MFS has no POSIX permissions.


## CP/M

```text
load cpm_sample.dsk
ls /
export out/
```

CP/M has no filesystem magic and does not store its disk geometry. The plugin
tries a small table of common geometries and validates the directory, so
`.dsk`/`.cpm` images should normally auto-detect:

- `ibm-3740` вЂ” 8" SSSD, 128-byte sectors, 26 sectors/track, 1 KiB blocks;
- `cpm-2k-40t` / `cpm-2k-210t` вЂ” 2 KiB blocks, 8-bit and 16-bit block pointers;
- `z80pack-hd`, `4mb-hd`, `sdcard`, `pcw`, `osborne1`.

Supported reader features:

- 32-byte directory entries, 8.3 names and user areas;
- 8-bit and 16-bit allocation block pointers;
- CP/M sector skew tables (`skew` and explicit tables for common formats);
- multiple logical extents per physical directory entry;
- data-only files; CP/M has no directories, symlinks or POSIX permissions.

User area 0 is exposed at the root. Other user areas appear as `uNN`
directories (`u01`, `u02`, ...).

The reader also supports an explicit geometry through
`new CpmReader(data, new CpmGeometry(...))`; this is useful for custom CP/M
formats that are not in the candidate table.

The plugin is read-only for arbitrary images. `store` can rebuild a fresh
standard `ibm-3740` image (77T/26S/128B, 1 KiB blocks) from a flat root tree.
`FsType` stays `Unknown` at the core level; the loaded filesystem is identified
by the plugin format `cpm`.
