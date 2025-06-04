using NyaFs.Processor.Scripting;
using NyaFs.Processor;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using NyaFs.ImageFormat.Elements.Kernel;
using NyaFs;
using NyaIO.Data;
using ZstdSharp;

namespace NyaFsRockchip.Loader
{
    class RkfwFsReader : NyaFs.ImageFormat.Elements.Fs.Reader.Reader
    {
        byte[] BinaryFS;

        public RkfwFsReader(byte[] BinaryFS)
        {
            this.BinaryFS = BinaryFS;
        }

        /// <summary>
        /// Читаем в файловую систему 
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Dst)
        {
            /*byte[] Raw = NyaFs.ImageFormat.Helper.FitHelper.GetDecompressedData(Image.Content, Types.CompressionType.IH_COMP_GZIP);

            if (Dst.Info.Type == Types.ImageType.IH_TYPE_INVALID)
                Dst.Info.Type = Types.ImageType.IH_TYPE_RAMDISK;

            Dst.Info.Compression = Types.CompressionType.IH_COMP_GZIP;*/

            if (BinaryFS != null)
            {
                var Comp = NyaFs.ImageFormat.Helper.FitHelper.DetectCompression(BinaryFS);
                if (Comp != NyaFs.ImageFormat.Types.CompressionType.IH_COMP_NONE)
                {
                    byte[] Decompressed = NyaFs.ImageFormat.Helper.FitHelper.GetDecompressedData(BinaryFS, Comp);

                    if(DetectAndRead(Dst, Decompressed))
                    {

                    }
                    else
                        Log.Error(0, "Unsupported firmware image: unknown ramfs format");
                }
                else
                {
                    if (DetectAndRead(Dst, BinaryFS))
                    {

                    }
                    else
                        Log.Error(0, "Unsupported firmware image: unknown ramfs format");
                }
            }
            else
                Log.Error(0, "Unsupported firmware image: no ramfs part");
        }
    }
}
