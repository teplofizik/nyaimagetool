# NTFS (read-only)

`NyaFs` reads NTFS volumes. Writing is not supported.

## Commands

```text
load volume.img
export out/
```

`NTFS    ` in the boot sector selects the reader automatically.

## Reader support

- boot sector (512..4096-byte sectors, cluster size, MFT location);
- MFT records with update-sequence fixup and in-use filtering;
- directory tree from `FILE_NAME` attributes (no `$I30` index parsing);
- resident and non-resident `$DATA` attributes, data runlists, sparse runs and
  initialized-size zeroing;
- files, directories and file sizes.

## Limitations

- read-only: no `store`/`build` writer;
- metadata files (`$MFT`, `$Extend`, ...) are hidden;
- compressed/encrypted attributes, alternate data streams, reparse points and
  NTFS symlinks are not decoded;
- timestamps are not exposed by the reader interface.

## Verification

- fixture created with `mkntfs` and populated through `ntfs-3g` in WSL;
- `NtfsTests` covers detection, MFT tree, subdirectories, resident/non-resident
  files and a data hash.
