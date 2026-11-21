# NyaFsAncientFs

External plugin for legacy filesystems and wrappers. Supported formats:
CP/M 2.2/3, Commodore DOS (`.d64`), Macintosh File System (MFS, 1984), UNIX V6
(PDP-11), Disk Copy 4.2 images and StuffIt 1.x/3.x archives.

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

- `ibm-3740` — 8" SSSD, 128-byte sectors, 26 sectors/track, 1 KiB blocks;
- `cpm-2k-40t` / `cpm-2k-210t` — 2 KiB blocks, 8-bit and 16-bit block pointers;
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

The plugin is read-only. `FsType` stays `Unknown` at the core level; the loaded
filesystem is identified by the plugin format `cpm`.
