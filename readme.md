# NyaImageTool

There is a tool for editing and converting uboot images in different formats: cpio, gz, legacy, FIT etc.
There is possible to add or update files in ramfs image.

## Supported image formats
1. Kernel: compressed raw, fit, raw, legacy
2. Ramfs: cpio, ext2, compressed cpio or ext2, legacy, fit
3. Device tree: dtb, fit

## Supported filesystems
Supported at now:
1. CPIO ASCII (RW)
2. EXT2 (R)
3. SquashFs (R)

## Supported compression types
Supported at now:
1. GZip (RW)
2. LZMA (RW)
3. LZ4 (RW)
4. BZIP2 (RW)
5. XZ (R)
6. LZO (R)
7. ZStd (R)

LZMA compression is provided by LZMA SDK package.
LZ4 compression is provided by FT.LZ4 package.
BZIP2 compression is provided by SharpZipLib package.
XZ compression is provided by SharpCompress package.
LZO compression is provided by NyaLZO library (Decompression code is ported from lzo1x_decompress_safe.c)
ZStd compression is provided by ZstdSharp package.

## How to
There are need to add scp support to image and add version information (device id or other info).
Run image processing:
```
./NyaImageTool.exe <scriptfilename>
```

If program is running without args, it will start in interactive shell mode:
![Interactive shell](/docs/images/interactiveshell.png)

Script is list of commands:
```
# Load original FIT image (kernel+devtree+ramfs[cpio.gz])...
load original.fit

# Add scp support
include include/scp.module

# Add image version file
file etc/version.txt version.txt rwxr--r-- 0 0

# Store modified FIT image...
store modified.fit
```
Empty lines are skipped. Lines, which starts with '#' are comments and skipped too.

## Common commands
Include another script:
```
include <scriptpath>
```

Information about loaded images:
```
info
```

## Commands for image loading
### Composite images
Load kernel,fs and devtree from FIT image:
```
load <filename.fit>
```

Load only one image from FIT image, where imagetype is "kernel", "ramfs" or "devtree":
```
load <filename.fit> <imagetype> fit
```

### Ramfs image
Load fs from legacy image:
```
load <filename.legacy> ramfs legacy
```
Load fs from compressed file (cpio or ext2 image):
```
load <filename.ct> ramfs <compression>
```
(compression) is "gzip", "lz4", "lzma", "bzip2", "zstd"

Load fs from cpio file:
```
load <filename.cpio> ramfs cpio
```
Load fs from ext2 image:
```
load <filename.ext2> ramfs ext2
```
Load fs from squashfs image:
```
load <filename.ext2> ramfs squashfs
```

### Kernel image
Load kernel from raw binary image:
```
load <filename.raw> kernel raw
```
Load kernel from archived image:
```
load <filename.ct> kernel <compression>
```
(compression) is "gzip", "lz4", "lzma", "bzip2", "zstd"

Load kernel from legacy (uImage, zImage) image:
```
load <filename.img> kernel legacy
```

### Device tree blob
Load device tree from dtb:
```
load <filename.dtb> devtree dtb
```
## Commands for image saving
To store images to legacy or FIT format, there is need to specify os/arch of these images, if they was loaded from images without such info (cpio, gz). 

### Composite images
Store data as FIT image:
```
store <filename.fit>
```

### Ramfs images
Store fs as legacy image:
```
store <filename.legacy> ramfs legacy
```
Store fs as compressed cpio archive:
```
store <filename.cpio.ct> ramfs <compression>
```
(compression) is "gzip", "lz4", "lzma", "bzip2"

Store fs as cpio file:
```
store <filename.cpio> ramfs cpio
```

### Kernel images
Store kernel as raw binary file:
```
store <kernel.raw> kernel raw
```
Store kernel as compressed image:
```
store <kernel.ct> kernel <compression>
```
(compression) is "gzip", "lz4", "lzma"

Store kernel as uncompressed legacy file:
```
store <kernel.uImage> kernel legacy
```

### Device tree
Store device tree as dtb:
```
store <filename.dtb> devtree dtb
```

## Export files from image
Exporting files and folders to specified directory. If directory not exists, it will be created 
```
export <path>
```

## Commands for modify image parameters:
Update target OS for image:
```
set <imagetype> os <ostype>
```
(imagetype) is "ramfs" or "kernel", or "all"

(ostype) is FIT os variants: "linux", etc...

Update target architecture for image:
```
set <imagetype> arch <arch>
```
(imagetype) is "ramfs", "kernel", "devtree" or "all"

(arch) is FIT arch variants: "arm", "arm64", "x86", "x86_64", etc.

Set image name (used in Legacy format):
```
set <imagetype> name <imagename>
```

Set compression type (for FIT/legacy):
```
set <imagetype> compression <compression>
```
(compression) is "none", "gzip", "lzma", "lz4", "bzip2"

Set entry address (for kernel):
```
set kernel entry <hexaddress>
```

Set data load address (for kernel):
```
set kernel load <hexaddress>
```

## Commands for modify fiolesystem content:
Standart arguments for fs commands:
(path) -- path in image filesystem
(localpath) -- path in local filesystem (Windows etc)
(mode) -- unix-like mode like "rwxr--r--"
(user) -- user id. 0 for root
(group) -- group id. 

Add directory:
```
dir <path> <mode> <uid> <gid>
```

Add file:
```
file <path> <localpath> <mode> <uid> <gid>
```

Update file content:
```
file <path> <localpath>
```

Add symlink to <target>:
```
slink <path> <target> <mode> <uid> <gid>
```

Remove file or dir or etc:
```
rm <path>
```

Change file mode:
```
chmod <path> <mode>
```

Change file owner:
```
chown <user>
chown <user> <group>
```

## Interactive shell commands
Print active directory:
```
pwd
```

Change active directory:
```
cd <path>
```

List items in active dir:
```
ls
```

List items in other dir:
```
ls <path>
```