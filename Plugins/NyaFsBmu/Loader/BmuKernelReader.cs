using NyaFs.Processor.Scripting;
using NyaFs.Processor;
using NyaFsBmu.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using NyaFs.ImageFormat.Elements.Kernel;
using NyaFs;
using NyaIO.Data;

namespace NyaFsBmu.Loader
{
    class BmuKernelReader : NyaFs.ImageFormat.Elements.Kernel.Reader.Reader
    {
        BmuImage Image;

        public BmuKernelReader(BmuImage Data)
        {
            Image = Data;
        }

        public override void ReadToKernel(LinuxKernel Dst)
        {
            if (Image.Correct)
            {
                var Raw = Image.GetImageByType(BmuImageType.KERNEL);

                if (Raw != null)
                {
                    var Header = Raw.ReadUInt32(0);
                    if (Header == 0x56190527)
                    {
                        var Reader = new NyaFs.ImageFormat.Elements.Kernel.Reader.LegacyReader(Raw);
                        Reader.ReadToKernel(Dst);
                    }
                    else
                        Log.Error(0, "Invalid BMU archive: unknown ramfs format");
                }
            }
            else
                Log.Error(0, "Invalid BMU archive");
        }
    }
}
