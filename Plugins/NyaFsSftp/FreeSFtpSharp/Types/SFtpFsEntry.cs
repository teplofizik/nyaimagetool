using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FreeSFtpSharp.Types
{
    public class SFtpFsEntry
    {
        public string Filename;

        public uint   UID;
        public uint   GID;

        public string User;
        public string Group;

        public long Size = 0;

        public DateTime Timestamp;

        public uint Mode;

        public SFtpFsEntryType Type;

        private string ItemType
        {
            get
            {
                switch (Type)
                {
                    case SFtpFsEntryType.File: return "-";
                    case SFtpFsEntryType.Directory: return "d";
                    case SFtpFsEntryType.SymLink: return "l";
                    case SFtpFsEntryType.Character: return "c";
                    case SFtpFsEntryType.Block: return "b";
                    case SFtpFsEntryType.Fifo: return "f";
                    default: return "?";
                }
            }
        }

        public string ModeString
        {
            get
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

        public string LsLine
        {
            get
            {
                var M = $"{ItemType}{ModeString}";
                var U = ((User == null) ? $"{UID}" : $"{User}").PadLeft(9);
                var G = ((Group == null) ? $"{GID}" : $"{Group}").PadLeft(9);
                var S = $"{Size}".PadLeft(12);
                var TS = Timestamp.ToString("MMM  d  yyyy", CultureInfo.GetCultureInfo("en-US"));

                return $"{M}    1{U}{G} {S}  {TS} {Filename}";
                // writer.Write($"drwxr-xr-x   1 foo     foo        Mar 25 14:29 " + "..", Encoding.UTF8);
            }
        }
    }
}
