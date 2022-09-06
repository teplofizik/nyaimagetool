using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal.Types
{
    public class DeviceInfo
    {
        public uint Major = 0;

        public uint Minor = 0;

        public DeviceInfo(uint Major, uint Minor)
        {
            this.Major = Major;
            this.Minor = Minor;
        }
    }
}
