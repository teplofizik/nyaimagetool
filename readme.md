# NyaImageTool

There is a tool for editing and converting uboot images in different formats: cpio, gz, legacy, FIT etc.
There is possible to add or update files in ramfs image.

## Supported image formats
1. Kernel: compressed raw, fit, android, raw, legacy
2. Ramfs: cpio, ext2, squashfs, cramfs, romfs, compressed cpio or ext2, legacy, fit, android image
3. Device tree: dtb, fit

## Supported filesystems
Supported at now:
1. CPIO ASCII (RW)
2. EXT2 (RW)
3. SquashFs (RW)
4. CramFs (RW)
5. RomFs (RW)

## Supported compression types
Supported at now:
1. GZip (RW)
2. LZMA (RW)
3. LZ4 (RW)
4. BZIP2 (RW)
5. XZ (R)
6. LZO (R)
7. ZStd (RW)

LZMA compression is provided by LZMA SDK package.
LZ4 compression is provided by FT.LZ4 package.
BZIP2 compression is provided by SharpZipLib package.
XZ compression is provided by SharpCompress package.
LZO compression is provided by NyaLZO library (Decompression code is ported from lzo1x_decompress_safe.c)
ZStd compression is provided by ZstdSharp package.

## Supported protocols
1. SFTP (server)
SFTP server is available for fast inspecting a content of loaded filesystem.

SFTP server is provided by (freesftpsharp)[https://github.com/mikaelliljedahl/freesftpsharp/] and (FxSsh)[https://github.com/Aimeast/FxSsh]. freesftpsharp is slightly rewrited and adopted for virtual fs.

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

Reset program state, drop all images:
```
reset
```

Echo any text:
```
echo <text>
```

## Commands for image loading
Default load command (type and image format autodetect):
```
load <filename>
```

### Composite images
Load only one image from FIT image, where imagetype is "kernel", "ramfs" or "devtree":
```
load <filename.fit> <imagetype> fit
```

### Ramfs image
Load fs from legacy image:
```
load <filename.legacy> ramfs legacy
```
Load fs from compressed file (cpio, ext2, squashfs image):
```
load <filename.ct> ramfs <compression>
```
(compression) is "gzip", "lz4", "lzma", "bzip2", "zstd", "lzo"

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
load <filename.sqfs> ramfs squashfs
```
Load fs from cramfs image:
```
load <filename.cramfs> ramfs cramfs
```
Load fs from romfs image:
```
load <filename.romfs> ramfs romfs
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
(compression) is "gzip", "lz4", "lzma", "bzip2", "zstd", "lzo"

Load kernel from legacy (uImage, zImage) image:
```
load <filename.img> kernel legacy
```

### Device tree blob
Load device tree from dtb:
```
load <filename.dtb> devtree dtb
```

Load device tree from compressed file (dtb image):
```
load <filename.ct> devtree <compression>
```
(compression) is "gzip", "lz4", "lzma", "bzip2", "zstd", "lzo"
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
Store fs as compressed cpio/ext2 archive:
```
store <filename.fs.ct> ramfs <compression>
```
(compression) is "gzip", "lz4", "lzma", "bzip2"

Store fs as romfs image:
```
store <filename.romfs> ramfs romfs
```

Store fs as cramfs image:
```
store <filename.cramfs> ramfs cramfs
```

Store fs as squashfs image:
```
store <filename.cpio> ramfs squashfs
```

Store fs as ext2 image:
```
store <filename.ext2> ramfs ext2
```

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
Update filesystem type:
```
set ramfs filesystem <fs>
```
(fs) is one of 'ext2', 'squashfs', 'cramfs', 'romfs' or 'cpio'.

Change squash compression type:
```
set ramfs squashfs.compression <compression>
```
(compression) is one of "gzip", "lzma", "lz4", "zstd"

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
(compression) is "none", "gzip", "lzma", "lz4", "bzip2", "zstd"

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

Add char device:
```
char <path> <major> <minor> <mode> <uid> <gid>
```

Add block device:
```
block <path> <major> <minor> <mode> <uid> <gid>
```

Add socket:
```
sock <path> <mode> <uid> <gid>
```

Add fifo:
```
fifo <path> <mode> <uid> <gid>
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

## Services
```
service <name> <command>
```
At now available only one service: 'sftp'.
Commands is 'start', 'stop' or 'status'.

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

## Variables
Variables can be defined with command:
```
var <name> <value>
```
or
```
var <name>
```
Variable names starts with $: $image, $path etc.

Variables can be used instead parameters:
```
var $image zynq.fit
load $image
```

Variables can be used in conditions. If variable defined, command line will be executed.
```
<var>? <command> <args...>
```
Example:
```
# Define image1 variable
var $image1

# Update some config based on condition:
$image1? file etc/config.cfg files/image1/config.cfg
$image2? file etc/config.cfg files/image2/config.cfg
```

As variant, variables can contain part of path:
```
var $image image1
echo Image is $image
file etc/config.cfg files/$image/config.cfg
```

## Plugins
Plugin is a dll that contain adiitional functionality.
Available plugins:
1. NyaFsSftp.dll

Plugins must be placed at 'plugins' folder, then they will be loaded on app startup.
