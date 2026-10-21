# TFTP server

`NyaFsTFtp` serves files from `dir` and can map individual request names to
local files or to components of the currently loaded image. This is useful for
network boot: one server can return different kernel/rootfs/dtb images depending
on the filename requested by the device.

## Start / stop

```
service tftp start
service tftp stop
service tftp info
service tftp set port 69
service tftp set dir C:\images
```

If a requested name is not mapped, the server looks for it under `dir`.

## Image mappings

```
tftpimage add <request-name> <source>
tftpimage remove <request-name>
tftpimage list
```

`<source>` is one of:

- `loaded:kernel` — raw bytes of the loaded kernel;
- `loaded:dtb` — flattened device tree built from the loaded DTB;
- `loaded:fs` — loaded filesystem in its native format, or CPIO when the type
  is unknown;
- `loaded:fs:<format>` — forced format: `cpio`, `tar`, `squashfs`, `cramfs`,
  `romfs`, `ext2`, `ext3`, `ext4`, `fat`, `f2fs`, `ubifs`, `littlefs`, `spiffs`;
- a local file path.

Example for a typical netboot setup:

```
tftpimage add zImage loaded:kernel
tftpimage add board.dtb loaded:dtb
tftpimage add rootfs.cpio loaded:fs:cpio
service tftp set port 69
service tftp start
```

The device then requests `zImage`, `board.dtb` and `rootfs.cpio`; the server
returns the corresponding parts of the currently loaded image. If you edit the
filesystem over SFTP/NFS, the next `rootfs.cpio` request contains the edited
files.

The same mapping can be configured through the service:

```
service tftp set image zImage=loaded:kernel
service tftp set image rootfs.cpio=loaded:fs:cpio
```

Mappings are kept in memory for the process lifetime. `tftpimage remove` removes
one; `tftpimage list` shows all mappings.

## Notes

- TFTP has no authentication; use it only on a trusted network.
- The server uses UDP and the standard TFTP block protocol; large images are
  transferred block by block.
- `loaded:fs` builds the whole filesystem in memory on each request. For very
  large images this can take time and memory.
- Local writes (`tftpput`) always go under `dir`; image mappings are read-only.
