# NyaImageTool 1.3.0

## Highlights

- Streaming filesystem writers: every built-in filesystem implements
  `IFilesystemStreamBuilder` and can emit its image directly to a `Stream`.
  Metadata-heavy formats use `IRandomAccessStore`/`ImageStore`; sequential
  formats write straight to the output. Non-seekable sinks fall back to an
  internal `MemoryStream`.
- UBIFS: `UbifsBuilder.PrepareLayout()` computes the final LEB geometry before
  writing; `PebStore` places LEBs directly into UBI PEB data areas.
- Multi-volume UBI: `UbiImageBuilder.BuildTo` streams UBIFS volumes (sizes from
  `PrepareLayout`) and writes static volumes without duplicating LEB arrays.
- NAND/disk layouts: filesystem slots with `content.dir` are written via
  `Writer.WriteFsTo` into `PlanSlot.WriteData`/`RegionStream`; `DiskImageBuilder`
  does the same for partitions. Auto-sized slots probe the size through a
  temporary file (O(1) RAM) and then write the image once more into the slot.
- Nested containers: `format: ubi` and `format: disk` children in NAND/disk
  layouts are streamed through `UbiImageBuilder.BuildTo`/`DiskImageBuilder.BuildTo`.
- Unified stream/byte[] selection: `PlanNodeSerializer` asks the plugin
  (`CompositeImagePlugin.SupportsStreaming`/`BuildTo`) instead of hardcoding
  container formats.

## Compatibility

- `NyaFs` 1.3.0 package; targets netstandard2.0, net8.0, net9.0, net10.0.
- `NyaImageTool` and bundled plugins: 1.3.0, net10.0.
- Layout schema remains `schema: 1`; existing scripts and layouts are unchanged.
- `GetFilesystemImage()` remains available; streaming is additive.

## Verification

- Development suite: 423/423.
- Repository suite: 339/339.
- Golden regression corpus matches; no golden regeneration was required.

## Upgrade notes

- Plugins may implement `IFilesystemStreamBuilder` to opt into streaming; otherwise
  `BaseFsWriter` keeps using `GetFilesystemImage()`.
- Container plugins opt into streaming by overriding
  `CompositeImagePlugin.BuildTo` and returning `true` from `SupportsStreaming`.
- FIT/Android/CIMG and vendor RKFW/SWUpdate/TVIP containers still build in
  `byte[]` when placed in NAND/disk slots. Review if such a slot exceeds ~256 MB,
  causes OOM, or probe+write time becomes noticeable (see `docs/build_plan.md`,
  «Ограничения»).
- Auto-sized filesystem slots build the filesystem twice (probe to a temp file,
  then the real write); this trades CPU/temp disk I/O for bounded RAM.
