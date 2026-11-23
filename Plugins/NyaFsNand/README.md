# NyaFsNand

Raw NAND flash dump reader (`nand` container, aliases `mtd` / `nanddump`).
Also handles logical (no OOB/ECC) MTD images — full-flash dumps with the spare
area removed.

A raw NAND dump stores every page as *page data + spare (OOB) area*, and has no
container magic. The plugin recovers the geometry from the dump itself and exposes
the used areas with the OOB stripped, so the regular pipeline can detect and read
the filesystems inside (UBI/UBIFS, Android boot, ext, squashfs, FIT, ...).

## Geometry autodetection

Candidates for `(page, oob)` are scored by data that validates itself:

- UBI erase-counter headers with a valid CRC **and** a `UBI!` volume header at the
  declared `vid_hdr_offset`, which also proves page-size alignment;
- UBIFS index/data nodes with a valid CRC;
- well-known magics (`ANDROID!`, SquashFS, uImage, FIT, ...);
- how "spare-like" the trailing bytes of every used page look (a real OOB area is
  dominated by `0x00`/`0xFF`, a wrongly guessed one contains file data).

The eraseblock size is inferred from the distance between UBI erase-counter
headers (or defaults to 128 KiB, validated against the file length).

Data-only dumps (no OOB) are ambiguous and are only accepted when loading is
forced explicitly; they are never auto-detected as raw NAND.

## Logical MTD images (no OOB/ECC)

A full-flash image with the spare area stripped is still a sequence of
eraseblocks. The plugin picks the eraseblock size at which the most
self-validating structures sit exactly on eraseblock boundaries (UBI
erase-counter headers are the strongest signal, since there is one per PEB) and
accepts the image only when it is clearly a flash layout: at least two UBI
erase-counter headers, plus either a non-zero start of the UBI area (bootloader
before it), several UBI devices, or several container types.

A plain, single UBI file is intentionally **not** wrapped: it is read directly by
the UBIFS plugin. Use explicit loading (`load image all nand`) to force it.

## Regions

Used eraseblocks are grouped into block-aligned regions:

- UBI devices are exposed one region each; a second layout volume or a changed
  UBI `image_seq` starts a new device (e.g. `config` followed by `nvdata`);
- other used areas are grouped with a one-block gap tolerance and split at
  `ANDROID!`, SquashFS, uImage, FIT, ROMFS, CramFS and ext superblock magics.

Region names are `<kind>@<logical offset>`, e.g. `ubi@04700000` or
`android@02200000`.

## Usage

```
nandinfo NAND_dump.bin
nandinfo NAND_dump.bin --blocks
nandinfo NAND_dump.bin nvdada_log
nandinfo NAND_dump.bin --blocks nvdada_log
inspect NAND_dump.bin
extract NAND_dump.bin ubi@04700000 nvdata.img
load nvdata.img
ls /
```

`nandinfo` prints the geometry, bad-block marker status, regions, UBI devices
(PEBs, erase-counter header health, erase counters, `image_seq`) and the volumes
with their reserved/mapped sizes. With an optional volume name or id it also
prints the LEB -> PEB map of that volume (each logical eraseblock with the
absolute eraseblock number and image offset it lives at). `--blocks` adds a
per-eraseblock table: `erased`, `data` (with the detected container) or `ubi`
(free / layout / volume+lnum, plus its erase counter).

Logical (no OOB) images support all of the above; only the spare-area based
bad-block marker line becomes `n/a`, and block offsets are identical to logical
offsets.

Explicit loading (also for OOB-stripped dumps):

```
load dump.bin all nand
```

## Writing (linear images only)

The writer preserves the source flash layout and emits a **logical image without
OOB/ECC**, replacing only the changed regions; everything else is copied from the
source and untouched regions stay byte-identical:

```
load dump.bin
file /etc/new.conf new.conf 644 0 0
store updated.bin all nand
```

- a raw dump turns into a linear image (this is also the easiest way to strip
  OOB/ECC from a dump);
- a linear image round-trips byte for byte when nothing changed;
- replaced regions keep their source size, the unused tail is filled with `0xFF`
  (erased flash); a replacement larger than its region is rejected.

Writing raw (OOB/ECC) dumps back is intentionally not supported: the vendor ECC
(e.g. BCH) would have to be regenerated. Feed the linear image to your flasher
(`nandwrite`, vendor tool), which applies ECC itself.

## Notes

- Bad-block markers follow the common "spare byte 0/1 != 0xFF" convention. When
  most written pages violate it (some controllers keep ECC/zeros there, e.g. the
  Amlogic Meson NAND driver), `nandinfo` reports the count as unknown instead of
  guessing.
- YAFFS1/YAFFS2 need the spare area for their tags; it is not reconstructed here.
