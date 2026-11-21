# NyaFs 1.4.3

## HFS classic read-only

Classic HFS (Mac OS Standard) volumes can now be inspected:

```text
load hfs.img
export hfs-out/
```

The reader handles the MDB, catalog B-tree, nested directories, data forks and
extents-overflow records. It is selected automatically by the `BD` signature at
offset 1024.

HFS+ is a separate format and is not covered by this reader.

## Verification

- fixture generated with `hfsutils` (`hformat`/`hmount`/`hcopy`);
- fragmented fixture with extents-overflow records verified manually;
- `HfsTests` covers detection, tree traversal and file contents.

## Upgrade notes

- Package version is now `1.4.3`; update `PackageReference NyaFs` in all
  consuming projects.
- No changes to existing readers.
