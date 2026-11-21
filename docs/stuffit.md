# StuffIt archive wrapper (external plugin)

`NyaFsAncientFs` can open classic StuffIt 1.x/3.x `.sit` archives directly:

```text
load "1.1 System Disk.sit"
ls /
```

Data forks are decompressed and exposed as child files; Disk Copy 4.2, MFS and
HFS detection then runs recursively. This makes the early Macintosh release
archives work without an external `unar` step.

## Reader support

- `SIT!` / `rLau` archive headers;
- stored entries (method 0);
- LZ+Huffman entries (method 13), including the dynamic canonical-Huffman
  variants used by the 400K System disks;
- folder records and nested file names.

## Limitations

- read-only;
- resource forks are not exposed;
- encrypted entries are skipped;
- method 13 static table variants, RLE/Compress/Huffman methods and StuffIt 5
  (`SIT5`) are not implemented yet;
- CRC16 fields are not verified.

## Verification

- real `1.1 System Disk.sit` from earlymacintosh.org decompresses byte-for-byte
  to the same 419284-byte DiskCopy image produced by `unar`;
- Stored-archive test covers the chain StuffIt → Disk Copy 4.2 → MFS;
- CLI smoke test lists the System 1.1 MFS volume after a direct `.sit` load.
