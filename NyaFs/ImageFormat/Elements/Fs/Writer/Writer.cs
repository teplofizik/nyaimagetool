using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class Writer
    {
        public virtual void WriteFs(LinuxFilesystem Fs)
        {

        }

        public virtual bool HasRawStreamData => RawStream != null;

        public virtual byte[] RawStream => null;

        protected static uint ConvertToUnixTimestamp(DateTime timestamp)
        {
            return Convert.ToUInt32(((DateTimeOffset)timestamp).ToUnixTimeSeconds());
        }

        public virtual bool CheckFilesystem(LinuxFilesystem Fs) => (Fs != null);
    }
}
