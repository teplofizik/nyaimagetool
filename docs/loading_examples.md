# Loading whole and partial images from different sources

Reference notes: how to load a whole image or a single sub-image from the many
containers/formats we support. Run from a `.nyas` script or the interactive shell.

## Commands

| command | purpose |
|---------|---------|
| `inspect <file>` | print the discovered sub-image tree with **stable paths** |
| `load <file>` | open a new workspace and load the whole image |
| `load.sub <file> <subpath>` | load exactly one sub-image without resetting the workspace |
| `use <path>` | select an element of the loaded build plan for editing |
| `import <file> <subpath> [target]` | replace an element from a part of another image |
| `exportfit <file> <dir>` | export FIT as `.its` plus sidecar binary blobs |
| `importfit <file.its> <out.fit>` | build FIT from an exported `.its` and its blobs |
| `extract <file> <subpath> <out>` | write a sub-image to a file, byte for byte |
| `store <out> <type> <format>` | write the loaded image back (see below) |
| `export <dir>` | dump the loaded filesystem to a directory |

Typical flow: `inspect <file>` → pick a path → `load.sub` (to work with it) or
`extract` (to get the raw bytes). Empty subpath (`""` or `"."`) means the whole file.

## Workspace and active element

`load <file>` resets the current workspace (plan, active element and options).
`load.sub` and `import` do not reset it: they add or replace a single part.

Once a container is loaded, `use` selects an element for FS/kernel/DTB commands:

```
load boot.emmc
use all/cimg/fit/kernel
set kernel entry 0x100090000
use all/cimg/fit/devtree
use all/cimg/fit/ramdisk@1
file etc/hostname hostname.txt
store out.emmc
```

`import` replaces the active element from another image part:

```
load boot.emmc
use all/cimg/fit/kernel
import F:\other.fit all/fit/kernel
```

`cd` remains filesystem navigation; `use` is container-plan navigation.

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

`exportfit` writes a U-Boot `.its` file and a `blobs/` directory. Image
`data` properties become `/incbin/("./blobs/...")` references; the blobs contain
the exact payload bytes stored in the FIT, not hex text. The command currently
and `importfit` builds a FIT from that source again. The importer supports
strings, cells, byte arrays and `/incbin/`; advanced DTS syntax/labels are not
supported yet.

FIT configuration and image descriptions are preserved on rebuild, as are
`load`/`entry` values for kernel, FDT and ramdisk images. Hash verification
accepts `sha1`, `sha256`, `sha512` and `crc32`.

FIT configurations are preserved. A multi-config image (for example
`fit/ramboot.itb` with four board configurations) exposes all referenced
kernel/ramdisk/fdt images; select one with `use fdt@...` and `store` rebuilds
all configurations together.

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
`tar.gz|tar.xz|tar.bz2|tar.zstd|tar.lz4|tar.lzma`; devtree `dtb`.
One-argument `store <out>` uses the build plan captured by `load`: if the image
has not been modified, the original bytes are written back exactly. After edits
the root container is rebuilt using its original format (CIMG/FIT); other root
formats require an explicit output format.

## Plan-based repack

`load` also records the native image hierarchy: container formats, compression,
header options and original payload bytes. This is the source for:

```
load F:\TEMP\Images\vendor-raw\boot.emmc
store F:\TEMP\out.emmc
```

For an untouched image this is a byte-for-byte copy, including the original CIMG
header and FIT layout. After edits the plan writer rebuilds the original root
format: CIMG/FIT containers, or a plain filesystem/kernel/devicetree using its
writer and compression (`cpio.gz`, `tar.gz`, `ext2/3/4`, UBIFS, F2FS and other
writable formats). Use explicit `store <out> all cimg`, `store <out> ramfs ...`
or `store <out> kernel ...` when you want a different output format.

```
load F:\TEMP\Images\ramfs\cpio\overlay.gz
store F:\TEMP\out\rootfs.tar.gz ramfs tar.gz
```

## Notes

- `load` follows the plugin pipeline recursively, so a container's children are
  re-detected (e.g. BMU -> kernel / filesystem / devicetree). External plugins must
  be present in `plugins/` next to the executable (the app loads them automatically).
- Exact sub-paths are whatever `inspect` prints (they are stable identifiers).
- Logical path aliases are also accepted: `all` and container-format segments may be
  included, and leaf kinds may be used instead of generated names, for example
  `all/cimg/fit/kernel`, `all/cimg/fit/devtree`, `cimg/fit/dtb`, `fit/fs`.
- Filesystem formats provided by plugins (e.g. littlefs/spiffs) are routed through the
  plugin's writer, so `store` emits them directly.
