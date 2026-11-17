# UBIFS

`NyaFs` supports both forms of UBIFS images:

- raw UBIFS volume (UBIFS nodes start at offset 0);
- UBI container with one or more UBIFS volumes (`UBI#` headers).

Raw UBIFS is detected by the `0x06101831` node magic and the superblock node
type; the LEB size is read from the superblock. A raw image is written back as
raw, a UBI image as UBI.

To force the output form while writing:

```text
set ramfs ubifs.container ubi
set ramfs ubifs.container raw
store out.ubifs ramfs ubifs
```

The default is `ubi`. Existing raw images keep `raw` automatically after
`load`, so a simple `store <out>` repack preserves the container form.

## Multi-volume UBI (layout)

`build` can create a UBI container with several volumes in one node:

```yaml
schema: 1
format: ubi
options: { peb: 128K, page: 2K }
children:
  - name: rootfs
    kind: filesystem
    format: ubifs
    content: { dir: inputs/config_root }
  - name: kernel
    kind: raw
    volume-type: static
    content: { file: inputs/Image }
```

- `format: ubifs` + `content.dir` is packed as a raw UBIFS volume (dynamic);
  other children are static volumes (or `volume-type: static|dynamic`);
- `peb`/`block` and `page` set the UBI geometry; `total`/`size` pads the image
  with `0xFF`;
- `volume-id` and `autoresize` override per-volume defaults;
- compression for UBIFS volumes: `options: { compression: lzo|zlib|zstd }`.

A single-volume UBI is still detected as `ramfs/ubifs`; multi-volume UBI is
exposed as a container with one child per volume.