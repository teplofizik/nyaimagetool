using Extension.Array;
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
                // Is legacy image...
                uint Magic = Image.Ramdisk.ReadUInt32(0);
                if (Magic == 0x56190527)
                {
                    // Parse as legacy
                    var Reader = new LegacyReader(Image.Ramdisk);
                    Reader.ReadToFs(Dst);
                }
                else
                {
                    var Comp = (Magic == 0x04224D18)
                        ? Types.CompressionType.IH_COMP_LZ4 
                        : Helper.FitHelper.DetectCompression(Image.Ramdisk);

                    var Uncompressed = Helper.FitHelper.GetDecompressedData(Image.Ramdisk, Comp);

                    Dst.Info.Compression = Comp;
                    Dst.Info.Type = Types.ImageType.IH_TYPE_RAMDISK;
                    Dst.Info.OperatingSystem = Types.OS.IH_OS_LINUX;
                    Dst.Info.DataLoadAddress = Image.RamdiskAddress;
                    DetectAndRead(Dst, Uncompressed);
                }
            }
        }
    }
}
