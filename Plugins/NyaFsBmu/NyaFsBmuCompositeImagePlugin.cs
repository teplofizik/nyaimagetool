using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsBmu
{
    /// <summary>
    /// Bitmain BMU firmware, two kinds (see FORMAT.md):
    ///  - single (magic 0x26): one set of files (uboot/devicetree/kernel/ramfs/...);
    ///  - merge (magic 0xABABABAB): several single BMUs for different model/hardware/chip.
    /// Contained elements are reported as Unknown so the pipeline re-runs detection on each
    /// (a single-BMU file is itself a standard container: uImage / dtb / gzip-ramfs / ...).
    /// </summary>
    public class NyaFsBmuCompositeImagePlugin : CompositeImagePlugin
    {
        private const int SingleHeaderSize = 0x800;
        private const int SingleFileCountOffset = 1304;   // 0x518
        private const int SingleDescriptorBase = 0x51D;   // 1309
        private const int SinglePayloadBase = 0x800;

        private const uint MergeMagic = 0xABABABABu;

        public NyaFsBmuCompositeImagePlugin() : base("image.bmu", "bmu")
        {
        }

        public override bool DetectImage(ImageData Image)
        {
            if (Image.Length < 4)
                return false;

            if (Image.ReadByte(0) == 0x26)
            {
                if (Image.Length < SingleHeaderSize)
                    return false;

                // Plausibility: file_count == popcount(content bitmask).
                var Content = Image.ReadUInt16BE(11);
                var FileCount = Image.ReadByte(SingleFileCountOffset);
                return (FileCount > 0) && (PopCount(Content) == FileCount);
            }

            if (Image.ReadUInt32(0) == MergeMagic)
            {
                if (Image.Length < 36)
                    return false;

                return Image.ReadUInt32(0x10) == 172; // item size
            }

            return false;
        }

        public override ContainedImage[] GetImages(ImageData Image)
        {
            if (Image.ReadByte(0) == 0x26)
                return GetSingleImages(Image);

            if (Image.ReadUInt32(0) == MergeMagic)
                return GetMergeImages(Image);

            return new ContainedImage[0];
        }

        private ContainedImage[] GetSingleImages(ImageData Image)
        {
            var FileCount = Image.ReadByte(SingleFileCountOffset);
            var Res = new List<ContainedImage>();

            long Offset = SinglePayloadBase;
            for (int i = 0; i < FileCount; i++)
            {
                var Descriptor = SingleDescriptorBase + i * 5;
                if (Descriptor + 5 > Image.Length)
                    break;

                int Code = Image.ReadByte(Descriptor);
                uint Size = Image.ReadUInt32BE(Descriptor + 1);
                var Name = FileName(Code);

                if (Offset + Size > Image.Length)
                {
                    Res.Add(new ContainedImage(Name, ImageKind.Unknown, null, Offset, Size, null));
                    break;
                }

                var Content = Image.ReadArray(Offset, Size);
                Res.Add(new ContainedImage(Name, ImageKind.Unknown, null, Offset, Size, Content));
                Offset += Size;
            }

            return Res.ToArray();
        }

        private ContainedImage[] GetMergeImages(ImageData Image)
        {
            var Count = Image.ReadUInt32(0x0C);
            var ItemSize = Image.ReadUInt32(0x10);
            var Res = new List<ContainedImage>();

            for (uint i = 0; i < Count; i++)
            {
                long Base = 36 + i * ItemSize;
                if (Base + ItemSize > Image.Length)
                    break;

                var Filename = ReadAnsi(Image, Base + 4, Image.ReadByte(Base), 64);
                var Chip = ReadAnsi(Image, Base + 68, Image.ReadByte(Base + 1), 32);
                var Hardware = ReadAnsi(Image, Base + 100, Image.ReadByte(Base + 2), 32);
                var Model = ReadAnsi(Image, Base + 132, Image.ReadByte(Base + 3), 32);

                var Offset = Image.ReadUInt32(Base + 164);
                var Size = Image.ReadUInt32(Base + 168);

                var Segments = new List<string>();
                if (Model.Length > 0) Segments.Add(Model);
                if (Hardware.Length > 0) Segments.Add(Hardware);
                if (Chip.Length > 0) Segments.Add(Chip);
                Segments.Add(Filename);
                var Name = string.Join("/", Segments);

                byte[] Content = (Offset + Size <= Image.Length) ? Image.ReadArray(Offset, Size) : null;

                Res.Add(new ContainedImage(Name, ImageKind.Unknown, "bmu", Offset, Size, Content));
            }

            return Res.ToArray();
        }

        private static string ReadAnsi(ImageData Image, long Offset, int Length, int FieldSize)
        {
            if ((Length <= 0) || (Length > FieldSize) || (Offset + Length > Image.Length))
                return "";

            return Encoding.ASCII.GetString(Image.ReadArray(Offset, Length));
        }

        private static string FileName(int Code)
        {
            switch (Code)
            {
                case 0: return "BOOT.bin";
                case 1: return "devicetree.dtb";
                case 2: return "uImage";
                case 3: return "minerfs.image.gz";
                case 4: return "update.image.gz";
                case 5: return "crl.tar.gz";
                case 6: return "miner.btm.tar.gz";
                case 7: return "reserve";
                case 9: return "datafile";
                default: return $"file_{Code}";
            }
        }

        private static int PopCount(uint V)
        {
            int Res = 0;
            while (V != 0) { Res += (int)(V & 1); V >>= 1; }
            return Res;
        }
    }
}
