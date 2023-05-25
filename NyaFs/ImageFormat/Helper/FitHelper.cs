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
            switch(Fs)
            {
                case Types.FsType.Cpio: return "cpio";
                case Types.FsType.Ext2: return "ext2";
                case Types.FsType.SquashFs: return "squashfs";
                case Types.FsType.CramFs: return "cramfs";
                case Types.FsType.RomFs: return "romfs";
                case Types.FsType.Unknown:
                default:
                    return "unknown";
            }
        }

        public static Types.FsType GetFilesystemType(string Fs)
        {
            switch (Fs)
            {
                case "cpio": return Types.FsType.Cpio;
                case "ext2": return Types.FsType.Ext2;
                case "squashfs": return Types.FsType.SquashFs;
                case "cramfs": return Types.FsType.CramFs;
                case "romfs": return Types.FsType.RomFs;
                default: return Types.FsType.Unknown;
            }
        }

        public static Types.CompressionType DetectCompression(byte[] Raw)
        {
            var Header16 = Raw.ReadUInt16(0);
            switch (Header16)
            {
                case 0x5a42: return Types.CompressionType.IH_COMP_BZIP2;
                case 0x4C89: return Types.CompressionType.IH_COMP_LZO;
                case 0x8b1f: return Types.CompressionType.IH_COMP_GZIP;
                case 0x005d: return Types.CompressionType.IH_COMP_LZMA;
                case 0x2204: return Types.CompressionType.IH_COMP_LZ4;
                case 0xb528: return Types.CompressionType.IH_COMP_ZSTD;
                case 0xfd37: return Types.CompressionType.IH_COMP_XZ;
                default: return Types.CompressionType.IH_COMP_NONE;
            }
        }

        public static bool Is64BitArchitecture(Types.CPU Arch)
        {
            switch (Arch)
            {
                case Types.CPU.IH_ARCH_MIPS64:
                case Types.CPU.IH_ARCH_ARM64:
                case Types.CPU.IH_ARCH_IA64:
                case Types.CPU.IH_ARCH_SPARC64:
                case Types.CPU.IH_ARCH_X86_64:
                    return true;
                default: return false;
            }
        }

        public static string GetCPUArchitecture(Types.CPU Arch)
        {
            switch (Arch)
            {
                case Types.CPU.IH_ARCH_ALPHA: return "alpha";
                case Types.CPU.IH_ARCH_ARM: return "arm";
                case Types.CPU.IH_ARCH_ARM64: return "arm64";
                case Types.CPU.IH_ARCH_I386: return "x86";
                case Types.CPU.IH_ARCH_IA64: return "ia64";
                case Types.CPU.IH_ARCH_M68K: return "m68k";
                case Types.CPU.IH_ARCH_MICROBLAZE: return "microblaze";
                case Types.CPU.IH_ARCH_MIPS: return "mips";
                case Types.CPU.IH_ARCH_MIPS64: return "mips64";
                case Types.CPU.IH_ARCH_NIOS: return "nios";
                case Types.CPU.IH_ARCH_NIOS2: return "nios2";
                case Types.CPU.IH_ARCH_PPC: return "ppc";
                case Types.CPU.IH_ARCH_S390: return "s390";
                case Types.CPU.IH_ARCH_SH: return "sh";
                case Types.CPU.IH_ARCH_SPARC: return "sparc";
                case Types.CPU.IH_ARCH_SPARC64: return "sparc64";
                case Types.CPU.IH_ARCH_BLACKFIN: return "blackfin";
                case Types.CPU.IH_ARCH_AVR32: return "avr32";
                case Types.CPU.IH_ARCH_NDS32: return "nds32";
                case Types.CPU.IH_ARCH_OPENRISC: return "or1k";
                case Types.CPU.IH_ARCH_SANDBOX: return "sandbox";
                case Types.CPU.IH_ARCH_ARC: return "arc";
                case Types.CPU.IH_ARCH_X86_64: return "x86_64";
                case Types.CPU.IH_ARCH_XTENSA: return "xtensa";
                case Types.CPU.IH_ARCH_RISCV: return "riscv";
                default: return "invalid";
            }
        }

        public static Types.CPU GetCPUArchitecture(string Arch)
        {
            switch (Arch)
            {
                case "alpha": return Types.CPU.IH_ARCH_ALPHA;
                case "arm": return Types.CPU.IH_ARCH_ARM;
                case "arm64": return Types.CPU.IH_ARCH_ARM64;
                case "x86": return Types.CPU.IH_ARCH_I386;
                case "ia64": return Types.CPU.IH_ARCH_IA64;
                case "m68k": return Types.CPU.IH_ARCH_M68K;
                case "microblaze": return Types.CPU.IH_ARCH_MICROBLAZE;
                case "mips": return Types.CPU.IH_ARCH_MIPS;
                case "mips64": return Types.CPU.IH_ARCH_MIPS64;
                case "nios": return Types.CPU.IH_ARCH_NIOS;
                case "nios2": return Types.CPU.IH_ARCH_NIOS2;
                case "powerpc":
                case "ppc": return Types.CPU.IH_ARCH_PPC;
                case "s390": return Types.CPU.IH_ARCH_S390;
                case "sh": return Types.CPU.IH_ARCH_SH;
                case "sparc": return Types.CPU.IH_ARCH_SPARC;
                case "sparc64": return Types.CPU.IH_ARCH_SPARC64;
                case "blackfin": return Types.CPU.IH_ARCH_BLACKFIN;
                case "avr32": return Types.CPU.IH_ARCH_AVR32;
                case "nds32": return Types.CPU.IH_ARCH_NDS32;
                case "or1k": return Types.CPU.IH_ARCH_OPENRISC;
                case "sandbox": return Types.CPU.IH_ARCH_SANDBOX;
                case "arc": return Types.CPU.IH_ARCH_ARC;
                case "x86_64": return Types.CPU.IH_ARCH_X86_64;
                case "xtensa": return Types.CPU.IH_ARCH_XTENSA;
                case "riscv": return Types.CPU.IH_ARCH_RISCV;

                default: return Types.CPU.IH_ARCH_INVALID;
            }
        }

        public static string GetOperatingSystem(Types.OS Os)
        {
            switch (Os)
            {
                case Types.OS.IH_OS_INVALID: return "invalid";
                case Types.OS.IH_OS_LINUX: return "linux";
                case Types.OS.IH_OS_LYNXOS: return "lynxos";
                case Types.OS.IH_OS_NETBSD: return "netbsd";
                case Types.OS.IH_OS_OSE: return "ose"; // ENEA OSE RTOS
                case Types.OS.IH_OS_PLAN9: return "plan9";
                case Types.OS.IH_OS_RTEMS: return "rtems";
                case Types.OS.IH_OS_TEE: return "tee";
                case Types.OS.IH_OS_U_BOOT: return "u-boot";
                case Types.OS.IH_OS_VXWORKS: return "vxworks";
                case Types.OS.IH_OS_QNX: return "qnx";
                case Types.OS.IH_OS_INTEGRITY: return "integrity";
                case Types.OS.IH_OS_4_4BSD: return "4_4bsd";
                case Types.OS.IH_OS_DELL: return "dell";
                case Types.OS.IH_OS_ESIX: return "esix";
                case Types.OS.IH_OS_FREEBSD: return "freebsd";
                case Types.OS.IH_OS_IRIX: return "irix";
                case Types.OS.IH_OS_NCR: return "ncr";
                case Types.OS.IH_OS_OPENBSD: return "openbsd";
                case Types.OS.IH_OS_PSOS: return "psos";
                case Types.OS.IH_OS_SCO: return "sco";
                case Types.OS.IH_OS_SOLARIS: return "solaris";
                case Types.OS.IH_OS_SVR4: return "svr4";
                case Types.OS.IH_OS_OPENRTOS: return "openrtos";
                case Types.OS.IH_OS_OPENSBI: return "opensbi";
                case Types.OS.IH_OS_EFI: return "efi";
                //case Types.OS.IH_OS_ARTOS: return "artos";
                //case Types.OS.IH_OS_UNITY: return "unity";
                //case Types.OS.IH_OS_ARM_TRUSTED_FIRMWARE: return "armtrusted";
                default: return "unknown";
            }
        }

        public static Types.OS GetOperatingSystem(string Os)
        {
            switch (Os)
            {
                case "linux": return ImageFormat.Types.OS.IH_OS_LINUX;
                case "lynxos": return ImageFormat.Types.OS.IH_OS_LYNXOS;
                case "netbsd": return ImageFormat.Types.OS.IH_OS_NETBSD;
                case "ose": return ImageFormat.Types.OS.IH_OS_OSE; // ENEA OSE RTOS
                case "plan9": return ImageFormat.Types.OS.IH_OS_PLAN9;
                case "rtems": return ImageFormat.Types.OS.IH_OS_RTEMS;
                case "tee": return ImageFormat.Types.OS.IH_OS_TEE;
                case "u-boot": return ImageFormat.Types.OS.IH_OS_U_BOOT;
                case "vxworks": return ImageFormat.Types.OS.IH_OS_VXWORKS;
                case "qnx": return ImageFormat.Types.OS.IH_OS_QNX;
                case "integrity": return ImageFormat.Types.OS.IH_OS_INTEGRITY;
                case "4_4bsd": return ImageFormat.Types.OS.IH_OS_4_4BSD;
                case "dell": return ImageFormat.Types.OS.IH_OS_DELL;
                case "esix": return ImageFormat.Types.OS.IH_OS_ESIX;
                case "freebsd": return ImageFormat.Types.OS.IH_OS_FREEBSD;
                case "irix": return ImageFormat.Types.OS.IH_OS_IRIX;
                case "ncr": return ImageFormat.Types.OS.IH_OS_NCR;
                case "openbsd": return ImageFormat.Types.OS.IH_OS_OPENBSD;
                case "psos": return ImageFormat.Types.OS.IH_OS_PSOS;
                case "sco": return ImageFormat.Types.OS.IH_OS_SCO;
                case "solaris": return ImageFormat.Types.OS.IH_OS_SOLARIS;
                case "svr4": return ImageFormat.Types.OS.IH_OS_SVR4;
                case "openrtos": return ImageFormat.Types.OS.IH_OS_OPENRTOS;
                case "opensbi": return ImageFormat.Types.OS.IH_OS_OPENSBI;
                case "efi": return ImageFormat.Types.OS.IH_OS_EFI;
                //case "artos": return ImageFormat.Types.OS.IH_OS_ARTOS;
                //case "unity": return ImageFormat.Types.OS.IH_OS_UNITY;
                //case "armtrusted": return ImageFormat.Types.OS.IH_OS_ARM_TRUSTED_FIRMWARE;
                default: return Types.OS.IH_OS_INVALID;
            }
        }

        public static string GetType(Types.ImageType Type)
        {
            switch (Type)
            {
                case Types.ImageType.IH_TYPE_KERNEL: return "kernel";
                case Types.ImageType.IH_TYPE_FLATDT: return "flat_dt";
                case Types.ImageType.IH_TYPE_RAMDISK: return "ramdisk";
                case Types.ImageType.IH_TYPE_AISIMAGE: return "aisimage";
                case Types.ImageType.IH_TYPE_FILESYSTEM: return "filesystem";
                case Types.ImageType.IH_TYPE_FIRMWARE: return "firmware";
                case Types.ImageType.IH_TYPE_GPIMAGE: return "gpimage";
                case Types.ImageType.IH_TYPE_KERNEL_NOLOAD: return "kernel_noload";
                case Types.ImageType.IH_TYPE_KWBIMAGE: return "kwbimage";
                case Types.ImageType.IH_TYPE_IMXIMAGE: return "imximage";
                case Types.ImageType.IH_TYPE_IMX8IMAGE: return "imx8image";
                case Types.ImageType.IH_TYPE_IMX8MIMAGE: return "imx8mimage";
                case Types.ImageType.IH_TYPE_MULTI: return "multi";
                case Types.ImageType.IH_TYPE_OMAPIMAGE: return "omapimage";
                case Types.ImageType.IH_TYPE_PBLIMAGE: return "pblimage";
                case Types.ImageType.IH_TYPE_SCRIPT: return "script";
                case Types.ImageType.IH_TYPE_SOCFPGAIMAGE: return "socfpgaimage";
                case Types.ImageType.IH_TYPE_SOCFPGAIMAGE_V1: return "socfpgaimage_v1";
                case Types.ImageType.IH_TYPE_STANDALONE: return "standalone";
                case Types.ImageType.IH_TYPE_UBLIMAGE: return "ublimage";
                case Types.ImageType.IH_TYPE_MXSIMAGE: return "mxsimage";
                case Types.ImageType.IH_TYPE_ATMELIMAGE: return "atmelimage";
                case Types.ImageType.IH_TYPE_X86_SETUP: return "x86_setup";
                case Types.ImageType.IH_TYPE_LPC32XXIMAGE: return "lpc32xximage";
                case Types.ImageType.IH_TYPE_RKIMAGE: return "rkimage";
                case Types.ImageType.IH_TYPE_RKSD: return "rksd";
                case Types.ImageType.IH_TYPE_RKSPI: return "rkspi";
                case Types.ImageType.IH_TYPE_VYBRIDIMAGE: return "vybridimage";
                case Types.ImageType.IH_TYPE_ZYNQIMAGE: return "zynqimage";
                case Types.ImageType.IH_TYPE_ZYNQMPIMAGE: return "zynqmpimage";
                case Types.ImageType.IH_TYPE_ZYNQMPBIF: return "zynqmpbif";
                case Types.ImageType.IH_TYPE_FPGA: return "fpga";
                case Types.ImageType.IH_TYPE_TEE: return "tee";
                case Types.ImageType.IH_TYPE_FIRMWARE_IVT: return "firmware_ivt";
                case Types.ImageType.IH_TYPE_PMMC: return "pmmc";
                case Types.ImageType.IH_TYPE_STM32IMAGE: return "stm32image";
                case Types.ImageType.IH_TYPE_MTKIMAGE: return "mtk_image";
                case Types.ImageType.IH_TYPE_COPRO: return "copro";
                case Types.ImageType.IH_TYPE_SUNXI_EGON: return "sunxi_egon";
                case Types.ImageType.IH_TYPE_INVALID: return "invalid";
                default: return "unknown";
            }
        }

        public static ImageFormat.Types.ImageType GetType(string Type)
        {
            switch (Type)
            {
                case "kernel": return ImageFormat.Types.ImageType.IH_TYPE_KERNEL;
                case "flat_dt": return ImageFormat.Types.ImageType.IH_TYPE_FLATDT;
                case "ramdisk": return ImageFormat.Types.ImageType.IH_TYPE_RAMDISK;
                case "aisimage": return ImageFormat.Types.ImageType.IH_TYPE_AISIMAGE;
                case "filesystem": return ImageFormat.Types.ImageType.IH_TYPE_FILESYSTEM;
                case "firmware": return ImageFormat.Types.ImageType.IH_TYPE_FIRMWARE;
                case "gpimage": return ImageFormat.Types.ImageType.IH_TYPE_GPIMAGE;
                case "kernel_noload": return ImageFormat.Types.ImageType.IH_TYPE_KERNEL_NOLOAD;
                case "kwbimage": return ImageFormat.Types.ImageType.IH_TYPE_KWBIMAGE;
                case "imximage": return ImageFormat.Types.ImageType.IH_TYPE_IMXIMAGE;
                case "imx8image": return ImageFormat.Types.ImageType.IH_TYPE_IMX8IMAGE;
                case "imx8mimage": return ImageFormat.Types.ImageType.IH_TYPE_IMX8MIMAGE;
                case "multi": return ImageFormat.Types.ImageType.IH_TYPE_MULTI;
                case "omapimage": return ImageFormat.Types.ImageType.IH_TYPE_OMAPIMAGE;
                case "pblimage": return ImageFormat.Types.ImageType.IH_TYPE_PBLIMAGE;
                case "script": return ImageFormat.Types.ImageType.IH_TYPE_SCRIPT;
                case "socfpgaimage": return ImageFormat.Types.ImageType.IH_TYPE_SOCFPGAIMAGE;
                case "socfpgaimage_v1": return ImageFormat.Types.ImageType.IH_TYPE_SOCFPGAIMAGE_V1;
                case "standalone": return ImageFormat.Types.ImageType.IH_TYPE_STANDALONE;
                case "ublimage": return ImageFormat.Types.ImageType.IH_TYPE_UBLIMAGE;
                case "mxsimage": return ImageFormat.Types.ImageType.IH_TYPE_MXSIMAGE;
                case "atmelimage": return ImageFormat.Types.ImageType.IH_TYPE_ATMELIMAGE;
                case "x86_setup": return ImageFormat.Types.ImageType.IH_TYPE_X86_SETUP;
                case "lpc32xximage": return ImageFormat.Types.ImageType.IH_TYPE_LPC32XXIMAGE;
                case "rkimage": return ImageFormat.Types.ImageType.IH_TYPE_RKIMAGE;
                case "rksd": return ImageFormat.Types.ImageType.IH_TYPE_RKSD;
                case "rkspi": return ImageFormat.Types.ImageType.IH_TYPE_RKSPI;
                case "vybridimage": return ImageFormat.Types.ImageType.IH_TYPE_VYBRIDIMAGE;
                case "zynqimage": return ImageFormat.Types.ImageType.IH_TYPE_ZYNQIMAGE;
                case "zynqmpimage": return ImageFormat.Types.ImageType.IH_TYPE_ZYNQMPIMAGE;
                case "zynqmpbif": return ImageFormat.Types.ImageType.IH_TYPE_ZYNQMPBIF;
                case "fpga": return ImageFormat.Types.ImageType.IH_TYPE_FPGA;
                case "tee": return ImageFormat.Types.ImageType.IH_TYPE_TEE;
                case "firmware_ivt": return ImageFormat.Types.ImageType.IH_TYPE_FIRMWARE_IVT;
                case "pmmc": return ImageFormat.Types.ImageType.IH_TYPE_PMMC;
                case "stm32image": return ImageFormat.Types.ImageType.IH_TYPE_STM32IMAGE;
                case "mtk_image": return ImageFormat.Types.ImageType.IH_TYPE_MTKIMAGE;
                case "copro": return ImageFormat.Types.ImageType.IH_TYPE_COPRO;
                case "sunxi_egon": return ImageFormat.Types.ImageType.IH_TYPE_SUNXI_EGON;
                default: return ImageFormat.Types.ImageType.IH_TYPE_INVALID;
            }
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
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                return sha1.ComputeHash(Data);
            }
        }

        public static byte[] CalcSHA256Hash(byte[] Data)
        {
            using (SHA256Managed sha256 = new SHA256Managed())
            {
                return sha256.ComputeHash(Data);
            }
        }
    }
}