# WOZ Apple II / 5.25" (external plugin, read-only)

`NyaFsAncientFs` reads WOZ1/WOZ2 Apple II 5.25" images:

```text
load "M&M Software - RT1225 Convert It.woz"
ls /
export out/
```

## Reader support

- WOZ1 and WOZ2 containers, TMAP/TRKS/INFO chunks;
- 35-track, 16-sector 5.25" images;
- 6-and-2 GCR address/data field decoding with checksums;
- ProDOS physical-to-logical sector mapping;
- output is a `disk.po` child, so the normal ProDOS detector loads it.

## Limitations

- read-only;
- only 5.25" single-sided 16-sector ProDOS-order output;
- DOS 3.3 ordering, 13-sector DOS 3.2, 3.5" and copy-protected tracks are not
  handled;
- SCP flux images are not supported yet.

## Verification

- real ProDOS WOZ `M&M Software - RT1225 Convert It.woz` from Disk-Archive;
- `WozTests` checks 35T/16S/256B decoding and ProDOS pipeline loading.
