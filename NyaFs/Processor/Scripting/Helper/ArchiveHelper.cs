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

    }
}
