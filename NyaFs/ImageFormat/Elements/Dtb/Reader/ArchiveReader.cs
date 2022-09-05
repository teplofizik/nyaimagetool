using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Dtb.Reader
{
    class ArchiveReader : Reader
    {
        byte[] Data = null;
        Types.CompressionType Compression;

        public ArchiveReader(string Filename, Types.CompressionType Compression)
        {
            Data = System.IO.File.ReadAllBytes(Filename);
            this.Compression = Compression;
        }

        public ArchiveReader(byte[] Data)
        {
            this.Data = Data;
        }

        public override void ReadToDevTree(DeviceTree Dst)
        {
            byte[] Raw = Helper.FitHelper.GetDecompressedData(Data, Compression);

            var Dtb = new FlattenedDeviceTree.Reader.FDTReader(Raw);
            if (Dtb.Correct)
            {
                var Reader = new DtbReader(Raw);
                Reader.ReadToDevTree(Dst);
            }
            else
                Log.Error(0, $"Unknown device tree format in compressed image.");
        }
    }
}
