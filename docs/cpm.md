# CP/M (external plugin, read-only)

`NyaFsAncientFs` adds read-only CP/M 2.2/3 support as an external plugin. Copy
`NyaFsAncientFs.dll` to the `plugins` directory (the normal build/publish
already does this).

## Commands

```text
load cpm_sample.dsk
ls /
export out/
```

CP/M has no filesystem magic and does not store its disk geometry. The plugin
tries a table of common geometries and validates the directory, so `.dsk` and
`.cpm` images normally auto-detect.

## Reader support

- 32-byte directory entries, 8.3 names and user areas;
- 8-bit and 16-bit allocation block pointers;
- sector skew (`skew` and explicit tables);
- multiple logical extents per physical directory entry;
- files and exact CP/M record lengths.

User area 0 is exposed at the root; other user areas appear as `uNN`
directories (`u01`, `u02`, ...).

Built-in geometry candidates: `ibm-3740`, `cpm-2k-40t`, `cpm-2k-210t`,
`z80pack-hd`, `4mb-hd`, `sdcard`, `pcw`, `osborne1`. Custom images can use the
explicit `CpmGeometry` constructor.

## Limitations

- read-only: no `store`/`build` writer;
- CP/M has no directories, symlinks, POSIX permissions or timestamps;
- images with a wrapper/header before the CP/M disk area are not supported;
- the core filesystem type stays `Unknown`; the plugin reports format `cpm`.

## Verification

- fixtures generated with `cpmtools` (`ibm-3740`, 2 KiB blocks with logical
  extents, and 2 KiB blocks with 16-bit block pointers);
- `CpmTests` covers detection, directory listing, exact sizes and data hashes;
- a CLI smoke test confirmed plugin auto-loading from `plugins/`.
