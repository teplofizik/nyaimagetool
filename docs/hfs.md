# HFS classic (read-only)

`NyaFs` reads HFS (Mac OS Standard, "classic HFS") volumes. Writing is not supported.

## Commands

```text
load hfs.img
export out/
```

The reader is selected automatically by the master directory block signature
(`BD` at offset 1024).

## Reader support

- HFS master directory block (MDB), allocation block size and volume name;
- catalog B-tree leaf records (directories, files, thread records ignored);
- data forks (first three extents plus extents-overflow records);
- nested directories and file data.

## Limitations

- read-only: `store`/`build` with `format: hfs` are not available;
- resource forks are not exposed;
- HFS has no POSIX permissions or symlinks, so exported mode bits are synthetic;
- HFS+ (`H+`) is a different format and is not handled by this reader.

## Verification

- fixture generated with `hformat`/`hmount`/`hcopy` from `hfsutils`, containing
  nested directories and a 40 KiB file;
- a fragmented image with an extents-overflow record was used to verify data-fork
  reassembly (temporary test, not committed);
- `HfsTests` covers detection, directory tree and file contents.
