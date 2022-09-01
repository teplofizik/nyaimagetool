using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal.Helper
{
    internal static class FsHelper
    {
        internal static DateTime ConvertFromUnixTimestamp(long timestamp) => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timestamp);
        
        internal static uint ConvertToUnixTimestamp(DateTime timestamp) => Convert.ToUInt32(((DateTimeOffset)timestamp).ToUnixTimeSeconds());

        internal static string CombinePath(string Base, string Name)
        {
            if ((Base == "/") || (Base == ".")) return Name;

            return Base + "/" + Name;
        }

        public static string ConvertModeToString(UInt32 Mode)
        {
            var Res = "";
            for (int i = 0; i < 3; i++)
            {
                UInt32 Part = (Mode >> (2 - i) * 3) & 0x7;

                Res += ((Part & 0x04) != 0) ? "r" : "-";
                Res += ((Part & 0x02) != 0) ? "w" : "-";
                Res += ((Part & 0x01) != 0) ? ((((Mode >> 9 >> (2 - i)) & 0x1) != 1) ? "x" : "s") : "-";
            }
            return Res;
        }
    }
}
