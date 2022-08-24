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
            var Dtb = new FlattenedDeviceTree.Reader.FDTReader(Path).Read();
            Dst.DevTree = Dtb;
            Dst.Info.Type = Types.ImageType.IH_TYPE_FLATDT;

            Helper.LogHelper.DevtreeInfo(Dst);
        }
    }
}
