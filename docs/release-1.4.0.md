# NyaFs 1.4.0

## JFFS v1 and YAFFS v1

Old devices often use the first versions of the flash filesystems. `NyaFs` now
reads and writes both:

- **JFFS v1** — `jffs` (`jffs1` alias): 60-byte raw inodes with the `"1984"`
  magic, little/big-endian, checksums, versioned data reconstruction.
- **YAFFS v1** — `yaffs` (`yaffs1` alias): classic 512+16 chunks, tags in the
  spare area, serial-based duplicate resolution, SmartMedia ECC on write.

```text
load old.jffs
store new.jffs ramfs jffs
load old.yaffs
store new.yaffs ramfs yaffs
```

Raw flash detection also recognises JFFS v1: the flash-layout heuristic adds a
`jffs` region and the NAND probe knows the `"1984"` magic.

New options:

```text
set ramfs jffs.pagesize 4096
set ramfs jffs.eraseblock 64K
```

## Other fixes in this release

- Multi-node build plans (flash/disk with several filesystems, kernels or DTBs)
  no longer reuse a single blob element; each node is reloaded independently.
- FIT auto-detection ignores malformed/unsupported FDTs instead of throwing.
- LZO accepts the vendor-modified lzop magic used by some Ingenic/LSC camera
  rootfs images.

## Limitations

- JFFS v1: no compression, garbage collection or bad-block handling; rdev is
  not stored by the format.
- YAFFS v1: no garbage collection/bad-block relocation/checkpoints; hardlinks
  and xattrs are not emitted; writer output is little-endian.
- Vendor ECC (e.g. BCH) is not regenerated for raw NAND dumps.

## Upgrade notes

- Package version is now `1.4.0`; update `PackageReference NyaFs` in all
  consuming projects.
- Tests (dev): `JffsTests`, `YaffsTests`, JFFS1 flash-layout detection and
  repack round-trips.
