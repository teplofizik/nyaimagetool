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
    class BmuFsReader : NyaFs.ImageFormat.Elements.Fs.Reader.Reader
    {
        BmuImage Image;

        public BmuFsReader(BmuImage Image)
        {
            this.Image = Image;
        }

        /// <summary>
        /// Читаем в файловую систему 
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Dst)
        {
            if (Image.Correct)
            {
                var Raw = Image.GetImageByType(BmuImageType.RAMFS);

                /*byte[] Raw = NyaFs.ImageFormat.Helper.FitHelper.GetDecompressedData(Image.Content, Types.CompressionType.IH_COMP_GZIP);

                if (Dst.Info.Type == Types.ImageType.IH_TYPE_INVALID)
                    Dst.Info.Type = Types.ImageType.IH_TYPE_RAMDISK;

                Dst.Info.Compression = Types.CompressionType.IH_COMP_GZIP;*/

                if (Raw != null)
                {
                    var Comp = NyaFs.ImageFormat.Helper.FitHelper.DetectCompression(Raw);
                    if (Comp != NyaFs.ImageFormat.Types.CompressionType.IH_COMP_NONE)
                    {
                        byte[] Decompressed = NyaFs.ImageFormat.Helper.FitHelper.GetDecompressedData(Raw, Comp);

                        if(DetectAndRead(Dst, Decompressed))
                        {

                        }
                        else
                            Log.Error(0, "Invalid BMU archive: unknown ramfs format");
                    }
                    else
                    {
                        var Header = Raw.ReadUInt32(0);
                        if (Header == 0x56190527)
                        {
                            var Reader = new NyaFs.ImageFormat.Elements.Fs.Reader.LegacyReader(Raw);
                            Reader.ReadToFs(Dst);
                        }
                        else
                            Log.Error(0, "Invalid BMU archive: unknown ramfs format");
                    }
                }
                else
                    Log.Error(0, "Invalid BMU archive: no ramfs part");
            }
            else
                Log.Error(0, "Invalid BMU archive");
        }
    }
}
