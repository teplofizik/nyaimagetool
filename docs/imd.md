# ImageDisk / .imd (external plugin)

`NyaFsAncientFs` reads ImageDisk `.imd` files and exposes the sectors as a raw
`disk.dsk` child:

```text
load "DISKUTIL IMPORTANT.imd"
ls /
```

## Reader support

- IMD text header terminated by `0x1A`;
- track records with mode, cylinder, head, sector count and sector size;
- cylinder/head maps and per-sector size maps;
- normal, deleted, error, no-data and RLE-compressed sector records;
- uniform-geometry images are packed into a flat sector image.

## Limitations

- read-only;
- mixed-sector tracks are packed into the largest sector size and padded;
- the child is a raw `.dsk`; the actual filesystem depends on the normal
  CoCo/CP/M/ProDOS/FAT detectors;
- unusual boot tracks may still need a filesystem-specific geometry.

## Verification

- real `DISKUTIL IMPORTANT.imd` from Disk-Archive: 40T/1S/18S/256B;
- raw output loads through the CoCo Disk BASIC reader;
- `ImdTests` covers geometry, CoCo discovery and pipeline loading.
