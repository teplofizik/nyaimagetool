# NyaFsWdNas

Western Digital My Cloud DL2100 firmware `.bin` container reader/writer.

Format specification, examples and reference tools:
https://github.com/michaelroland/wdnas-firmware-modding

The container has a 128-byte main header followed by `uImage`, `uRamdisk`,
`image.cfs` (2048-byte header + SquashFS), `config_default.tar.gz`, and optional
extraction targets. The plugin exposes all images; `image.cfs` also exposes its
inner `image.squashfs`.

Writing preserves the header/extraction metadata and refreshes offsets, lengths,
XOR checksums and the main-header checksum. The original file is returned
byte-for-byte when nothing is dirty.
