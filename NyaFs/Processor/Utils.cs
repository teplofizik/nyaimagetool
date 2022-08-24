using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor
{
    static class Utils
    {
        public static bool CheckMode(string Text)
        {
            if (Text.Length == 3)
            {
                // Проверим на числовой код
                for (int i = 0; i < 3; i++)
                {
                    var C = Text[i];

                    if ((C < '0') || (C > '7')) return false;
                }
                return true;
            }
            else if (Text.Length == 9)
            {
                // проверим на строковое представление
                for (int i = 0; i < 3; i++)
                {
                    int Offset = i * 3;

                    var R = Text[Offset + 0];
                    var W = Text[Offset + 1];
                    var X = Text[Offset + 2];

                    if ((R != 'r') && (R != '-')) return false;
                    if ((W != 'w') && (W != '-')) return false;
                    if ((X != 'x') && (X != 's') && (X != '-')) return false;
                }
                return true;
            }
            else
                return false;
        }

        public static UInt32 ConvertMode(string Mode)
        {
            UInt32 ModeX = 0;
            for (int i = 0; i < 3; i++)
            {
                int Offset = i * 3;

                var R = Mode[Offset + 0];
                var W = Mode[Offset + 1];
                var X = Mode[Offset + 2];

                if (R == 'r') ModeX |= 4U << ((2 - i) * 4);
                if (W == 'w') ModeX |= 2U << ((2 - i) * 4);
                if (X == 'x') 
                    ModeX |= 1U << ((2 - i) * 4);
                else if (X == 's')
                {
                    ModeX |= 1U << ((2 - i) * 4);
                    ModeX |= 1U << 12 << (2 - i);
                }
            }
            return ModeX;
        }

        public static string ConvertModeToString(UInt32 Mode)
        {
            var Res = "";
            for (int i = 0; i < 3; i++)
            {
                UInt32 Part = (Mode >> (2 - i) * 4) & 0x7;

                Res += ((Part & 0x04) != 0) ? "r" : "-";
                Res += ((Part & 0x02) != 0) ? "w" : "-";
                Res += ((Part & 0x01) != 0) ? ((((Mode >> 12 >> (2 - i)) & 0x1) != 1) ? "x" : "s") : "-";
            }
            return Res;
        }

        public static uint ConvertToUnixTimestamp(DateTime timestamp)
        {
            return Convert.ToUInt32(((DateTimeOffset)timestamp).ToUnixTimeSeconds());
        }

        public static DateTime ConvertFromUnixTimestamp(long timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }
    }
}
