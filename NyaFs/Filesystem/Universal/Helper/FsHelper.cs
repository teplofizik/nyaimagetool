using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal.Helper
{
    internal static class FsHelper
    {
        internal static DateTime ConvertFromUnixTimestamp(long timestamp) => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timestamp);
        
        internal static uint ConvertToUnixTimestamp(DateTime timestamp) => Convert.ToUInt32(((DateTimeOffset)timestamp).ToUnixTimeSeconds());

        internal static string GetName(string Path)
        {
            int Pos = Path.LastIndexOf('/');
            if (Pos >= 0)
                return Path.Substring(Pos + 1);
            else
                return Path;
        }

        internal static string GetParentDirPath(string Path)
        {
            int Pos = Path.LastIndexOf('/');
            if (Pos >= 0)
            {
                var Res = Path.Substring(0, Pos);
                return (Res.Length > 0) ? Res : "/";
            }
            else
                return "/";
        }

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


        /// <summary>
        /// 0x1000  S_IFIFO(FIFO)
        /// 0x2000  S_IFCHR(Character device)
        /// 0x4000  S_IFDIR(Directory)
        /// 0x6000  S_IFBLK(Block device)
        /// 0x8000  S_IFREG(Regular file)
        /// 0xA000  S_IFLNK(Symbolic link)
        /// 0xC000  S_IFSOCK(Socket)
        /// </summary>
        private static LinuxINodeType GetNodeType(uint LinuxMode) => (LinuxINodeType)(LinuxMode & 0xF000);

        /// <summary>
        /// Filesystem node type
        /// </summary>
        public static Types.FilesystemItemType GetFsNodeType(uint LinuxMode)
        {
            switch (GetNodeType(LinuxMode))
            {
                case LinuxINodeType.FIFO: return Universal.Types.FilesystemItemType.Fifo;
                case LinuxINodeType.CHAR: return Universal.Types.FilesystemItemType.Character;
                case LinuxINodeType.DIR: return Universal.Types.FilesystemItemType.Directory;
                case LinuxINodeType.BLOCK: return Universal.Types.FilesystemItemType.Block;
                case LinuxINodeType.REG: return Universal.Types.FilesystemItemType.File;
                case LinuxINodeType.LINK: return Universal.Types.FilesystemItemType.SymLink;
                case LinuxINodeType.SOCK: return Universal.Types.FilesystemItemType.Socket;
                default: return Types.FilesystemItemType.Unknown;
            }
        }

        /// <summary>
        /// Mode as string
        /// </summary>
        public static string GetModeString(uint LinuxMode) => ConvertModeToString(LinuxMode & 0xFFF);

        /// <summary>
        /// 0x1000  S_IFIFO(FIFO)
        /// 0x2000  S_IFCHR(Character device)
        /// 0x4000  S_IFDIR(Directory)
        /// 0x6000  S_IFBLK(Block device)
        /// 0x8000  S_IFREG(Regular file)
        /// 0xA000  S_IFLNK(Symbolic link)
        /// 0xC000  S_IFSOCK(Socket)
        /// </summary>
        private enum LinuxINodeType
        {
            NONE = 0x0000,
            FIFO = 0x1000,
            CHAR = 0x2000,
            DIR = 0x4000,
            BLOCK = 0x6000,
            REG = 0x8000,
            LINK = 0xA000,
            SOCK = 0xC000
        }
    }
}
