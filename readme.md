# NyaImageTool

There is a tool for editing and converting uboot images in different formats: cpio, gz, legacy, FIT etc.
There is possible to add or update files in ramfs image.

## Install / build

See [docs/BUILDING.md](docs/BUILDING.md) for Windows and Ubuntu instructions:
.NET 10 SDK, local NuGet feed (`NyaFs`, `NyaXZ`), build, tests, run and publish.

![Run on linux](/docs/images/linux.png)

## Supported image formats
1. Kernel: compressed raw, fit, android, raw, legacy
2. Ramfs: cpio, ext2, squashfs, cramfs, romfs, compressed cpio or ext2, legacy, fit, android image
3. Device tree: dtb, fit

## Supported filesystems
Supported at now:
1. CPIO ASCII (RW)
2. TAR (RW, GNU long names, PAX xattrs)
3. EXT2 / EXT3 / EXT4 (RW; xattr: external block + inline/EA-inode read)
4. SquashFs (RW; extended inodes + xattr table)
5. CramFs (RW)
6. RomFs (RW)
7. FAT12 / FAT16 / FAT32 (RW, VFAT long names)
8. F2FS (RW): files/dirs/symlinks, indirect/double-indirect, xattr (inline + external
   node) and per-file compression (ZSTD/LZ4/LZO/LZORLE, configurable cluster size).
9. UBIFS (RW): writer with LZO/Zlib/Zstd data compression, big LPT/LSAVE, xattr
   (XENT + hidden xattr inodes).
10. LittleFS / SPIFFS (RW, plugins).

Extended attributes are kept in the generic filesystem model; they are written
for EXT2/3/4 (external block; inline and EA-inode values are read), F2FS
(inline/external), SquashFS (extended inodes + xattr table, including out-of-line
values), UBIFS (XENT + hidden xattr inodes) and preserved inside packages
(tar PAX, including root xattrs).

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

SFTP server is provided by [freesftpsharp](https://github.com/mikaelliljedahl/freesftpsharp/) and [FxSsh](https://github.com/Aimeast/FxSsh). freesftpsharp is slightly rewrited and adopted for virtual fs.

2. TFTP (client/server)
TFTP server is available to provide tftp-access to builded packages. TFTP client allow download and upload files.
TFTP support is provided by [tftp.net](https://github.com/Callisto82/tftp.net) library.

3. Remote filesystem clients (NyaFsRemote.dll)
SCP, SFTP, FTP and SSH `cat` transfers between the loaded filesystem and a device.
See [docs/remote.md](docs/remote.md).

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
(fs) is one of 'ext2', 'squashfs', 'cramfs', 'romfs', 'f2fs' or 'cpio'.

Change squash compression type:
```
set ramfs squashfs.compression <compression>
```
(compression) is one of "gzip", "lzma", "lz4", "zstd"

F2FS per-file compression and compression cluster size:
```
set ramfs f2fs.compression <none|zstd|lz4|lzo|lzorle>
set ramfs f2fs.compression.cluster <16K|32K|64K|...|1M>
```
Cluster size must be a power of two between 16K and 1M (default 16K).

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

Remove file or dir or etc with excluded pathes (allowed * for mask):
```
rm <path1> [<path2>] [<path3>] [-<exclude1>] [-<exclude2>]
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

Set an extended attribute (UTF-8 value, or `0x`-hex; `-` removes):
```
xattr <path> <name> <value>
```

## Packages

A package is one file (`.nyapkg`): gzip(tar) with a `PACKAGE` control file
(`key: value` lines; unknown keys are preserved, so the format is extensible)
and a `data/` tree. Metadata can carry name, version, arch, os, description and
arbitrary extra fields; configuration/settings files can be shipped inside a
package as ordinary files. Extended attributes are preserved in the package
(PAX `SCHILY.xattr.*`) and restored on `pkg add`; device nodes and FIFOs are
preserved as tar entry types.

Create a package from a subtree of the current in-memory filesystem:
```
pkg create <file.nyapkg> <fs-path> <name> <version>
```
If target arch/os are set (`set ... arch/os`), they are stored in the metadata.

Print package metadata:
```
pkg info <file.nyapkg>
```

Install package data into the current filesystem; the package is skipped when
its arch/os does not match the target:
```
pkg add <file.nyapkg>
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
2. NyaFsLinux.dll
3. NyaFsTFtp.dll
4. NyaFsFiles.dll
5. NyaFsRemote.dll\n6. NyaFsFtp.dll
Plugins must be placed at 'plugins' folder, then they will be loaded on app startup.

## SFTP plugin (NyaFsSftp.dll)
Default server port is 22.
Start server:
```
service sftp start
```
Stop server:
```
service sftp stop
```
Information about server parameters:
```
service sftp info
```
Port selection (while server is stopped):
```
service sftp set port <port>
```
There are need to setup in WinSCP connection settings sending null packets every 20-30 sec to keep connection alive. Timeout is 45 seconds.

## TFTP plugin (NyaFsTFtp.dll)
Default server port is 69.
Start server:
```
service tftp start
```
Stop server:
```
service tftp stop
```
Port selection (while server is stopped):
```
service tftp set port <port>
```
Information about server parameters:
```
service tftp info
```
Directory selection (while server is stopped):
```
service tftp set dir <directory>
```

Downloading from remote tftp server:
```
tftpget <filename> <server> <remotefilename>
```
(filename) - filename of file to write received data
(server) - ip or hostname of remote server
(remotefilename) - filename of requested file on server

Uploading to remote tftp server:
```
tftpput <filename> <server> <remotefilename>
```
(filename) - filename with content of sended file
(server) - ip or hostname of remote server
(remotefilename) - filename of target file on server

## Remote plugin (NyaFsRemote.dll)

Copy a file or directory between the loaded filesystem and a remote device:
```
remoteget <protocol> <host[:port]> <login> <pass> <fs-path> <remote-path>
remoteput <protocol> <host[:port]> <login> <pass> <fs-path> <remote-path>
```
Protocols: `ssh`, `scp`, `sftp`, `ftp`. `ssh` uses a `cat` exec channel and works
on devices without SFTP/SCP. Directory transfers are recursive.
The old `copysshfrom` command remains as an alias for `remoteget ssh`.
See [docs/remote.md](docs/remote.md) for details and examples.

## Operations with local files (NyaFsFiles.dll)
Copy file on local filesystem:
```
copy <src> <dst>
```
(src) - filename of file to be copied
(dst) - filename of new file

Download file from http/https:
```
download <filename> <url>
```
(filename) - filename of file to write received data
(url) - url of remote file

Remove file from local filesystem:
```
remove <filename>
```

Load local directory to filesystem:
```loaddir <path> <localdir> <dirmode> <filemode> <user> <group>```
Example:
```loaddir /tmp/xxx include/xxx rwxr-xr-x rw-r--r-- 0 0```

Load files from zip archive to filesystem:
```loadzip <path> <zippath> <dirmode> <filemode> <user> <group>```
Example:
```loadzip /www www.zip rwxr-xr-x rw-r--r-- 0 0```


## Linux-specific operations (NyaFsLinux.dll)
List of all users (from /etc/passwd):
```
lsusr
```
List of password hashes (from /etc/shadow):
```
lshashes
```

Example of users inspection:
![Interactive shell](/docs/images/plugins/linux_lsusr.png)

Set new password for user (/etc/shadow will be updated, SHA-512 hash is used by default):
```
mkpasswd <user> <password>
```

Check user password - is password valid for this user:
```
passwd <user> <password>
passwd <user>
```
(password) - password to check. If field is not specified, user will be checked for empty password.

Find user password from list of passwords (bruteforce, very slow, but applicable for finding which password from list of used passwords was used in loaded rootfs)
```
findpasswd <user> <listfilename>
```
(listfilename) is filename of file, which contains a list of passwords to check.

