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

        private static LinuxINodeType GetLinuxNodeType(Types.FilesystemItemType Type)
        {
            switch (Type)
            {
                case Types.FilesystemItemType.Fifo: return LinuxINodeType.FIFO;
                case Types.FilesystemItemType.Character: return LinuxINodeType.CHAR;
                case Types.FilesystemItemType.Directory: return LinuxINodeType.DIR;
                case Types.FilesystemItemType.Block: return LinuxINodeType.BLOCK;
                case Types.FilesystemItemType.File: return LinuxINodeType.REG;
                case Types.FilesystemItemType.SymLink: return LinuxINodeType.LINK;
                case Types.FilesystemItemType.Socket: return LinuxINodeType.SOCK;
                default: return LinuxINodeType.NONE;
            }
        }

        /// <summary>
        /// Filesystem node type
        /// </summary>
        public static uint GetLinuxMode(Types.FilesystemItemType Type, uint Mode)
        {
            var LinuxType = GetLinuxNodeType(Type);

            return (Mode & 0xFFF) | Convert.ToUInt32(LinuxType);
        }


        /// <summary>
        /// Filesystem node type
        /// </summary>
        public static Types.FilesystemItemType GetFsNodeType(uint LinuxMode)
        {
            switch (GetNodeType(LinuxMode))
            {
                case LinuxINodeType.FIFO: return Types.FilesystemItemType.Fifo;
                case LinuxINodeType.CHAR: return Types.FilesystemItemType.Character;
                case LinuxINodeType.DIR: return Types.FilesystemItemType.Directory;
                case LinuxINodeType.BLOCK: return Types.FilesystemItemType.Block;
                case LinuxINodeType.REG: return Types.FilesystemItemType.File;
                case LinuxINodeType.LINK: return Types.FilesystemItemType.SymLink;
                case LinuxINodeType.SOCK: return Types.FilesystemItemType.Socket;
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
