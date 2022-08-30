using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Writer
{
    public class LegacyWriter : Writer
    {
        string Filename;
        bool Compressed;

        public LegacyWriter(string Filename, bool Compressed)
        {
            this.Filename = Filename;
            this.Compressed = Compressed;
        }

        private Types.LegacyImage GetImage(Types.ImageInfo Info, byte[] Raw)
        {
            if(Compressed)
            {
                var PackedData = Compressors.Gzip.CompressWithHeader(Raw);

                return new Types.LegacyImage(Info, Types.CompressionType.IH_COMP_GZIP, PackedData);
            }
            else
                return new Types.LegacyImage(Info, Types.CompressionType.IH_COMP_NONE, Raw);
        }

        public override void WriteKernel(LinuxKernel Kernel)
        {
            if ((Kernel.Image != null) && IsImageInfoCorrect(Kernel.Info))
            {

                var Info = Kernel.Info.Clone();
                Info.Type = ImageFormat.Types.ImageType.IH_TYPE_KERNEL;

                var Image = GetImage(Info, Kernel.Image);
                System.IO.File.WriteAllBytes(Filename, Image.getPacket());
            }
        }

        bool IsImageInfoCorrect(Types.ImageInfo Info)
        {
            if(Info.Architecture == Types.CPU.IH_ARCH_INVALID)
            {
                Log.Error(0, "No architecture info for generate legacy kernel image.");
                return false;
            }

            if (Info.OperatingSystem == Types.OS.IH_OS_INVALID)
            {
                Log.Error(0, "No operating system info for generate legacy kernel image.");
                return false;
            }

            return true;
        }
    }
}
