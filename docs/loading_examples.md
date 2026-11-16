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

`sign-images` additionally signs every image node (`/images/*/signature1`).

A FIT boot script is an image node referenced by the configuration `script` property; it is loaded/listed as an opaque `script` element and preserved on rebuild.

External payloads can be written with:

```
set container fit.external true
set container fit.external-align 4
store F:\TEMP\fit\external.itb
```

`external-position` uses `data-position` instead of `data-offset` (absolute
address; no payload is appended). External data is read back by `load`/`inspect`
when `data-offset` points after the FIT.

## Android boot image

Sub-images: `kernel`, `ramdisk`, `second`, `recovery_dtbo`.

```
inspect F:\TEMP\Images\android\boot.fex
load.sub F:\TEMP\Images\android\boot.fex ramdisk
```

Android boot v0-v4 can also be written (v4 preserves the appended boot signature):

```
load F:\TEMP\Images\android\boot.fex
store F:\TEMP\android-out.img all android
```

Header fields are preserved or can be overridden with `set container android.*`
(`header-version`, `page-size`, `name`, `cmdline`, `kernel-addr`, `ramdisk-addr`,
`tags-addr`, ...). v3 uses a fixed 4096-byte page and has no second/DTB fields.

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

## Android vendor_boot

`VNDRBOOT` v3/v4 images are a composite of vendor ramdisk fragment(s), DTB and
(v4) bootconfig:

```
inspect vendor_boot.img
load vendor_boot.img
store vendor_boot-new.img all vendorboot
```

`v3` exposes `vendor_ramdisk` + `dtb`; `v4` exposes `vendor_ramdisk_0..N`
(from the vendor ramdisk table), `dtb` and `bootconfig`. Fragment table
metadata (type/name/board_id) and the header are preserved on save; changed
fragments get updated sizes/offsets and the region/table are rebuilt.

## TVIP firmware

## TVIP firmware

`NyaFsTvip` plugin. Sections are `kernel` (uImage), `rootfs` (CramFS),
`splash` and `uboot`:

```
inspect F:\TEMP\Images\vendor-raw\tvip_firmware.bin
load F:\TEMP\Images\vendor-raw\tvip_firmware.bin
extract F:\TEMP\Images\vendor-raw\tvip_firmware.bin rootfs F:\TEMP\rootfs.cramfs
```

The rootfs uses a CramFS variant with 16-byte inodes; it is read and written
transparently (`Fs.CramFsWideInodes`). Editing and storing back:

```
load F:\TEMP\Images\vendor-raw\tvip_firmware.bin
file etc/version.txt F:\TEMP\version.txt rw-r--r-- 0 0
store F:\TEMP\tvip-new.bin all tvip
```

Dirty sections get a recalculated size/MD5; untouched sections are copied
byte-for-byte. The 0x24-byte global header is preserved.

## Legacy uImage

## Legacy uImage

Single-payload uImage types are detected and loaded by the `legacy` plugin:

- `kernel`, `kernel_noload` -> kernel
- `ramdisk`, `filesystem` -> filesystem
- `flat_dt` -> device tree
- `multi` -> size table + payloads (first payload as kernel, the rest auto-detected)
- `standalone`, `firmware`, `script` and vendor mkimage types -> opaque `payload`
  (use `inspect` / `extract` / `use` + `import` to read or replace it)

Script and multi-file uImage have dedicated writers:

```
storescript script.img script.txt
load boot.fex
store multi.img all multi
```

`storescript` wraps text in `IH_TYPE_SCRIPT`; `store all multi` wraps loaded kernel/ramfs/DTB
as `IH_TYPE_MULTI` (size table + payloads). An edited legacy image is saved back with
`store out.img all legacy` (type/name/os/arch/compression are preserved, CRC32 is recalculated).

## Bare Linux Image (arm64 / riscv)

`booti`-style uncompressed kernel `Image` (`ARM\x64` / `RISCV` magic at 0x38)
and its compressed variants are detected as `kernel/image`:

```
load F:\TEMP\Images\openwrt\openwrt-25.12.6-imx-cortexa53-imx8m-kernel.bin
store out.Image kernel image
```

## U-Boot environment

`NyaFsUboot` plugin. A binary environment is also a first-class image: `load`
verifies the CRC32 and `store` recalculates it.

```
load F:\TEMP\Images\vendor-raw\whats-env.bin
env get bootcmd
env set bootdelay 5
store F:\TEMP\env-new.bin all env

env load env.bin
env list
env save env-new.bin

env new 128K
env import env.txt
env save created.bin
```

See `Plugins/NyaFsUboot/README.md` for all actions and layouts.

## Allwinner IMAGEWTY

```
load image.img
storewty out.img
```

`storewty` preserves existing WTY file headers and can also wrap a loaded kernel,
rootfs and DTB into a new IMAGEWTY container.

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
