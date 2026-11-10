# CIMG / CVITEK image container

CIMG is a container, not a filesystem. The current format comes from
`tools/image_tools/raw2cimg.py` in `sophgo/cvi_rtt`.

## Current header

```text
offset  size  field
0x00    4     magic "CIMG"
0x04    4     version
0x08    4     chunk-header size (64)
0x0C    4     chunk count
0x10    4     payload + chunk headers size
0x14    32    partition label / extra flags
0x34    12    reserved
```

Each chunk at `0x40`:

```text
offset  size  field
+0x00   4     chunk type (1 = CRC-checked data)
+0x04   4     chunk data size
+0x08   4     program part offset
+0x0C   4     program part size
+0x10   4     CRC32 of the chunk data
+0x14   44    reserved
+0x40   N     data
```

The first chunk data therefore starts at `0x80` for the usual single-chunk
image. `CvImage` also accepts the older single-payload CIMG layout where the
CRC is stored at `0x4C`, because existing vendor images (for example
`boot.emmc`) still use it.

## Writing

The formatter is implemented by `CImgBuilder` / `CImgWriter`. It produces the
chunked format used by `raw2cimg.py`:

```text
set container cimg.label TEST
set container cimg.chunk-size 16M
set container cimg.offset 0
set container cimg.part-size 0
store out.cimg ramfs cimg
store out.cimg kernel cimg
store out.cimg devtree cimg
store out.cimg all cimg
```

`chunk-size` controls the 16 MiB payload split; `offset` and `part-size` are
written into every chunk header. `part-size=0` uses the payload size.

When a CIMG is loaded, its header metadata pre-fills `cimg.label`,
`cimg.offset` and `cimg.part-size`, so a plain load/store keeps the original
partition label without an explicit `set container` command.

`store ... all cimg` wraps a FIT image built from the currently loaded
kernel/devicetree/ramfs. This covers the `all/cimg/fit/{devtree,kernel}` case:
loading a CIMG whose payload is FIT loads its children recursively, and saving
with `all cimg` writes a new CIMG containing a rebuilt FIT.

## Regeneration check

`CImgBuilder` preserves the original payload bytes and partition label when
re-wrapping an existing CIMG. The container header is normalized to the current
chunked layout; legacy single-payload headers are accepted on read.

For `ramfs/cimg/minerfs.gz`, a full `load -> store ... ramfs cimg -> load`
round-trip produced the same 419 exported files with identical hashes. The inner
filesystem is reserialized, so the compressed payload bytes are not expected to
match the original exactly.

For `vendor-raw/boot.emmc`, `load -> store ... all cimg -> load` preserves the
kernel and devicetree payloads and the `kernel` label. The FIT wrapper itself is
rebuilt, so the outer CIMG/FIT bytes differ from the vendor image.

## Loading

`CImgImagePlugin` is a `CompositeImagePlugin`. It exposes the concatenated
chunk payload to `ImagePipeline`, which detects whether the payload is:
- a FIT / DTB;
- a kernel;
- a gzip-compressed ramfs;
- another container.

`ArchiveHelper` reports the CIMG detection as `all/cimg`.
