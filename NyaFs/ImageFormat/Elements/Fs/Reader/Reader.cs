using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class Reader
    {
        /// <summary>
        /// Читаем в файловую систему из внешнего источника
        /// </summary>
        /// <param name="Dst"></param>
        public virtual void ReadToFs(Filesystem Dst)
        {

        }

        protected static DateTime ConvertFromUnixTimestamp(long timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }

        protected bool DetectAndRead(Filesystem Dst, byte[] Raw)
        {
            var Fs = FilesystemDetector.DetectFs(Raw);
            switch (Fs)
            {
                case Types.FsType.Cpio:
                    {
                        var Reader = new CpioReader(Raw);
                        Reader.ReadToFs(Dst);

                        return true;
                    }
                case Types.FsType.Ext2:
                    {
                        var Reader = new ExtReader(Raw);
                        Reader.ReadToFs(Dst);

                        Helper.LogHelper.RamfsInfo(Dst, "Ext2");
                        return true;
                    }
                default:
                    //throw new NotImplementedException($"Unknown filesystem");
                    return false;
            }
        }
    }
}
