# NyaImageTool 1.3.1

## Highlights

- Packages: `pkg meta <file> <key> <value|->` edits the `PACKAGE` metadata
  (`depends`, `conflicts`, `provides`, `description`, custom fields) and repacks
  the package with its existing compression.
- `PackageDatabase.Resolve` (used by `pkg adddir`) now honours `provides`, not
  only direct package names; version constraints and `provides` versions are
  checked consistently with `CheckInstall`.
- `pkg info` prints all metadata fields (including dependencies/provides/
  compression) and the entry count.
- Documentation for packages updated: `createdir`, `adddir`, `meta`, gzip/zstd,
  version operators and dependency resolution.

## Compatibility

- `NyaFs` 1.3.1 package; targets netstandard2.0, net8.0, net9.0, net10.0.
- `NyaImageTool` and bundled plugins: 1.3.1, net10.0.
- Package format is unchanged: `.nyapkg` gzip/zstd(tar) with `PACKAGE` + `data/`.
- No breaking API changes; `PackageMetadata` gained `Remove(key)`.

## Verification

- Development suite: 425/425.
- Repository suite: 341/341.
- Added tests: `Package_Database_Resolve_Uses_Provides` and
  `Package_Cli_Meta_Updates_Dependency_And_Keeps_Compression`.

## Upgrade notes

- `pkg add` still installs a single package and checks it against the installed
  database; use `pkg adddir` for dependency graph resolution.
- A `provides` entry without a version satisfies any version constraint; if both
  the provide and the dependency specify versions, the operator is honoured.
