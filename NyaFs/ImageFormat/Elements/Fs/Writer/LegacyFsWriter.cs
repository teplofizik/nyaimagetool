using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class LegacyFsWriter : Writer
    {
        string Filename;

        public LegacyFsWriter(string Filename)
        {
            this.Filename = Filename;
        }

        public override void WriteFs(Filesystem Fs)
        {
            if (IsImageInfoCorrect(Fs.Info))
            {
                var CpWriter = new CpioWriter();
                CpWriter.WriteFs(Fs);

                var PackedData = Compressors.Gzip.CompressWithHeader(CpWriter.RawStream);

                var Info = Fs.Info.Clone();
                Info.Type = ImageFormat.Types.ImageType.IH_TYPE_RAMDISK;

                var Image = new Types.LegacyImage(Info, PackedData);
                System.IO.File.WriteAllBytes(Filename, Image.getPacket());
            }
        }

        public override bool CheckFilesystem(Filesystem Fs) => base.CheckFilesystem(Fs) && IsImageInfoCorrect(Fs.Info);

        bool IsImageInfoCorrect(Types.ImageInfo Info)
        {
            if(Info.Architecture == Types.CPU.IH_ARCH_INVALID)
            {
                Log.Error(0, "No architecture info for generate legacy ramfs image.");
                return false;
            }

            if (Info.OperatingSystem == Types.OS.IH_OS_INVALID)
            {
                Log.Error(0, "No operating system info for generate legacy ramfs image.");
                return false;
            }

            return true;
        }
    }
}
