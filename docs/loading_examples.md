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
| `signfit <in> <out> <key> [control]` | re-sign existing FIT signature nodes in place; optionally write control DTB |
| `verifyfit <fit> <key/cert>` | verify FIT config signatures |

Typical flow: `inspect <file>` → pick a path → `load.sub` (to work with it) or
`extract` (to get the raw bytes). Empty subpath (`""` or `"."`) means the whole file.

## Whole images

`load <file>` works for every supported format: it detects compression, composite
containers (FIT / Android / Legacy / BMU / AMLSECU / Allwinner / ...) and leaves
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

### FIT signing

`signfit` expects mkimage-prepared signature nodes (`value`, `hashed-nodes`,
`hashed-strings`); it signs the same FDT regions as U-Boot and updates `value`
in place. Node names with `@` are supported.

```
signfit F:\TEMP\fit\image.itb F:\TEMP\fit\signed.itb F:\TEMP\keys\test.key F:\TEMP\fit\control.dtb
verifyfit F:\TEMP\fit\signed.itb F:\TEMP\keys\test.crt
```

For a mkimage-produced FIT the resulting `value` is byte-identical to `mkimage`.

To generate and sign a FIT during `store`, import a private key into KeyStore and
select it for the FIT container:

```
keys import fit-test F:\TEMP\keys\test.key F:\TEMP\keys\test.crt
use kernel1
import F:\TEMP\fit\image.itb all/fit/kernel1
set container fit.sign fit-test
set container fit.sign-control F:\TEMP\fit\control.dtb
store F:\TEMP\fit\generated.itb
```

`sign-algo`, `sign-key-name` and `sign-control` are optional FIT container
options. `sign-control` writes the U-Boot `/signature/key-*` control DTB with the
public modulus, exponent, r-squared and n0-inverse. If the target already exists,
it is merged: other signature keys and nodes are preserved, and a key with the
same name is replaced.

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

Sub-paths are `<Model>/<Hardware>/<Chip>/<file>`; the `merge` ones carry images for
many boards at once (including an Amlogic `AMLCtrl` item):

```
inspect G:\Projects\Асик конфиг\BMU\DATABASE\Antminer-T21-merge-release-20240716102718.bmu
load.sub  ...\Antminer-T21-merge-release-20240716102718.bmu Antminer T21/AMLCtrl_BHB6870X/update.bmu
```

## AMLSECU (Amlogic secure update) — hidden plugin

An encrypted Android image (marker `AMLSECU!`@0x400). The plugin decrypts the
sections and hands back a plain Android image for re-detection:

```
inspect F:\TEMP\Images\android\am_kernel_enc.bin
```
```
am_kernel_enc.bin (root)  [Unknown/amlogic.encrypted]
  decrypted            [Unknown/android]
    decrypted/kernel   [Kernel/raw comp=IH_COMP_GZIP]
    decrypted/ramdisk  [Filesystem/cpio comp=IH_COMP_GZIP]
    decrypted/second   [Unknown/? comp=IH_COMP_GZIP]
```
```
load.sub F:\TEMP\Images\android\am_kernel_enc.bin decrypted/ramdisk
extract F:\TEMP\Images\android\am_kernel_enc.bin decrypted/kernel F:\TEMP\out\kernel.gz
```

The same inside a BMU (single or merge), where the AMLSECU image is the `datafile`:

```
inspect F:\TEMP\Images\bmu\Antminer-S21Imm-AML-release-202408061155.bmu
#   datafile -> datafile/decrypted -> datafile/decrypted/kernel ...
load.sub F:\TEMP\Images\bmu\Antminer-S21Imm-AML-release-202408061155.bmu datafile/decrypted/ramdisk
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
`cpio|tar|gz|xz|zstd|legacy|ext2|squashfs|cramfs|romfs` and `tar.gz|tar.xz|tar.bz2|tar.zstd|tar.lz4|tar.lzma`;
devtree `dtb`; one-arg `store <out>` writes a FIT.

```
load F:\TEMP\Images\ramfs\cpio\overlay.gz
store F:\TEMP\out\rootfs.tar.gz ramfs tar.gz
```

## Notes / gaps

- `load` follows the plugin pipeline recursively, so a container's children are
  re-detected (e.g. BMU -> AMLSECU -> Android -> kernel). External plugins must be
  present in `plugins/` next to the executable (the app loads them automatically).
- Exact sub-paths are whatever `inspect` prints (they are stable identifiers).
- `store` uses the built-in writers, not the plugin writers, so it does not yet
  emit MCU filesystems (littlefs/spiffs). Converting between them is done through the
  plugin writers (see the MCU conversion tests) — a `store` extension is a TODO.
- AMLSECU examples require the hidden `NyaFsAmlogicEncrypted` plugin (private repo).
