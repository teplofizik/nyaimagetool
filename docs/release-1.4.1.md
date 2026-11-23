# NyaFs 1.4.1

## XFS read-only

`NyaFs` can now inspect XFS v4 and v5 filesystems:

```text
load server-disk.img
export out/
```

Supported:

- superblock v4/v5, 512..65536-byte blocks;
- inode v1/v2/v3, local/extents/btree data forks, `NREXT64`;
- AG-encoded fsblock mapping, shortform/block/leaf directories;
- files, dirs, symlinks, FIFOs/devices, sparse and unwritten extents.

Not supported yet:

- writing (`store`/`build`) — the plugin is read-only;
- xattrs/attr forks, v5 CRC verification, project ids/timestamps in the
  filesystem-entry API.

## Verification

- fixtures generated with `xfsprogs 6.18` (v4/v5, 300 MiB);
- `XfsTests` covers detection, directories, extents/btree, sparse files and
  symlinks.

## Upgrade notes

- Package version is now `1.4.1`; update `PackageReference NyaFs` in all
  consuming projects.
- JFFS/YAFFS v1 from 1.4.0 is unchanged.
