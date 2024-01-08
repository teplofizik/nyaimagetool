using Extension.Array;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NyaFs.ImageFormat.Helper
{
    public static class FitHelper
    {
        public static FlattenedDeviceTree.Types.Node GetHashNode(FlattenedDeviceTree.Types.Node Node)
        {
            foreach (var N in Node.Nodes)
            {
                if (N.Name.StartsWith("hash@"))
                    return N;
            }

            return null;
        }

        public static string GetFilesystemType(Types.FsType Fs)
        {
            return Fs switch
            {
                Types.FsType.Cpio => "cpio",
                Types.FsType.Ext2 => "ext2",
                Types.FsType.SquashFs => "squashfs",
                Types.FsType.CramFs => "cramfs",
                Types.FsType.RomFs => "romfs",
                _ => "unknown",
            };
        }

        public static Types.FsType GetFilesystemType(string Fs)
        {
            return Fs switch
            {
                "cpio" => Types.FsType.Cpio,
                "ext2" => Types.FsType.Ext2,
                "squashfs" => Types.FsType.SquashFs,
                "cramfs" => Types.FsType.CramFs,
                "romfs" => Types.FsType.RomFs,
                _ => Types.FsType.Unknown,
            };
        }

        public static Types.CompressionType DetectCompression(byte[] Raw)
        {
            var Header16 = Raw.ReadUInt16(0);
            return Header16 switch
            {
                0x5a42 => Types.CompressionType.IH_COMP_BZIP2,
                0x4C89 => Types.CompressionType.IH_COMP_LZO,
                0x8b1f => Types.CompressionType.IH_COMP_GZIP,
                0x005d => Types.CompressionType.IH_COMP_LZMA,
                0x2204 => Types.CompressionType.IH_COMP_LZ4,
                0xb528 => Types.CompressionType.IH_COMP_ZSTD,
                0xfd37 => Types.CompressionType.IH_COMP_XZ,
                _ => Types.CompressionType.IH_COMP_NONE,
            };
        }

        public static bool Is64BitArchitecture(Types.CPU Arch)
        {
            return Arch switch
            {
                Types.CPU.IH_ARCH_MIPS64 or Types.CPU.IH_ARCH_ARM64 or Types.CPU.IH_ARCH_IA64 or Types.CPU.IH_ARCH_SPARC64 or Types.CPU.IH_ARCH_X86_64 => true,
                _ => false,
            };
        }

        public static string GetCPUArchitecture(Types.CPU Arch)
        {
            return Arch switch
            {
                Types.CPU.IH_ARCH_ALPHA => "alpha",
                Types.CPU.IH_ARCH_ARM => "arm",
                Types.CPU.IH_ARCH_ARM64 => "arm64",
                Types.CPU.IH_ARCH_I386 => "x86",
                Types.CPU.IH_ARCH_IA64 => "ia64",
                Types.CPU.IH_ARCH_M68K => "m68k",
                Types.CPU.IH_ARCH_MICROBLAZE => "microblaze",
                Types.CPU.IH_ARCH_MIPS => "mips",
                Types.CPU.IH_ARCH_MIPS64 => "mips64",
                Types.CPU.IH_ARCH_NIOS => "nios",
                Types.CPU.IH_ARCH_NIOS2 => "nios2",
                Types.CPU.IH_ARCH_PPC => "ppc",
                Types.CPU.IH_ARCH_S390 => "s390",
                Types.CPU.IH_ARCH_SH => "sh",
                Types.CPU.IH_ARCH_SPARC => "sparc",
                Types.CPU.IH_ARCH_SPARC64 => "sparc64",
                Types.CPU.IH_ARCH_BLACKFIN => "blackfin",
                Types.CPU.IH_ARCH_AVR32 => "avr32",
                Types.CPU.IH_ARCH_NDS32 => "nds32",
                Types.CPU.IH_ARCH_OPENRISC => "or1k",
                Types.CPU.IH_ARCH_SANDBOX => "sandbox",
                Types.CPU.IH_ARCH_ARC => "arc",
                Types.CPU.IH_ARCH_X86_64 => "x86_64",
                Types.CPU.IH_ARCH_XTENSA => "xtensa",
                Types.CPU.IH_ARCH_RISCV => "riscv",
                _ => "invalid",
            };
        }

        public static Types.CPU GetCPUArchitecture(string Arch)
        {
            return Arch switch
            {
                "alpha" => Types.CPU.IH_ARCH_ALPHA,
                "arm" => Types.CPU.IH_ARCH_ARM,
                "arm64" => Types.CPU.IH_ARCH_ARM64,
                "x86" => Types.CPU.IH_ARCH_I386,
                "ia64" => Types.CPU.IH_ARCH_IA64,
                "m68k" => Types.CPU.IH_ARCH_M68K,
                "microblaze" => Types.CPU.IH_ARCH_MICROBLAZE,
                "mips" => Types.CPU.IH_ARCH_MIPS,
                "mips64" => Types.CPU.IH_ARCH_MIPS64,
                "nios" => Types.CPU.IH_ARCH_NIOS,
                "nios2" => Types.CPU.IH_ARCH_NIOS2,
                "powerpc" or "ppc" => Types.CPU.IH_ARCH_PPC,
                "s390" => Types.CPU.IH_ARCH_S390,
                "sh" => Types.CPU.IH_ARCH_SH,
                "sparc" => Types.CPU.IH_ARCH_SPARC,
                "sparc64" => Types.CPU.IH_ARCH_SPARC64,
                "blackfin" => Types.CPU.IH_ARCH_BLACKFIN,
                "avr32" => Types.CPU.IH_ARCH_AVR32,
                "nds32" => Types.CPU.IH_ARCH_NDS32,
                "or1k" => Types.CPU.IH_ARCH_OPENRISC,
                "sandbox" => Types.CPU.IH_ARCH_SANDBOX,
                "arc" => Types.CPU.IH_ARCH_ARC,
                "x86_64" => Types.CPU.IH_ARCH_X86_64,
                "xtensa" => Types.CPU.IH_ARCH_XTENSA,
                "riscv" => Types.CPU.IH_ARCH_RISCV,
                _ => Types.CPU.IH_ARCH_INVALID,
            };
        }

        public static string GetOperatingSystem(Types.OS Os)
        {
            return Os switch
            {
                Types.OS.IH_OS_INVALID => "invalid",
                Types.OS.IH_OS_LINUX => "linux",
                Types.OS.IH_OS_LYNXOS => "lynxos",
                Types.OS.IH_OS_NETBSD => "netbsd",
                Types.OS.IH_OS_OSE => "ose",// ENEA OSE RTOS
                Types.OS.IH_OS_PLAN9 => "plan9",
                Types.OS.IH_OS_RTEMS => "rtems",
                Types.OS.IH_OS_TEE => "tee",
                Types.OS.IH_OS_U_BOOT => "u-boot",
                Types.OS.IH_OS_VXWORKS => "vxworks",
                Types.OS.IH_OS_QNX => "qnx",
                Types.OS.IH_OS_INTEGRITY => "integrity",
                Types.OS.IH_OS_4_4BSD => "4_4bsd",
                Types.OS.IH_OS_DELL => "dell",
                Types.OS.IH_OS_ESIX => "esix",
                Types.OS.IH_OS_FREEBSD => "freebsd",
                Types.OS.IH_OS_IRIX => "irix",
                Types.OS.IH_OS_NCR => "ncr",
                Types.OS.IH_OS_OPENBSD => "openbsd",
                Types.OS.IH_OS_PSOS => "psos",
                Types.OS.IH_OS_SCO => "sco",
                Types.OS.IH_OS_SOLARIS => "solaris",
                Types.OS.IH_OS_SVR4 => "svr4",
                Types.OS.IH_OS_OPENRTOS => "openrtos",
                Types.OS.IH_OS_OPENSBI => "opensbi",
                Types.OS.IH_OS_EFI => "efi",
                //case Types.OS.IH_OS_ARTOS: return "artos";
                //case Types.OS.IH_OS_UNITY: return "unity";
                //case Types.OS.IH_OS_ARM_TRUSTED_FIRMWARE: return "armtrusted";
                _ => "unknown",
            };
        }

        public static Types.OS GetOperatingSystem(string Os)
        {
            return Os switch
            {
                "linux" => ImageFormat.Types.OS.IH_OS_LINUX,
                "lynxos" => ImageFormat.Types.OS.IH_OS_LYNXOS,
                "netbsd" => ImageFormat.Types.OS.IH_OS_NETBSD,
                "ose" => ImageFormat.Types.OS.IH_OS_OSE,// ENEA OSE RTOS
                "plan9" => ImageFormat.Types.OS.IH_OS_PLAN9,
                "rtems" => ImageFormat.Types.OS.IH_OS_RTEMS,
                "tee" => ImageFormat.Types.OS.IH_OS_TEE,
                "u-boot" => ImageFormat.Types.OS.IH_OS_U_BOOT,
                "vxworks" => ImageFormat.Types.OS.IH_OS_VXWORKS,
                "qnx" => ImageFormat.Types.OS.IH_OS_QNX,
                "integrity" => ImageFormat.Types.OS.IH_OS_INTEGRITY,
                "4_4bsd" => ImageFormat.Types.OS.IH_OS_4_4BSD,
                "dell" => ImageFormat.Types.OS.IH_OS_DELL,
                "esix" => ImageFormat.Types.OS.IH_OS_ESIX,
                "freebsd" => ImageFormat.Types.OS.IH_OS_FREEBSD,
                "irix" => ImageFormat.Types.OS.IH_OS_IRIX,
                "ncr" => ImageFormat.Types.OS.IH_OS_NCR,
                "openbsd" => ImageFormat.Types.OS.IH_OS_OPENBSD,
                "psos" => ImageFormat.Types.OS.IH_OS_PSOS,
                "sco" => ImageFormat.Types.OS.IH_OS_SCO,
                "solaris" => ImageFormat.Types.OS.IH_OS_SOLARIS,
                "svr4" => ImageFormat.Types.OS.IH_OS_SVR4,
                "openrtos" => ImageFormat.Types.OS.IH_OS_OPENRTOS,
                "opensbi" => ImageFormat.Types.OS.IH_OS_OPENSBI,
                "efi" => ImageFormat.Types.OS.IH_OS_EFI,
                //case "artos": return ImageFormat.Types.OS.IH_OS_ARTOS;
                //case "unity": return ImageFormat.Types.OS.IH_OS_UNITY;
                //case "armtrusted": return ImageFormat.Types.OS.IH_OS_ARM_TRUSTED_FIRMWARE;
                _ => Types.OS.IH_OS_INVALID,
            };
        }

        public static string GetType(Types.ImageType Type)
        {
            return Type switch
            {
                Types.ImageType.IH_TYPE_KERNEL => "kernel",
                Types.ImageType.IH_TYPE_FLATDT => "flat_dt",
                Types.ImageType.IH_TYPE_RAMDISK => "ramdisk",
                Types.ImageType.IH_TYPE_AISIMAGE => "aisimage",
                Types.ImageType.IH_TYPE_FILESYSTEM => "filesystem",
                Types.ImageType.IH_TYPE_FIRMWARE => "firmware",
                Types.ImageType.IH_TYPE_GPIMAGE => "gpimage",
                Types.ImageType.IH_TYPE_KERNEL_NOLOAD => "kernel_noload",
                Types.ImageType.IH_TYPE_KWBIMAGE => "kwbimage",
                Types.ImageType.IH_TYPE_IMXIMAGE => "imximage",
                Types.ImageType.IH_TYPE_IMX8IMAGE => "imx8image",
                Types.ImageType.IH_TYPE_IMX8MIMAGE => "imx8mimage",
                Types.ImageType.IH_TYPE_MULTI => "multi",
                Types.ImageType.IH_TYPE_OMAPIMAGE => "omapimage",
                Types.ImageType.IH_TYPE_PBLIMAGE => "pblimage",
                Types.ImageType.IH_TYPE_SCRIPT => "script",
                Types.ImageType.IH_TYPE_SOCFPGAIMAGE => "socfpgaimage",
                Types.ImageType.IH_TYPE_SOCFPGAIMAGE_V1 => "socfpgaimage_v1",
                Types.ImageType.IH_TYPE_STANDALONE => "standalone",
                Types.ImageType.IH_TYPE_UBLIMAGE => "ublimage",
                Types.ImageType.IH_TYPE_MXSIMAGE => "mxsimage",
                Types.ImageType.IH_TYPE_ATMELIMAGE => "atmelimage",
                Types.ImageType.IH_TYPE_X86_SETUP => "x86_setup",
                Types.ImageType.IH_TYPE_LPC32XXIMAGE => "lpc32xximage",
                Types.ImageType.IH_TYPE_RKIMAGE => "rkimage",
                Types.ImageType.IH_TYPE_RKSD => "rksd",
                Types.ImageType.IH_TYPE_RKSPI => "rkspi",
                Types.ImageType.IH_TYPE_VYBRIDIMAGE => "vybridimage",
                Types.ImageType.IH_TYPE_ZYNQIMAGE => "zynqimage",
                Types.ImageType.IH_TYPE_ZYNQMPIMAGE => "zynqmpimage",
                Types.ImageType.IH_TYPE_ZYNQMPBIF => "zynqmpbif",
                Types.ImageType.IH_TYPE_FPGA => "fpga",
                Types.ImageType.IH_TYPE_TEE => "tee",
                Types.ImageType.IH_TYPE_FIRMWARE_IVT => "firmware_ivt",
                Types.ImageType.IH_TYPE_PMMC => "pmmc",
                Types.ImageType.IH_TYPE_STM32IMAGE => "stm32image",
                Types.ImageType.IH_TYPE_MTKIMAGE => "mtk_image",
                Types.ImageType.IH_TYPE_COPRO => "copro",
                Types.ImageType.IH_TYPE_SUNXI_EGON => "sunxi_egon",
                Types.ImageType.IH_TYPE_INVALID => "invalid",
                _ => "unknown",
            };
        }

        public static ImageFormat.Types.ImageType GetType(string Type)
        {
            return Type switch
            {
                "kernel" => ImageFormat.Types.ImageType.IH_TYPE_KERNEL,
                "flat_dt" => ImageFormat.Types.ImageType.IH_TYPE_FLATDT,
                "ramdisk" => ImageFormat.Types.ImageType.IH_TYPE_RAMDISK,
                "aisimage" => ImageFormat.Types.ImageType.IH_TYPE_AISIMAGE,
                "filesystem" => ImageFormat.Types.ImageType.IH_TYPE_FILESYSTEM,
                "firmware" => ImageFormat.Types.ImageType.IH_TYPE_FIRMWARE,
                "gpimage" => ImageFormat.Types.ImageType.IH_TYPE_GPIMAGE,
                "kernel_noload" => ImageFormat.Types.ImageType.IH_TYPE_KERNEL_NOLOAD,
                "kwbimage" => ImageFormat.Types.ImageType.IH_TYPE_KWBIMAGE,
                "imximage" => ImageFormat.Types.ImageType.IH_TYPE_IMXIMAGE,
                "imx8image" => ImageFormat.Types.ImageType.IH_TYPE_IMX8IMAGE,
                "imx8mimage" => ImageFormat.Types.ImageType.IH_TYPE_IMX8MIMAGE,
                "multi" => ImageFormat.Types.ImageType.IH_TYPE_MULTI,
                "omapimage" => ImageFormat.Types.ImageType.IH_TYPE_OMAPIMAGE,
                "pblimage" => ImageFormat.Types.ImageType.IH_TYPE_PBLIMAGE,
                "script" => ImageFormat.Types.ImageType.IH_TYPE_SCRIPT,
                "socfpgaimage" => ImageFormat.Types.ImageType.IH_TYPE_SOCFPGAIMAGE,
                "socfpgaimage_v1" => ImageFormat.Types.ImageType.IH_TYPE_SOCFPGAIMAGE_V1,
                "standalone" => ImageFormat.Types.ImageType.IH_TYPE_STANDALONE,
                "ublimage" => ImageFormat.Types.ImageType.IH_TYPE_UBLIMAGE,
                "mxsimage" => ImageFormat.Types.ImageType.IH_TYPE_MXSIMAGE,
                "atmelimage" => ImageFormat.Types.ImageType.IH_TYPE_ATMELIMAGE,
                "x86_setup" => ImageFormat.Types.ImageType.IH_TYPE_X86_SETUP,
                "lpc32xximage" => ImageFormat.Types.ImageType.IH_TYPE_LPC32XXIMAGE,
                "rkimage" => ImageFormat.Types.ImageType.IH_TYPE_RKIMAGE,
                "rksd" => ImageFormat.Types.ImageType.IH_TYPE_RKSD,
                "rkspi" => ImageFormat.Types.ImageType.IH_TYPE_RKSPI,
                "vybridimage" => ImageFormat.Types.ImageType.IH_TYPE_VYBRIDIMAGE,
                "zynqimage" => ImageFormat.Types.ImageType.IH_TYPE_ZYNQIMAGE,
                "zynqmpimage" => ImageFormat.Types.ImageType.IH_TYPE_ZYNQMPIMAGE,
                "zynqmpbif" => ImageFormat.Types.ImageType.IH_TYPE_ZYNQMPBIF,
                "fpga" => ImageFormat.Types.ImageType.IH_TYPE_FPGA,
                "tee" => ImageFormat.Types.ImageType.IH_TYPE_TEE,
                "firmware_ivt" => ImageFormat.Types.ImageType.IH_TYPE_FIRMWARE_IVT,
                "pmmc" => ImageFormat.Types.ImageType.IH_TYPE_PMMC,
                "stm32image" => ImageFormat.Types.ImageType.IH_TYPE_STM32IMAGE,
                "mtk_image" => ImageFormat.Types.ImageType.IH_TYPE_MTKIMAGE,
                "copro" => ImageFormat.Types.ImageType.IH_TYPE_COPRO,
                "sunxi_egon" => ImageFormat.Types.ImageType.IH_TYPE_SUNXI_EGON,
                _ => ImageFormat.Types.ImageType.IH_TYPE_INVALID,
            };
        }

        public static string GetCompression(Filesystem.SquashFs.Types.SqCompressionType Compression)
        {
            switch (Compression)
            {
                case Filesystem.SquashFs.Types.SqCompressionType.Gzip: return "gzip";
                case Filesystem.SquashFs.Types.SqCompressionType.Lzma: return "lzma";
                case Filesystem.SquashFs.Types.SqCompressionType.Lz4: return "lz4";
                case Filesystem.SquashFs.Types.SqCompressionType.Xz: return "xz";
                case Filesystem.SquashFs.Types.SqCompressionType.Zstd: return "zstd";
                case Filesystem.SquashFs.Types.SqCompressionType.Lzo: return "lzo";
                default:
                    Log.Error(0, $"Unsupported compression type: {Compression}");
                    throw new ArgumentException($"Unsupported compression type: {Compression}");
            }
        }

        public static string GetCompression(Types.CompressionType Compression)
        {
            switch (Compression)
            {
                case Types.CompressionType.IH_COMP_NONE: return "none";
                case Types.CompressionType.IH_COMP_GZIP: return "gzip";
                case Types.CompressionType.IH_COMP_LZMA: return "lzma";
                case Types.CompressionType.IH_COMP_LZ4: return "lz4";
                case Types.CompressionType.IH_COMP_BZIP2: return "bzip2";
                case Types.CompressionType.IH_COMP_ZSTD: return "zstd";
                case Types.CompressionType.IH_COMP_LZO: return "lzo";
                case Types.CompressionType.IH_COMP_XZ: return "xz";
                default:
                    Log.Error(0, $"Unsupported compression type: {Compression}");
                    throw new ArgumentException($"Unsupported compression type: {Compression}");
            }
        }

        public static Types.CompressionType GetCompression(string Compression)
        {
            switch (Compression)
            {
                case "none": return Types.CompressionType.IH_COMP_NONE;
                case "gzip": return Types.CompressionType.IH_COMP_GZIP;
                case "lzma": return Types.CompressionType.IH_COMP_LZMA;
                case "lz4": return Types.CompressionType.IH_COMP_LZ4;
                case "zstd": return Types.CompressionType.IH_COMP_ZSTD;
                case "bzip2": return Types.CompressionType.IH_COMP_BZIP2;
                case "lzo": return Types.CompressionType.IH_COMP_LZO;
                default:
                    Log.Error(0, $"Unsupported compression type: {Compression}");
                    throw new ArgumentException($"Unsupported compression type: {Compression}");
            }
        }

        public static byte[] GetDecompressedData(byte[] Source, string Compression) => GetDecompressedData(Source, GetCompression(Compression));

        public static byte[] GetDecompressedData(byte[] Source, Types.CompressionType Compression)
        {
            switch (Compression)
            {
                case Types.CompressionType.IH_COMP_GZIP: return Compressors.Gzip.Decompress(Source);
                case Types.CompressionType.IH_COMP_LZMA: return Compressors.Lzma.Decompress(Source);
                case Types.CompressionType.IH_COMP_LZ4: return Compressors.Lz4.Decompress(Source);
                case Types.CompressionType.IH_COMP_BZIP2: return Compressors.Bzip2.Decompress(Source);
                case Types.CompressionType.IH_COMP_ZSTD: return Compressors.ZStd.Decompress(Source);
                case Types.CompressionType.IH_COMP_LZO: return Compressors.LZO.Decompress(Source);
                case Types.CompressionType.IH_COMP_XZ: return Compressors.Xz.Decompress(Source);
                case Types.CompressionType.IH_COMP_NONE: return Source;
                default:
                    Log.Error(0, $"Unsupported compression type: {Compression}");
                    throw new ArgumentException($"Unsupported compression type: {Compression}");
            }
        }

        public static byte[] GetCompressedData(byte[] Source, Types.CompressionType Compression)
        {
            switch (Compression)
            {
                case Types.CompressionType.IH_COMP_NONE: return Source;
                case Types.CompressionType.IH_COMP_GZIP: return Compressors.Gzip.CompressWithHeader(Source);
                case Types.CompressionType.IH_COMP_LZMA: return Compressors.Lzma.CompressWithHeader(Source);
                case Types.CompressionType.IH_COMP_LZ4: return Compressors.Lz4.CompressWithHeader(Source);
                case Types.CompressionType.IH_COMP_BZIP2: return Compressors.Bzip2.CompressWithHeader(Source);
                case Types.CompressionType.IH_COMP_ZSTD: return Compressors.ZStd.CompressWithHeader(Source);
                default:
                    Log.Error(0, $"Unsupported compression type: {Compression}");
                    throw new ArgumentException($"Unsupported compression type: {Compression}");
            }
        }

        public static bool CheckHash(byte[] image, FlattenedDeviceTree.Types.Node Node)
        {
            var algo = Node.GetStringValue("algo");
            var value = Node.GetValue("value");
            switch(algo)
            {
                case "sha1":
                    {
                        byte[] calchash = CalcSHA1Hash(image);

                        for (int i = 0; i < calchash.Length; i++)
                            if (calchash[i] != value[i])
                                return false;

                        return true;
                    }
                case "sha256":
                    {
                        byte[] calchash = CalcSHA256Hash(image);

                        for (int i = 0; i < calchash.Length; i++)
                            if (calchash[i] != value[i])
                                return false;

                        return true;
                    }
                default:
                    Log.Write(0, $"Hash algo '{algo}' is not supported.");
                    return false;
            }
        }

        public static byte[] CalcSHA1Hash(byte[] Data)
        {
            using SHA1 sha1 = SHA1.Create();
            return sha1.ComputeHash(Data);
        }

        public static byte[] CalcSHA256Hash(byte[] Data)
        {
            using SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(Data);
        }
    }
}