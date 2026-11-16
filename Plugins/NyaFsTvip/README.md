# NyaFsTvip

TVIP firmware container plugin for NyaImageTool.

## Format

```
0x00  "TVIP" | 20 bytes global header (preserved) | model\0 | version\0
0x24  sections:
        name[7]        (NUL-terminated, e.g. kernel/rootfs/splash/uboot)
        flags    u32 BE
        size     u32 BE (declared section size)
        md5[16]        (MD5 of the section data)
        data...
```

Sections are exposed as opaque children. The normal pipeline recurses and
detects:

- `kernel` — U-Boot legacy image (often LZMA compressed)
- `rootfs` — CramFS (including the 16-byte-inode TVIP variant)
- `splash` — BMP (opaque)
- `uboot` — opaque (the collected sample is truncated)

## Commands

```
load <firmware.bin>
inspect <firmware.bin>
extract <firmware.bin> rootfs F:\TEMP\rootfs.cramfs
```

Editing the rootfs and writing the container back:

```
load F:\TEMP\Images\vendor-raw\tvip_firmware.bin
file etc/version.txt F:\TEMP\version.txt rw-r--r-- 0 0
store F:\TEMP\tvip-new.bin all tvip
```

On save, dirty sections get a recalculated size and MD5; untouched sections are
copied byte-for-byte (a truncated final section keeps its declared size/MD5).
The global 0x24-byte header is preserved as-is.

## CramFS 16-byte inodes

TVIP rootfs images use a CramFS variant with 16-byte inodes: the 32-bit word at
inode +8 stores `padded_name_length << 6` and the offset is a separate u32 at
inode +12 (in 4-byte units). `CramFsReader` auto-detects this layout by probing
the root inode, and `CramFsBuilder` can write it (`Fs.CramFsWideInodes`).
