# Disk Copy 4.2 wrapper (external plugin)

`NyaFsAncientFs` unwraps Apple Disk Copy 4.2 images so the raw volume can be
detected as MFS or HFS:

```text
load "System Disk.image"
ls /
export out/
```

Disk Copy 4.2 stores an 84-byte header, the raw volume and an optional tag area.
The plugin validates the declared data/tag sizes against the file length and
exposes the raw volume as a child. No core `NyaFs` change is required.

## Verification

- real `System Disk.image` from the Finder 1.0 archive (earlymacintosh.org)
  loads as MFS and lists `System`, `Note Pad File`, `Clipboard File` etc.;
- `DiskCopy42Tests` wraps the synthetic MFS fixture and covers direct unwrapping
  plus pipeline loading.

## Limitations

- read-only wrapper; no writer;
- StuffIt/BinHex/MacBinary/NDIF wrappers are separate formats and are not
  handled by this plugin.
