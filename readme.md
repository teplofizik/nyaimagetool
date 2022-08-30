# NyaImageTool

There is a tool for editing and converting uboot images in different formats: cpio, gz, legacy, FIT etc.
There is possible to add or update files in ramfs image.

## Supported image formats
1. Kernel: gz, fit, raw, legacy (uImage, zImage)
2. Ramfs: cpio, cpio.gz, ext2.gz, legacy, fit, ext2
3. Device tree: dtb, fit

## Supported filesystems

Supported at now:
1. CPIO ASCII (RW)
2. EXT2 (R)

## How to
There are need to add scp support to image and add version information (device id or other info).
Run image processing:
```
./NyaImageTool.exe <scriptfilename>
```

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

## Commands for image loading
Load kernel,fs and devtree from FIT image:
```
load <filename.fit>
```
Load only one image from FIT image, where imagetype is "kernel", "ramfs" or "devtree":
```
load <filename.fit> <imagetype> fit
```
Load fs from legacy image:
```
load <filename.legacy> ramfs legacy
```
Load fs from gz file (cpio or ext2 image):
```
load <filename.gz> ramfs gz
```
Load fs from cpio file:
```
load <filename.cpio> ramfs cpio
```
Load fs from ext2 image:
```
load <filename.ext2> ramfs ext2
```
Load kernel from raw binary image:
```
load <filename.gz> kernel raw
```
Load kernel from gzipped image:
```
load <filename.gz> kernel gz
```
Load kernel from legacy (uImage, zImage) image:
```
load <filename.gz> kernel legacy
```
Load device tree from dtb:
```
load <filename.dtb> devtree dtb
```
## Commands for image saving
To store images to legacy or FIT format, there is need to specify os/arch of these images, if they was loaded from images without such info (cpio, gz). 

Store data as FIT image:
```
store <filename.fit>
```
Store fs as legacy image:
```
store <filename.legacy> ramfs legacy
```
Store fs as cpio.gz archive:
```
store <filename.cpio.gz> ramfs gz
```
Store fs as cpio file:
```
store <filename.cpio> ramfs cpio
```
Store kernel as raw binary file:
```
store <kernel.raw> kernel raw
```
Store kernel as gz file:
```
store <kernel.gz> kernel gz
```
Store kernel as uncompressed legacy (uImage) file:
```
store <kernel.uImage> kernel uImage
```
Store kernel as compressed legacy (zImage) file:
```
store <kernel.zImage> kernel zImage
```
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
<imagetype> is "ramfs" or "kernel", or "all"
<ostype> is FIT os variants: "linux", etc...

Update target architecture for image:
```
set <imagetype> arch <arch>
```
<imagetype> is "ramfs", "kernel", "devtree" or "all"
<arch> is FIT arch variants: "arm", "arm64", "x86", "x86_64", etc.

Set image name (used in Legacy format):
```
set <imagetype> name <imagename>
```

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
<path> -- path in image filesystem
<localpath> -- path in local filesystem (Windows etc)
<mode> -- unix-like mode like "rwxr--r--"
<user> -- user id. 0 for root
<group> -- group id. 

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