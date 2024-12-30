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
using NyaFs.FlattenedDeviceTree;
using NyaFs.ImageFormat.Elements.Dtb;

namespace NyaFsBmu.Loader
{
    class BmuDevtreeReader : NyaFs.ImageFormat.Elements.Dtb.Reader.Reader
    {
        BmuImage Image;

        public BmuDevtreeReader(BmuImage Data)
        {
            Image = Data;
        }

        /// <summary>
        /// Читаем в дерево устройств из внешнего источника
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToDevTree(DeviceTree Dst)
        {
            var Data = Image.GetImageByType(BmuImageType.DEVTREE);
            if (Data != null)
            {
                var Dtb = new NyaFs.FlattenedDeviceTree.Reader.FDTReader(Data);
                if (Dtb.Correct)
                {
                    Dst.DevTree = Dtb.Read();
                    Dst.Info.Type = NyaFs.ImageFormat.Types.ImageType.IH_TYPE_FLATDT;
                    Dst.Info.Compression = NyaFs.ImageFormat.Types.CompressionType.IH_COMP_NONE;
                }
                else
                    Log.Error(0, $"Invalid dtb header in BMU");
            }
            else
                Log.Error(0, $"No dtb in BMU");
        }
    }
}
