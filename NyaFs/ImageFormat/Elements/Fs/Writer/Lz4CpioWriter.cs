using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class Lz4CpioWriter : Writer
    {
        string Filename;
        byte[] PackedData = null;

        public Lz4CpioWriter()
        {

        }

        public Lz4CpioWriter(string Filename)
        {
            this.Filename = Filename;
        }

        public override void WriteFs(Filesystem Fs)
        {
            var CpWriter = new CpioWriter();
            CpWriter.WriteFs(Fs);

            var Data = Compressors.Lz4.CompressWithHeader(CpWriter.RawStream);

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
