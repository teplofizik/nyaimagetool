using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Reader
{
    public class LegacyReader : Reader
    {
        bool Loaded = false;
        Types.LegacyImage Image;

        public LegacyReader(string Filename)
        {
            Image = new Types.LegacyImage(Filename);

            if (!Image.CorrectHeader)
            {
                Log.Error(0, $"Invalid legacy header in file {Filename}.");
                return;
            }
            if (!Image.Correct)
            {
                Log.Error(0, $"Invalid data in file {Filename}.");
                return;
            }

            if (Image.Type != ImageFormat.Types.ImageType.IH_TYPE_KERNEL)
            {
                Log.Error(0, $"File {Filename} is not kernel legacy file.");
                return;
            }

            Loaded = true;
        }

        public void UpdateImageInfo(LinuxKernel Dst)
        {
            if (Loaded)
            {
                Dst.Info.Architecture = Image.CPUArchitecture;
                Dst.Info.OperatingSystem = Image.OperatingSystem;
                Dst.Info.Name = Image.Name;
                Dst.Info.DataLoadAddress = Image.DataLoadAddress;
                Dst.Info.EntryPointAddress = Image.EntryPointAddress;
                Dst.Info.Type = Image.Type;
            }
        }

        /// <summary>
        /// Читаем в ядро
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToKernel(LinuxKernel Dst)
        {
            if (!Loaded) return;

            Dst.Image = Helper.FitHelper.GetDecompressedData(Image.ImageData, Image.Compression);
            Dst.Info.Compression = Image.Compression;
            UpdateImageInfo(Dst);
        }

        /// <summary>
        /// Тип ОС
        /// </summary>
        /// <param name="OS"></param>
        /// <returns></returns>
        private string GetOS(Types.OS OS)
        {
            switch(OS)
            {
                case Types.OS.IH_OS_LINUX: return "Linux";
                default: return $"{OS}";
            }
        }

        /// <summary>
        /// Тип архитектуры
        /// </summary>
        /// <param name="CPU"></param>
        /// <returns></returns>
        private string GetArch(Types.CPU CPU)
        {
            switch (CPU)
            {
                case Types.CPU.IH_ARCH_ARM: return "ARM";
                case Types.CPU.IH_ARCH_ARM64: return "ARM64";
                case Types.CPU.IH_ARCH_I386: return "I386";
                case Types.CPU.IH_ARCH_MIPS: return "MIPS";
                case Types.CPU.IH_ARCH_MIPS64: return "MIPS64";
                case Types.CPU.IH_ARCH_X86_64: return "X86_64";
                default: return $"{CPU}";
            }
        }

        /// <summary>
        /// Тип сжатия
        /// </summary>
        /// <param name="Compr"></param>
        /// <returns></returns>
        private string GetCompression(Types.CompressionType Compr)
        {
            switch (Compr)
            {
                case Types.CompressionType.IH_COMP_GZIP: return "gzip";
                case Types.CompressionType.IH_COMP_NONE: return "none";
                default: return $"{Compr}";
            }
        }

        private string GetType(Types.ImageType Type)
        {
            switch (Type)
            {
                case ImageFormat.Types.ImageType.IH_TYPE_KERNEL: return "kernel";
                case ImageFormat.Types.ImageType.IH_TYPE_MULTI: return "multi";
                case ImageFormat.Types.ImageType.IH_TYPE_SCRIPT: return "script";
                case ImageFormat.Types.ImageType.IH_TYPE_RAMDISK: return "ramdisk";
                default: return $"{Type}";
            }
        }
    }
}
