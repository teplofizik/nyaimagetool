# Loading whole and partial images from different sources

Reference notes: how to load a whole image or a single sub-image from the many
containers/formats we support. Run from a `.nyas` script or the interactive shell.

## Commands

| command | purpose |
|---------|---------|
| `inspect <file>` | print the discovered sub-image tree with **stable paths** |
| `load <file>` | load the whole image (auto-detect through the plugin pipeline) |
| `load.sub <file> <subpath>` | load exactly one sub-image by its stable path |
| `extract <file> <subpath> <out>` | write a sub-image to a file, byte for byte |
| `store <out> <type> <format>` | write the loaded image back (see below) |
| `export <dir>` | dump the loaded filesystem to a directory |

Typical flow: `inspect <file>` → pick a path → `load.sub` (to work with it) or
`extract` (to get the raw bytes). Empty subpath (`""` or `"."`) means the whole file.

## Whole images

`load <file>` works for every supported format: it detects compression, composite
containers (FIT / Android / Legacy / BMU / Allwinner / ...) and leaves
(kernel / filesystem / devicetree) via the plugin pipeline.

```
load F:\TEMP\Images\ramfs\cpio\overlay.gz
load F:\TEMP\Images\kernel\raw\netbsd-RPI
load F:\TEMP\Images\mcu\littlefs.bin
load F:\TEMP\Images\devtree\bcm2836-rpi-2-b.dtb.gz
```

## FIT (U-Boot)

```
inspect F:\TEMP\Images\fit\amlogic-a113d.fit
```
```
amlogic-a113d.fit (root)  [Unknown/fit]
  kernel@a113d   [Kernel/raw comp=IH_COMP_GZIP]
  ramdisk@a113d  [Filesystem/cpio comp=IH_COMP_GZIP]
  fdt@a113d      [Devtree/dtb]
```
```
load.sub F:\TEMP\Images\fit\amlogic-a113d.fit kernel@a113d
extract F:\TEMP\Images\fit\amlogic-a113d.fit ramdisk@a113d F:\TEMP\out\ramdisk.gz
```

## Android boot image

Sub-images: `kernel`, `ramdisk`, `second`, `recovery_dtbo`.

```
inspect F:\TEMP\Images\android\boot.fex
load.sub F:\TEMP\Images\android\boot.fex ramdisk
```

## BMU (single)

Sub-images are the BMU files: `BOOT.bin`, `devicetree.dtb`, `uImage`,
`minerfs.image.gz`, `update.image.gz`, `crl.tar.gz`, `miner.btm.tar.gz`, `datafile`.

```
inspect F:\TEMP\Images\bmu\Antminer-L7-release-202301300939.bmu
load.sub F:\TEMP\Images\bmu\Antminer-L7-release-202301300939.bmu uImage
extract F:\TEMP\Images\bmu\Antminer-L7-release-202301300939.bmu minerfs.image.gz F:\TEMP\out\minerfs.gz
```

## BMU (merge, several boards in one file)

Sub-paths are `<Model>/<Hardware>/<Chip>/<file>`; a `merge` BMU carries a separate
image for every board/variant in one file.

```
inspect ...\Antminer-T21-merge-release-20240716102718.bmu
load.sub  ...\Antminer-T21-merge-release-20240716102718.bmu Antminer T21/AMLCtrl_BHB6870X/update.bmu
```

## Ramfs / filesystems

cpio / tar / squashfs / cramfs / ext2 / romfs, optionally wrapped in a compressor
(`.gz`, `.xz`, `.bz2`, `.zst`, `.lz4`, `.lzma`, `.lzo`) — auto-detected:

```
load F:\TEMP\Images\ramfs\cpio\overlay.gz
export F:\TEMP\out\rootfs
load F:\TEMP\Images\ramfs\squashfs\rootfs.sqfs
```

## MCU filesystems (littlefs / spiffs)

Whole-file filesystems (the file *is* the filesystem; size = `FsSize`):

```
load F:\TEMP\Images\mcu\littlefs.bin
export F:\TEMP\out\littlefs
load F:\TEMP\Images\mcu\test.spiffs
```

### Creating / modifying / converting MCU filesystems

`set ramfs size <n>` sets the filesystem size (`FsSize`); `store <out> ramfs littlefs|spiffs`
writes it back (`<n>` accepts decimal, `0x...` hex, or a `K`/`M` suffix).

**1. Create** a new filesystem:
```
mkfs
set ramfs size 1M
file /hello.txt F:\TEMP\mcu_src\hello.txt rw-r--r-- 0 0
file /readme.txt F:\TEMP\mcu_src\readme.txt rw-r--r-- 0 0
store F:\TEMP\out\create.lfs ramfs littlefs
```

**2. Modify** an existing filesystem (keeps its size/parameters):
```
load F:\TEMP\Images\mcu\littlefs.bin
file /newfile.txt F:\TEMP\mcu_src\new.txt rw-r--r-- 0 0
store F:\TEMP\out\modified.lfs ramfs littlefs
```

**3. Convert** SPIFFS -> LittleFS (set the target size):
```
load F:\TEMP\Images\mcu\test.spiffs
set ramfs size 2M
file /extra.txt F:\TEMP\mcu_src\extra.txt rw-r--r-- 0 0
store F:\TEMP\out\conv.lfs ramfs littlefs
```

## Devicetree

```
load F:\TEMP\Images\devtree\bcm2836-rpi-2-b.dtb.gz
store F:\TEMP\out\rpi.dtb devtree dtb
```

## Writing back

`store <out> <type> <format>`: `type` is `kernel` | `ramfs` | `devtree` | `all`.
Formats: kernel `raw|gz|lzma|lz4|bz2|xz|zstd|legacy`; ramfs
`cpio|tar|gz|xz|zstd|legacy|ext2|squashfs|cramfs|romfs|littlefs|spiffs` and
`tar.gz|tar.xz|tar.bz2|tar.zstd|tar.lz4|tar.lzma`; devtree `dtb`; one-arg
`store <out>` writes a FIT.

```
load F:\TEMP\Images\ramfs\cpio\overlay.gz
store F:\TEMP\out\rootfs.tar.gz ramfs tar.gz
```

## Notes

- `load` follows the plugin pipeline recursively, so a container's children are
  re-detected (e.g. BMU -> kernel / filesystem / devicetree). External plugins must
  be present in `plugins/` next to the executable (the app loads them automatically).
- Exact sub-paths are whatever `inspect` prints (they are stable identifiers).
- Filesystem formats provided by plugins (e.g. littlefs/spiffs) are routed through the
  plugin's writer, so `store` emits them directly.
