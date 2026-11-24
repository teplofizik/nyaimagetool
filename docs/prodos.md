# Apple ProDOS 8 / .po (external plugin, read/write)

`NyaFsAncientFs` reads and rebuilds ProDOS 8 `.po` images:

```text
load samples.po
ls /
export out/
```

## Reader support

- volume directory header at block 2 and linked directory blocks;
- 39-byte directory entries with names, file type, key pointer and EOF;
- seedling, sapling and tree file storage;
- split low/high block-pointer tables in ProDOS index blocks;
- subdirectories (`0xD`) recursively.

## Writer support

`store` rebuilds a fresh 140K ProDOS volume: 280 blocks of 512 bytes, volume
directory at block 2, bitmap at block 6, seedling/sapling/tree storage and a
flat root directory (up to 51 entries).

## Limitations

- writer supports only flat root files;
- ProDOS 2.0/2.5 `.dsk` images in DOS sector order are not converted yet;
- Apple GS/OS extended files and resource forks are not exposed;
- reader does not validate the allocation bitmap.

## Verification

- real AppleWorks `APPLEWORKS SAMPLES` and `Appleworks_FA` `.po` images from
  the markaudacity Disk-Archive;
- `ProDosTests` checks directory listing, `MOMS.BULLETIN` and `LEDGER` hashes,
  plus pipeline loading;
- `ProDos_RebuildsImage` verifies the fresh-volume writer and reader round-trip.
