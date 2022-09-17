using CrcSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs
{
    static class Crc32
    {
        public static uint CalcCrc(byte[] data)
        {
            var crc32 = new Crc(new CrcParameters(32, 0x04c11db7, 0xffffffff, 0xffffffff, true, true));

            return Convert.ToUInt32(crc32.CalculateAsNumeric(data));
        }
    }
}
