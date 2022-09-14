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
                if(Image.HeaderVersion == 2)
                {
                    // v2 contains Dtb
                    var Imagev2 = new Types.Android.AndroidImagev2(Image.getPacket());

                    var Raw = Imagev2.Dtbo;
                    if(Raw.Length > 0)
                    {
                        var Dtb = new FlattenedDeviceTree.Reader.FDTReader(Raw);
                        if (Dtb.Correct)
                        {
                            var Reader = new DtbReader(Raw);
                            Reader.ReadToDevTree(Dst);
                        }
                        else
                            Log.Error(0, $"Unknown device tree format in android image.");
                    }
                }
                else if(Image.HeaderVersion < 2)
                {
                    // No dtb...
                }
                else
                {
                    // Different image format!..
                    throw new NotImplementedException("Android image v3-4 are not supported now!");
                }
            }
        }
    }
}
