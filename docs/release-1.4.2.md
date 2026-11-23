# NyaFs 1.4.2

## Minix and NTFS read-only

Two more filesystems can now be inspected:

```text
load minix.img
export minix-out/

load ntfs-volume.img
export ntfs-out/
```

**Minix**
- v1/v2/v3, 1/2/4 KiB blocks;
- 14/30/60-character names, shortform-style directories, indirect blocks;
- files, dirs, symlinks, FIFOs and devices.

**NTFS**
- MFT-based tree (`FILE_NAME`), resident/non-resident `$DATA`, runlists,
  sparse and initialized-size handling;
- metadata files are hidden.

Both readers are read-only. Writing is not supported yet.

## Verification

- Minix: `mkfs.minix` fixtures for v1/v2/v3 and 14/30-character names;
- NTFS: `mkntfs` + `ntfs-3g` fixture;
- `MinixTests` and `NtfsTests` cover detection, trees and file data.

## Upgrade notes

- Package version is now `1.4.2`; update `PackageReference NyaFs` in all
  consuming projects.
- XFS (1.4.1), JFFS/YAFFS v1 (1.4.0) are unchanged.
