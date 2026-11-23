# Atari DOS 2.x / .atr (external plugin, read/write)

`NyaFsAncientFs` reads and rebuilds Atari DOS 2.0/2.5 `.atr` images:

```text
load dos25.atr
ls /
export out/
```

## Reader support

- ATR header (magic `0x0296`, sector size 128/256);
- flat directory at sector 360, 64 entries of 16 bytes;
- contiguous file data sectors;
- 8.3 names and the DOS 2.x directory flags.

## Writer support

`store` creates a fresh 720-sector, 128-byte ATR with files placed contiguously
from sector 4 and the 64-entry directory at sector 360. Only flat root files
are supported.

## Limitations

- Atari DOS 3 / MyDOS directory formats are not covered;
- file length is sector-granular (DOS 2.x does not store byte EOF);
- no VTOC/free-space validation.

## Verification

- real `DOS 2.5, FORMATTED BLONDELL` and `DOS 2.5 working disk` ATR images
  from the markaudacity Disk-Archive;
- `AtariDosTests` checks names, `BLONDELL` hash, `DOS.SYS` size and pipeline
  loading;
- `AtariDos_RebuildsImage` verifies the writer and reader round-trip.
