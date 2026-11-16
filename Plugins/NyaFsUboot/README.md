# NyaFsUboot

U-Boot helper plugin for NyaImageTool: reads, edits and creates U-Boot environment images.

The environment is kept as an ordered `key=value` list. Binary images are parsed
with CRC32 verification and written back with a recalculated CRC32, preserving the
source sector size, number of copies and header layout (4-byte CRC, optional
flags byte/u32).

## Commands

```
env load <file>              # parse a binary env image (CRC32 checked)
env save <file>              # write the current env as a binary image
env new [size]               # start an empty env (default 64K, e.g. env new 128K)
env list                     # print all key=value entries
env get <key>                # print one value
env set <key> <value>        # add or replace a value (quote values with spaces)
env unset <key>              # remove a key
env import <textfile>        # replace entries from a key=value text file
env export <textfile>        # write the entries as a key=value text file
env size <size>              # set sector size (64K, 128K, 0x10000, ...)
env layout <offset> <flags>  # header layout: offset 4/5/8 and flags value
env redundant on|off         # write one or two copies
```

`env import` ignores empty lines and lines starting with `#`.

## Examples

Edit a real environment and write it back:

```
env load F:\TEMP\Images\vendor-raw\whats-env.bin
env get bootcmd
env set bootdelay 5
env set bootcmd "run setargs_nand boot_normal"
env save F:\TEMP\env-new.bin
```

Create one from text:

```
env new 128K
env import F:\TEMP\env.txt
env save F:\TEMP\env.bin
```

`env.txt`:

```
bootdelay=1
bootcmd=run boot
```

## Layouts

| data offset | header |
|---|---|
| 4 | `crc32` then `key=value...` (non-redundant) |
| 5 | `crc32`, one flags byte, then `key=value...` |
| 8 | `crc32`, u32 flags, then `key=value...` |

Redundant images use two sectors of `region size` bytes; each sector has its own
CRC and flags. The loader recognises `crc32` with the standard reflected CRC32
polynomial (`0xEDB88320`), i.e. the same value as `crc32(0, data, len)` in U-Boot.
