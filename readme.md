# NyaImageTool

There is a tool for editing and converting uboot images in different formats: cpio, gz, legacy, FIT etc.
There is possible to add or update files in ramfs image.

## Supported image formats
1. Kernel: gz, fit
2. Ramfs: cpio, cpio.gz, legacy, fit
3. Device tree: dtb, fit

## Supported filesystems

Supported at now:
1. CPIO ASCII

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
Load fs from cpio.gz file:
```
load <filename.cpio.gz> ramfs gz
```
Load fs from cpio file:
```
load <filename.cpio> ramfs cpio
```
Load kernel from gzipped image:
```
load <filename.gz> kernel gz
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
Store device tree as dtb:
```
store <filename.dtb> devtree dtb
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