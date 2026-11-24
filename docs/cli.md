# Layout CLI

Declarative builds use a YAML/JSON schema (`schema: 1`). Details:
[build_plan.md](build_plan.md); filesystem plugin contract: [fs_plugin.md](fs_plugin.md).

## `build`

Build an image in one step (no session state):

```text
build <schema.yaml> <output>
```

Examples:

```yaml
# NAND image with two slots
schema: 1
format: nand
options: { page: 2048, oob: 0, block: 131072, blocks: 2048 }
children:
  - { name: boot, offset: 0x0, size: auto, kind: raw,
      content: { file: inputs/bootloader.bin } }
  - { name: rootfs, offset: 0x200000, size: auto, kind: filesystem,
      format: ubifs, content: { dir: inputs/rootfs } }
```

```yaml
# Standalone squashfs
schema: 1
format: squashfs
options: { compression: lz4 }
content: { dir: inputs/rootfs }
```

```yaml
# Disk with GPT
schema: 1
format: disk
options: { table: gpt, align: 1M }
children:
  - { name: boot, offset: 1M, size: 16M, kind: raw,
      content: { file: inputs/boot.img } }
```

`size` is the output/region length (`auto` = natural), `fs-size` is the
filesystem volume size, `fill` is the padding byte.

## `mkimage`

Create an editable session plan from a schema:

```text
mkimage <format> <schema.yaml>
```

For a composite format the plan contains slots; use `use`, `import`, `file`,
`mkfs`, `loaddir` and save with `store`.

For a filesystem (`cpio`, `ext4`, `squashfs`, ...) it creates the active
filesystem node, optionally pre-filled from `content.dir`:

```text
mkimage cpio rootfs.yaml
store rootfs.cpio ramfs cpio
```

```text
mkimage cpio empty.yaml      # no content: empty filesystem
loaddir inputs/rootfs /
store rootfs.cpio ramfs cpio
```

`content.compression` is applied by `build`; `store` writes the raw filesystem.

## `layout export` / `layout validate`

```text
layout export <image> <schema.yaml>
layout validate <schema.yaml>
```

- `export` writes a buildable schema. Filesystems are extracted to
  `<schema>.files/<path>/` as `content.dir` (editable, not immutable); kernels,
  DTBs and containers are written to `<schema>.blobs/<sha256>` as `content.file`.
  If a filesystem contains device/fifo/socket nodes, it falls back to an
  immutable blob.
- `validate` parses the schema, resolves the plugin, builds content in memory
  and reports the resulting size. It works for composite and filesystem roots.

## `store`

```text
store <file> <type> <format>
```

- composite: `store out.img all rkfw`, `store out.ubi all ubi`, ...
- filesystem: `store out.cpio ramfs cpio`, `store out.ext4 ramfs ext4`, ...

## `drop`

```text
drop <plan-path>
```

Remove an element from a loaded image before `store`: the container is rebuilt
without it. Only containers that report `drop=yes` in `inspect` are allowed
(for example FIT, legacy multi, multi-volume UBI, RKFW, merged BMU, disk, NAND
and SPI-flash). For disk/NAND/SPI the slot is kept but the region is blanked
(`0x00` for disks, `0xFF` for flash). Layout templates (`mkimage`) cannot be
edited this way.

## UBI and ISO

```yaml
# Multi-volume UBI
schema: 1
format: ubi
options: { peb: 128K, page: 2K }
children:
  - { name: rootfs, kind: filesystem, format: ubifs,
      content: { dir: inputs/config_root } }
  - { name: kernel, kind: raw, volume-type: static,
      content: { file: inputs/Image } }
```

```yaml
# Bootable ISO9660 (El Torito, no emulation)
schema: 1
format: iso9660
options: { boot-image: boot/loader.bin, boot-load-size: 8 }
content: { dir: inputs/cd_root }
```
