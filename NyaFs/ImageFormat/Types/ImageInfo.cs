using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Types
{
    public class ImageInfo
    {
        public OS  OperatingSystem = OS.IH_OS_INVALID;
        public CPU Architecture = CPU.IH_ARCH_INVALID;
        public ImageType Type = ImageType.IH_TYPE_INVALID;
        public CompressionType Compression = CompressionType.IH_COMP_NONE;

        public string Name = "";

        public UInt64 DataLoadAddress = 0;
        public UInt64 EntryPointAddress = 0;

        public ImageInfo Clone()
        {
            var Res = new ImageInfo();

            Res.Architecture = Architecture;
            Res.DataLoadAddress = DataLoadAddress;
            Res.EntryPointAddress = EntryPointAddress;
            Res.Name = Name;
            Res.OperatingSystem = OperatingSystem;
            Res.Type = Type;
            Res.Compression = Compression;

            return Res;
        }
    }
}
