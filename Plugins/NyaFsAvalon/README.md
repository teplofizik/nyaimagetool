# NyaFsAvalon

Canaan Avalon Upgrade Package (`.aup`) reader and writer.

Format information and sample packages:
https://github.com/HashSource/avalon_upgrade_package_extract

The package consists of an `AUP format` header (versions 0, 1 and 2) followed by
a raw firmware payload. The plugin exposes the payload as `payload.bin`; in
current Avalon firmware it is usually encrypted/opaque, so nested detection is
not attempted. Header metadata (firmware version, hardware/software lists and
CRC values) is available through `ReadOptions`/`ReadDescription`.

Use:

```text
load package.aup
ls
```

Writing preserves the original header metadata and refreshes the payload length,
payload CRC32 and, for AUP v2, the header CRC32.
