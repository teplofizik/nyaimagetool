using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Helper
{
    static class KernelHelper
    {
        private static string GetStringFromOffset(byte[] Image, long Offset)
        {
            for (long i = Offset; i < Image.Length; i++)
            {
                if(Image[i] == 0)
                    return UTF8Encoding.UTF8.GetString(Image, Convert.ToInt32(Offset), Convert.ToInt32(i - Offset));
            }

            return null;
        }

        private static string[] SearchInImage(byte[] Image, string Part)
        {
            var Res = new List<string>();
            var BytePattern = UTF8Encoding.UTF8.GetBytes(Part);
            long Start = 0;
            long Count = 0;

            for(long i = 0; i < Image.Length; i++)
            {
                var Search = BytePattern[Count];
                if (Image[i] == Search)
                {
                    if (Count == 0) Start = i;
                    Count++;
                    if (Count == BytePattern.Length)
                    {
                        Res.Add(GetStringFromOffset(Image, Start));
                        Count = 0;
                    }
                }
                else
                    Count = 0;
            }
            return Res.ToArray();
        }

        private static ImageFormat.Types.CPU Detect(string Line)
        {
            if (Line.IndexOf("/arch/alpha/") == 0) return ImageFormat.Types.CPU.IH_ARCH_ALPHA;
            if (Line.IndexOf("/arch/arc/") == 0) return ImageFormat.Types.CPU.IH_ARCH_ARC;
            if (Line.IndexOf("/arch/arm/") == 0) return ImageFormat.Types.CPU.IH_ARCH_ARM;
            if (Line.IndexOf("/arch/arm64/") == 0) return ImageFormat.Types.CPU.IH_ARCH_ARM64;
            if (Line.IndexOf("/arch/ia64/") == 0) return ImageFormat.Types.CPU.IH_ARCH_IA64;
            if (Line.IndexOf("/arch/m68k/") == 0) return ImageFormat.Types.CPU.IH_ARCH_M68K;
            if (Line.IndexOf("/arch/microblaze/") == 0) return ImageFormat.Types.CPU.IH_ARCH_MICROBLAZE;
            if (Line.IndexOf("/arch/mips/") == 0) return ImageFormat.Types.CPU.IH_ARCH_MIPS;
            if (Line.IndexOf("/arch/nios/") == 0) return ImageFormat.Types.CPU.IH_ARCH_NIOS2;
            if (Line.IndexOf("/arch/openrisc/") == 0) return ImageFormat.Types.CPU.IH_ARCH_OPENRISC;
            if (Line.IndexOf("/arch/powerpc/") == 0) return ImageFormat.Types.CPU.IH_ARCH_PPC;
            if (Line.IndexOf("/arch/riscv/") == 0) return ImageFormat.Types.CPU.IH_ARCH_RISCV;
            if (Line.IndexOf("/arch/s390/") == 0) return ImageFormat.Types.CPU.IH_ARCH_S390;
            if (Line.IndexOf("/arch/sh/") == 0) return ImageFormat.Types.CPU.IH_ARCH_SH;
            if (Line.IndexOf("/arch/sparc/") == 0) return ImageFormat.Types.CPU.IH_ARCH_SPARC;
            if (Line.IndexOf("/arch/x86/") == 0) return ImageFormat.Types.CPU.IH_ARCH_I386;
            if (Line.IndexOf("/arch/xtensa/") == 0) return ImageFormat.Types.CPU.IH_ARCH_XTENSA;

            return ImageFormat.Types.CPU.IH_ARCH_INVALID;
        }
        

        private static ImageFormat.Types.CPU GetElfInfo(byte[] Image)
        {
            var Machine = Image.ReadByte(0x12);
            switch(Machine)
            {
                case 0x29: return ImageFormat.Types.CPU.IH_ARCH_ALPHA; // Digital Alpha
                case 0x2D: return ImageFormat.Types.CPU.IH_ARCH_ARC; // Argonaut RISC Core
                case 0x28: return ImageFormat.Types.CPU.IH_ARCH_ARM; // ARM (up to ARMv7/Aarch32)
                case 0xB7: return ImageFormat.Types.CPU.IH_ARCH_ARM64; // ARM 64-bits (ARMv8/Aarch64)
                case 0x32: return ImageFormat.Types.CPU.IH_ARCH_IA64; // IA-64
                case 0x04: return ImageFormat.Types.CPU.IH_ARCH_M68K; // Motorola 68000 (M68k)
                case 0x3E: return ImageFormat.Types.CPU.IH_ARCH_X86_64; // AMD x86-64
                case 0x08: return ImageFormat.Types.CPU.IH_ARCH_MIPS; // 	MIPS
                case 0x14: return ImageFormat.Types.CPU.IH_ARCH_PPC; // PowerPC
                case 0xF3: return ImageFormat.Types.CPU.IH_ARCH_RISCV; // RISC-V
                case 0x16: return ImageFormat.Types.CPU.IH_ARCH_S390; // S390, including S390x
                case 0x2A: return ImageFormat.Types.CPU.IH_ARCH_SH; // SuperH
                case 0x02: return ImageFormat.Types.CPU.IH_ARCH_SPARC; // SPARC
                case 0x03: return ImageFormat.Types.CPU.IH_ARCH_I386; // Intel 80386
                default: return ImageFormat.Types.CPU.IH_ARCH_INVALID;
            }
        }

        public static ImageFormat.Types.CPU DetectImageArch(byte[] Image)
        {
            var Arch = ImageFormat.Types.CPU.IH_ARCH_INVALID;
            var Magic = Image.ReadUInt32(0);
            if (Magic == 0x464C457F)
            {
                var ElfArch = GetElfInfo(Image);
                if (ElfArch != ImageFormat.Types.CPU.IH_ARCH_INVALID)
                    return ElfArch;
            }

            var SearchSources = SearchInImage(Image, "/arch/");

            foreach (var SS in SearchSources)
            {
                var Detected = Detect(SS);
                if(Detected != ImageFormat.Types.CPU.IH_ARCH_INVALID)
                {
                    if (Arch != ImageFormat.Types.CPU.IH_ARCH_INVALID)
                    {
                        if (Detected != Arch)
                            return ImageFormat.Types.CPU.IH_ARCH_INVALID;
                    }
                    else
                        Arch = Detected;
                }
            }

            if (SearchInImage(Image, "arm64").Length > 0) return ImageFormat.Types.CPU.IH_ARCH_ARM64;
            if (SearchInImage(Image, "arm32").Length > 0) return ImageFormat.Types.CPU.IH_ARCH_ARM;
            if (SearchInImage(Image, "mips").Length > 0) return ImageFormat.Types.CPU.IH_ARCH_MIPS;
            if (SearchInImage(Image, "x86_64").Length > 0) return ImageFormat.Types.CPU.IH_ARCH_X86_64;
            if (SearchInImage(Image, "x86").Length > 0) return ImageFormat.Types.CPU.IH_ARCH_I386;

            return Arch;
        }

        public static ImageFormat.Types.OS DetectImageOs(byte[] Image)
        {
            if (SearchInImage(Image, "Linux version").Length > 0) return ImageFormat.Types.OS.IH_OS_LINUX;
            if (SearchInImage(Image, "netbsd32").Length > 0) return ImageFormat.Types.OS.IH_OS_NETBSD;
            if (SearchInImage(Image, "openbsd32").Length > 0) return ImageFormat.Types.OS.IH_OS_OPENBSD;
            if (SearchInImage(Image, "freebsd32").Length > 0) return ImageFormat.Types.OS.IH_OS_FREEBSD;

            return ImageFormat.Types.OS.IH_OS_INVALID;
        }
            //  
    }
}
