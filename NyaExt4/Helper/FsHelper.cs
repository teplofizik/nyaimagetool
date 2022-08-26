using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2.Helper
{
    internal static class FsHelper
    {
        internal static DateTime ConvertFromUnixTimestamp(long timestamp) => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timestamp);
        
        internal static uint ConvertToUnixTimestamp(DateTime timestamp) => Convert.ToUInt32(((DateTimeOffset)timestamp).ToUnixTimeSeconds());
    }
}
