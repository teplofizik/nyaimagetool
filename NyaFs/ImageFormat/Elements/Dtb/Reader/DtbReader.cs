using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Dtb.Reader
{
    public class DtbReader : Reader
    {
        string Path;

        public DtbReader(string Path)
        {
            this.Path = Path;
        }

        /// <summary>
        /// Читаем в дерево устройств из внешнего источника
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToDevTree(DeviceTree Dst)
        {
            var Dtb = new FlattenedDeviceTree.Reader.FDTReader(Path);
            if (Dtb.Correct)
            {
                Dst.DevTree = Dtb.Read();
                Dst.Info.Type = Types.ImageType.IH_TYPE_FLATDT;
                Dst.Info.Compression = Types.CompressionType.IH_COMP_NONE;

                Helper.LogHelper.DevtreeInfo(Dst);
            }
            else
                Log.Error(0, $"Invalid dtb header in {Path}");
        }
    }
}
