# Allwinner IMAGEWTY format (brief)

Reference: https://github.com/Ithamar/awutils/blob/master/imagewty.h (see also NyaFsAllwinner/Types/WtyImage.cs).

- Magic `"IMAGEWTY"` at 0x00; `header_version` at 0x08: `0x0100` or `0x0300`.
- Main header: `header_size` (0x0C), `ram_base` (0x10), `format_version` (0x14, `0x100234`),
  `image_size` (0x18), `image_header_size` (0x1C).
- Image header at 0x20 (`IMAGEWTY_HEADER`): `pid`(0x00), `vid`(0x04), `hardware_id`(0x08),
  `firmware_id`(0x0C), `num_files`(0x18). For v0x0300 the fields are shifted by +4.
- For each file `i` (0..num_files-1) a 1024-byte `file_header` at `1024 * (1 + i)`:
  `filename_len`(0x00), `total_header_size`(0x04), `maintype`(0x08, 8), `subtype`(0x10, 16),
  then (with a version-dependent +0x100 field offset for 0x0300):
  `stored_length`, `original_length`, `offset`, and `filename` (up to 256).

Data of partition `i` = `[offset, offset + original_length)`.
