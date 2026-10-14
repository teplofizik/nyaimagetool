# Bitmain BMU format (brief)

Reference: https://github.com/VladTheJunior/BitmainFirmwareUnpacker (bmu.py). All multi-byte fields are big-endian unless noted.

## Single BMU
Magic byte `0x26` at offset 0. Total header = 2048 bytes.

| Offset | Size | Meaning |
| --- | --- | --- |
| 0x00 | 1 | magic = `0x26` |
| 0x02 | 8 | miner type hash (u64) |
| 0x0B | 2 | content bitmask (u16); `file_count = popcount(content)` |
| 0x18 | 2 | `miner.pem` length (u16) |
| 0x18+? | … | `miner.pem` (at 0x18 = 24), `miner.pem.sig` at 0x418 (1048), 256 bytes |
| 0x518 (1304) | 1 | file_count |
| 0x51D (1309) | 5*N | file descriptors: `u8 code` + `u32 size` |
| 0x800 | … | payload: files stored sequentially (sizes from descriptors) |
| after payload | 256*N | per-file signatures |
| last 256 | 256 | `bmu.sig` |

File codes: 0=BOOT.bin, 1=devicetree.dtb, 2=uImage, 3=minerfs.image.gz, 4=update.image.gz, 5=crl.tar.gz, 6=miner.btm.tar.gz, 7=reserve, 9=datafile.
(On CV183X: BOOT.bin→minerfs.gz, devicetree.dtb→dtb, uImage→kernel.)

## Merge BMU
Magic `0xABABABAB` at offset 0. Header = 36 bytes (u32 fields, little-endian).

| Offset | Meaning |
| --- | --- |
| 0x00 | magic `0xABABABAB` |
| 0x04 | version |
| 0x08 | header size |
| 0x0C | item_count |
| 0x10 | item_size (172) |
| 0x14 | data_offset (16384) |
| 0x18 | crc32 (over header with the crc field zeroed) |
| 0x24 | items start (item_size bytes each) |

Item (172 bytes): `u8 filename_len, u8 chip_len, u8 hardware_len, u8 model_len, char filename[64], char chip[32], char hardware[32], char model[32], u32 offset, u32 size`.
Each item points to a single-BMU blob at `[offset, offset+size)`.

So a merge BMU enumerates several single BMUs; the pipeline recurses into each and lists their files.
