# Commodore DOS / .d64 (external plugin, read-only)

`NyaFsAncientFs` reads standard 35-track Commodore 1541 `.d64` images:

```text
load cool.d64
ls /
export out/
```

The BAM at track 18/sector 0 and the directory chain at 18/1 are used to find
files; each file is followed through its track/sector chain.

## Reader support

- standard 35-track D64 (174848 bytes; extra error-byte variants tolerated);
- BAM/directory chain and file chains;
- SEQ, PRG, USR and REL file entries;
- PETSCII names converted to readable ASCII;
- flat namespace (CBM DOS has no directories).

## Limitations

- read-only;
- 40-track D64/D71/G64/SCP are not handled yet;
- REL side-sector files expose only the main data chain;
- deleted entries and BAM free-block accounting are not exposed.

## Verification

- real `C128 Public Domain_S1.d64` and C64 `Cool Shorts` images from the
  markaudacity Disk-Archive;
- `CbmDosTests` checks directory listing, the `WORM` file hash and pipeline
  loading.
