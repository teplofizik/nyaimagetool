using NyaFs.Processor.Scripting;
using NyaFs.Processor;
using NyaFsInnosilicon.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using NyaFs.ImageFormat.Elements.Kernel;
using NyaFs;
using NyaIO.Data;

namespace NyaFsInnosilicon.Loader
{
    class SwuKernelReader : NyaFs.ImageFormat.Elements.Kernel.Reader.Reader
    {
        InnoFirmwareUpdate Image;

        public SwuKernelReader(InnoFirmwareUpdate Data)
        {
            Image = Data;
        }

        public override void ReadToKernel(LinuxKernel Dst)
        {
            if (Image.Correct)
            {
                var Raw = Image.Kernel;

                if (Raw != null)
                {
                    var Header = Raw.ReadUInt32(0);
                    if (Header == 0x56190527)
                    {
                        var Reader = new NyaFs.ImageFormat.Elements.Kernel.Reader.LegacyReader(Raw);
                        Reader.ReadToKernel(Dst);
                    }
                    else
                        Log.Error(0, "Invalid Innosilicon archive: unknown ramfs format");
                }
            }
            else
                Log.Error(0, "Invalid Innosilicon archive");
        }
    }
}
