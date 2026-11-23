# TRS-80 TRSDOS/LDOS (external plugin, read-only)

`NyaFsAncientFs` reads TRSDOS/LDOS filesystems inside a 40-track DMK image:

```text
load "CHURCH DATA.dmk"
ls /
export out/
```

The DMK container is unpacked into a track/sector map. The directory is
normally on track 20 (Model III/4); the reader falls back to track 17
(Model I). Directory entries are 32 bytes, and file data is allocated in
3-sector granules on 18-sector double-density disks.

## Reader support

- 32-byte TRSDOS/LDOS directory entries, 8.3 names;
- 5 extent pairs per entry, end marker `FF`;
- 3-sector granules, 6 granules per track, 0-based sector IDs;
- EOF in either RBA form (`EOF low = 0`) or the `raw EOF - 255` form;
- read-only flat namespace; `BOOT.SYS` and `DIR.SYS` are ordinary files.

## Limitations

- read-only;
- only 18-sector/256-byte DMK images with a Model 20/17 directory track;
- FXDE (extended file descriptor) chains are not followed;
- LDOS directories/subdirectories and date/protection metadata are not
  exposed as separate nodes;
- auto-detection requires a recognizable `BOOT.SYS` or `DIR.SYS`.

## Verification

- real `sample0.dmk` / `sample1.dmk` (`CHURCH DATA`, TRS-80 Model 4);
- names and sizes checked against `trsmount`'s generic TRSDOS parser;
- `TrsDosTests` checks the 17-entry directory, file sizes, `CHURCH00.PM1`
  SHA-256 and pipeline loading.
