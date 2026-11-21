# NyaFsAncientFs

External read-only filesystem plugin for legacy formats. Supported formats:
CP/M 2.2/3 and Macintosh File System (MFS, 1984).

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
