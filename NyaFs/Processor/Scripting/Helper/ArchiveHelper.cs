using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Helper
{
    internal static class ArchiveHelper
    {
        /// <summary>
        /// Get compression format in uboot format
        /// </summary>
        /// <param name="Format">compression format name</param>
        /// <returns></returns>
        internal static ImageFormat.Types.CompressionType GetCompressionFormat(string Format)
        {
            switch (Format)
            {
                case "raw": return ImageFormat.Types.CompressionType.IH_COMP_NONE;
                case "lzma": return ImageFormat.Types.CompressionType.IH_COMP_LZMA;
                case "lz4": return ImageFormat.Types.CompressionType.IH_COMP_LZ4;
                case "gz":
                case "gzip":
                    return ImageFormat.Types.CompressionType.IH_COMP_GZIP;
                case "bz2":
                case "bzip2":
                    return ImageFormat.Types.CompressionType.IH_COMP_BZIP2;
                case "zstd":
                    return ImageFormat.Types.CompressionType.IH_COMP_ZSTD;
                default: throw new ArgumentException($"Invalid compression format: {Format}");
            }
        }

        public static Tuple<string,string> DetectArchiveFormat(string Filename)
        {
            var Raw = System.IO.File.ReadAllBytes(Filename);

            var FilesystemType = ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Raw);
            if(FilesystemType != ImageFormat.Types.FsType.Unknown)
            {
                switch(FilesystemType)
                {
                    case ImageFormat.Types.FsType.Cpio: return new Tuple<string, string>("ramfs", "cpio");
                    case ImageFormat.Types.FsType.Ext2: return new Tuple<string, string>("ramfs", "ext2");
                    case ImageFormat.Types.FsType.SquashFs: return new Tuple<string, string>("ramfs", "squashfs");
                }
            }

            // Detect FIT or DTB image...
            var Header = Raw.ReadUInt32(0);
            if(Header == 0xedfe0dd0u)
            {
                // DTB
                var DevTree = new FlattenedDeviceTree.Reader.FDTReader(Raw).Read();
                if (DevTree.HasNode("default") && DevTree.HasNode("images"))
                    return new Tuple<string, string>("all", "fit");
                else
                    return new Tuple<string, string>("devtree", "dtb");
            }
            // Detect legacy image
            if(Header == 0x56190527)
            {
                var Legacy = new ImageFormat.Types.LegacyImage(Raw);
                if(Legacy.Correct)
                {
                    switch (Legacy.Type)
                    {
                        case ImageFormat.Types.ImageType.IH_TYPE_RAMDISK: return new Tuple<string, string>("ramdisk", "legacy");
                        case ImageFormat.Types.ImageType.IH_TYPE_KERNEL: return new Tuple<string, string>("kernel", "legacy");
                        default: return null;
                    }
                }
            }
            // Detect Android image
            if (Header == 0x52444E41u)
                return new Tuple<string, string>("all", "android");

            // Detect archives
            // ...

            return null;
        }
    }
}
