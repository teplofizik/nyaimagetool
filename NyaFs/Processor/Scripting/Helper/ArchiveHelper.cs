﻿using Extension.Array;
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

        private static Tuple<string, string> DetectImageFormat(byte[] Raw)
        {
            var FilesystemType = ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Raw);
            if (FilesystemType != ImageFormat.Types.FsType.Unknown)
            {
                switch (FilesystemType)
                {
                    case ImageFormat.Types.FsType.Cpio: return new Tuple<string, string>("ramfs", "cpio");
                    case ImageFormat.Types.FsType.Ext2: return new Tuple<string, string>("ramfs", "ext2");
                    case ImageFormat.Types.FsType.SquashFs: return new Tuple<string, string>("ramfs", "squashfs");
                }
            }

            // Detect FIT or DTB image...
            var Header = Raw.ReadUInt32(0);
            if (Header == 0xedfe0dd0u)
            {
                // DTB
                var DevTree = new FlattenedDeviceTree.Reader.FDTReader(Raw).Read();
                if (DevTree.HasNode("configurations") && DevTree.HasNode("images"))
                    return new Tuple<string, string>("all", "fit");
                else
                    return new Tuple<string, string>("devtree", "dtb");
            }
            // Detect legacy image
            if (Header == 0x56190527)
            {
                var Legacy = new ImageFormat.Types.LegacyImage(Raw);
                if (Legacy.Correct)
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

            // Detect linux COFF image
            if(Raw.ReadUInt16(0) == 0x5A4D)
                return new Tuple<string, string>("kernel", "raw");

            return null;
        }

        public static Tuple<string,string> DetectImageFormat(string Filename)
        {
            var Data = System.IO.File.ReadAllBytes(Filename);
            // Detect by extension
            var Extension = System.IO.Path.GetExtension(Filename);
            switch(Extension)
            {
                case ".gz": return TryDecompressArchive(Data, "gzip", ImageFormat.Types.CompressionType.IH_COMP_GZIP);
                case ".zst":
                case ".zstd": return TryDecompressArchive(Data, "zstd", ImageFormat.Types.CompressionType.IH_COMP_ZSTD);
                case ".bz2":
                case ".bzip2": return TryDecompressArchive(Data, "bzip2", ImageFormat.Types.CompressionType.IH_COMP_BZIP2);
                case ".lz4": return TryDecompressArchive(Data, "lz4", ImageFormat.Types.CompressionType.IH_COMP_LZ4);
                case ".lzimg":
                case ".lzma": return TryDecompressArchive(Data, "lzma", ImageFormat.Types.CompressionType.IH_COMP_LZMA);
            }

            // Detect by content
            return DetectImageFormat(Data);
        }

        private static Tuple<string, string> TryDecompressArchive(byte[] Data, string Name, ImageFormat.Types.CompressionType Type)
        {
            try
            {
                var Uncompressed = ImageFormat.Helper.FitHelper.GetDecompressedData(Data, Type);

                var Res = DetectImageFormat(Uncompressed);
                if(Res != null)
                {
                    if (Res.Item1 == "ramfs")
                        return new Tuple<string, string>("ramfs", Name);
                    if(Res.Item1 == "kernel")
                        return new Tuple<string, string>("kernel", Name);
                }
                return null;
            }
            catch(Exception)
            {
                return null;
            }
        }
    }
}
