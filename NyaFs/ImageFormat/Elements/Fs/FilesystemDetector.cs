using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs
{
    static class FilesystemDetector
    {
        private static bool IsExt4(byte[] Raw)
        {
            for(int i = 0; i < 0x400; i++)
            {
                if (Raw[i] != 0)
                    return false;
            }

            // Check magic
            if (Raw.ReadUInt16(0x438) != 0xEF53)
                return false;

            return true;
        }

        public static Types.FsType DetectFs(byte[] Raw)
        {
            if (Raw.ReadUInt32BE(0) == 0x30373037u) // CPIO start of block
                return Types.FsType.Cpio;

            if (Raw.ReadUInt32BE(0) == 0x68737173) // hsqs magic
                return Types.FsType.SquashFs;

            if (IsExt4(Raw))
                return Types.FsType.Ext2;

            return Types.FsType.Unknown;
        }

        public static string GetFilesystemType(Types.FsType Type)
        {
            switch (Type)
            {
                case Types.FsType.Cpio: return "CPIO (ASCII)";
                case Types.FsType.Ext2: return "Ext2";
                case Types.FsType.SquashFs: return "SquashFs";
                default: return "Unknown";
            }
        }
    }

}
