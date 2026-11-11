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
