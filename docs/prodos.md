# Apple ProDOS 8 / .po (external plugin, read-only)

`NyaFsAncientFs` reads ProDOS 8 `.po` images:

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

## Limitations

- read-only;
- ProDOS 2.0/2.5 `.dsk` images in DOS sector order are not converted yet;
- Apple GS/OS extended files and resource forks are not exposed;
- no bitmap/free-space validation.

## Verification

- real AppleWorks `APPLEWORKS SAMPLES` and `Appleworks_FA` `.po` images from
  the markaudacity Disk-Archive;
- `ProDosTests` checks directory listing, `MOMS.BULLETIN` and `LEDGER` hashes,
  plus pipeline loading.
