# TRS-80 DMK / CP/M (external plugin, read-only)

`NyaFsAncientFs` reads TRS-80 DMK images whose payload is a CP/M 2.2
filesystem. TRSDOS/LDOS payloads are handled by the [trsdos.md](trsdos.md)
reader.

```text
load "CPM QC.dmk"
ls /
export out/
```

DMK is a low-level track/sector container, not a filesystem. This plugin
extracts the track image and then parses the CP/M directory through the
existing `CpmReader`.

## Reader support

- standard TRS-80 Model 4 CP/M layout: 40 tracks, 18 sectors of 256 bytes;
- two reserved boot tracks, 2 KiB CP/M blocks, 128 directory entries;
- CP/M sector skew 2 (physical order 1, 3, 5, ..., 17, 2, 4, ..., 18);
- DMK IDAM/data-mark parsing with 128/256/512/1024-byte sectors;
- CP/M 8.3 names, user areas, extents and exact record lengths.

## Limitations

- read-only;
- CP/M geometry is fixed to the 40-track/18-sector/256-byte Model 4 format;
- DMK files with missing sectors in any track are rejected;
- TRSDOS/LDOS payloads use the separate `trsdos` format.

## Verification

- real `CPM QC.dmk` (TRS-80 Model 4, CP/M 2.2) from the Disk-Archive;
- the extracted sector image matches `cpmtools` with the `trsg` definition
  (`seclen 256`, `sectrk 18`, `blocksize 2048`, `maxdir 128`, `skew 2`,
  `boottrk 2`);
- `DmkCpmTests` checks names, sizes and SHA-256 hashes for `GETMOR.OVL`,
  `MENU.OVL` and `QC.COM`, plus pipeline loading.
