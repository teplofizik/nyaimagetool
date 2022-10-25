# TODO

## Filesystems
CramFS Big Endian (select+detection)
TAR
VFAT
EXT3 BASE
EXT4 BASE

Detection of filesystem limitations before writing! Uid/Gid, mode.
Scan filesystem: detect elf arch, etc...

## Bugfix
Root node mode!..

## Composite Images
Android boot image: v3-4 reader v0-4 writer
TVIP firmware
OpenWRT
Legacy multiimage

## Encryption
FIT encryption (AES)

## Plugins
Plugin system
* Change all fs readers/writers to plugin-base system, provided by NyaPlugin class: Provide reader, provide writer
* Change all compressors as plugins
* Change all image formats as plugins
* Change all commands to plugin
Editing filesystem: change users, groups, rights
Lua scripts as commands: open file, edit, save

Filesystem i/o
  [optional] Filesystem compression wrapper [cramfs, squashfs has internal compression, additional compression is not needed]
    [optional] Image


##  Additional images processing
Uboot (TVIP)
Secondary bootloader (Android)
Recovery DTB (Android)
Different resources (TVIP splash)

## Commands
User/Group id change for all system: from -> to


## Remote access
Access, adding and editing files from exist commanders like mc, WinSCP and other:
SCP
SFTP
FTP
NFS

TFTP: return different images based on requested filename

## Links
1. [filesystems info](https://gitlab.arm.com/linux-arm/linux-ae/-/tree/WIP-pmu-nmi/fs)