using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    class AndroidReader : Reader
    {
        Types.Android.LegacyAndroidImage Image;

        public AndroidReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename)) { }
        public AndroidReader(byte[] Raw)
        {
            Image = new Types.Android.LegacyAndroidImage(Raw);
        }

        public override void ReadToFs(LinuxFilesystem Dst)
        {
            if (Image.IsMagicCorrect)
            {
                var Uncompressed = Compressors.Gzip.Decompress(Image.Ramdisk);

                Dst.Info.Compression = Types.CompressionType.IH_COMP_GZIP;
                Dst.Info.DataLoadAddress = Image.RamdiskAddress;
                DetectAndRead(Dst, Uncompressed);
            }
        }
    }
}
