using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Writer
{
    public class LegacyWriter : Writer
    {
        string Filename;

        public LegacyWriter(string Filename)
        {
            this.Filename = Filename;
        }

        public override void WriteKernel(LinuxKernel Kernel)
        {
            if ((Kernel.Image != null) && IsImageInfoCorrect(Kernel.Info))
            {

                var Info = Kernel.Info.Clone();
                Info.Type = ImageFormat.Types.ImageType.IH_TYPE_KERNEL;

                var Image = new Types.LegacyImage(Info, Info.Compression, Helper.FitHelper.GetCompressedData(Kernel.Image, Info.Compression));
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
