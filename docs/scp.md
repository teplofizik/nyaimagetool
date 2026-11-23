# SCP Apple II flux / 5.25" (external plugin, read-only)

`NyaFsAncientFs` reads SuperCard Pro `.scp` flux images for Apple II 5.25"
disks:

```text
load "clean_prodos.scp"
ls /
export out/
```

SCP stores flux-transition intervals, not sectors. The reader applies a PLL
(4 µs Apple II bitcells) to recover the nibble stream, decodes 6-and-2 GCR
address/data fields and exposes a `disk.po` child in ProDOS logical order.

## Reader support

- SCP track table/TDH parsing, 16-bit big-endian flux words and zero
  (65536-tick) gaps;
- Greaseweazle-style PLL: clock 4 µs, ±10% range, period 5% / phase 60%;
- 35-track single-sided Apple II 5.25" images;
- 6-and-2 GCR with checksum validation;
- ProDOS physical-to-logical sector mapping.

## Limitations

- read-only;
- only single-sided 35-track Apple II 5.25" images;
- DOS 3.3 ordering, 13-sector DOS 3.2, 3.5" and copy-protected tracks are not
  handled;
- the flux PLL is tuned for standard 300 rpm 4 µs Apple II disks.

## Verification

- clean SCP produced by Greaseweazle `gw convert --format apple2.prodos.140`
  from a validated ProDOS image;
- `ScpTests` checks flux decoding, ProDOS detection and pipeline loading;
- the `Letter Wizard` SCP from the public “raw with errors” collection is
  intentionally not used as a validation fixture.
