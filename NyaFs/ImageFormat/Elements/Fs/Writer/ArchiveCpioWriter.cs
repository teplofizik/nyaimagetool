using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class ArchiveCpioWriter : Writer
    {
        string Filename;
        byte[] PackedData = null;
        Types.CompressionType Compression;

        public ArchiveCpioWriter(Types.CompressionType Compression)
        {
            this.Compression = Compression;
        }

        public ArchiveCpioWriter(string Filename, Types.CompressionType Compression)
        {
            this.Filename = Filename;
            this.Compression = Compression;
        }

        public override void WriteFs(Filesystem Fs)
        {
            var CpWriter = new CpioWriter();
            CpWriter.WriteFs(Fs);

            var Data = Helper.FitHelper.GetCompressedData(CpWriter.RawStream, Compression);

            if (Filename != null)
            {
                PackedData = null;
                System.IO.File.WriteAllBytes(Filename, Data);
            }
            else
                PackedData = Data;
        }


        public override byte[] RawStream => PackedData;
    }
}
