using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Dtb.Reader
{
    class AndroidReader : Reader
    {
        Types.Android.LegacyAndroidImage Image;

        public AndroidReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename)) { }
        public AndroidReader(byte[] Raw)
        {
            Image = new Types.Android.LegacyAndroidImage(Raw);
        }

        /// <summary>
        /// Читаем в дерево устройств из внешнего источника
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToDevTree(DeviceTree Dst)
        {
            if (Image.IsMagicCorrect)
            {
                // TODO: detect, is dtb present in image
            }
        }
    }
}
