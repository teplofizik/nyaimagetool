using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Dtb.Reader
{
    public class DtbReader : Reader
    {
        byte[] Data;
        string Path;

        public DtbReader(string Path)
        {
            this.Path = Path;
            this.Data = System.IO.File.ReadAllBytes(Path);
        }

        public DtbReader(byte[] Data)
        {
            this.Path = "loaded image";
            this.Data = Data;
        }

        /// <summary>
        /// Читаем в дерево устройств из внешнего источника
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToDevTree(DeviceTree Dst)
        {
            var Dtb = new FlattenedDeviceTree.Reader.FDTReader(Data);
            if (Dtb.Correct)
            {
                Dst.DevTree = Dtb.Read();
                Dst.Info.Type = Types.ImageType.IH_TYPE_FLATDT;
                Dst.Info.Compression = Types.CompressionType.IH_COMP_NONE;
            }
            else
                Log.Error(0, $"Invalid dtb header in {Path}");
        }
    }
}
