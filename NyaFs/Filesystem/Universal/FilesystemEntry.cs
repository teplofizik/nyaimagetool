using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal
{
    public class FilesystemEntry
    {
        public readonly Types.FilesystemItemType NodeType;
        public readonly string Path;
        public readonly uint User;
        public readonly uint Group;
        public readonly uint Mode;
        public readonly uint Size;

        public readonly string ShortName;

        public FilesystemEntry(Types.FilesystemItemType NodeType, string Path, uint User, uint Group, uint Mode, uint Size)
        {
            this.NodeType = NodeType;
            this.Path = Path;
            this.User = User;
            this.Group = Group;
            this.Mode = Mode;
            this.Size = Size;

            var Idx = Path.LastIndexOf('/');
            ShortName = (Idx > 0) ? Path.Substring(Idx + 1) : Path;
        }

        public override string ToString() => $"{StrNodeType} {Path} {User}:{Group} {Helper.FsHelper.ConvertModeToString(Mode)} {Size} bytes";

        public UInt32 HexMode => Mode & 0xFFFU;

        private string StrNodeType
        {
            get
            {
                switch(NodeType)
                {
                    case Types.FilesystemItemType.Fifo: return "FIFO";
                    case Types.FilesystemItemType.Character: return "CHAR";
                    case Types.FilesystemItemType.Block: return "BLK ";
                    case Types.FilesystemItemType.Directory: return "DIR ";
                    case Types.FilesystemItemType.SymLink: return "LINK";
                    case Types.FilesystemItemType.File: return "FILE";
                    case Types.FilesystemItemType.Socket: return "SOCK";
                    case Types.FilesystemItemType.Unknown:
                    default:
                        return "----";
                }
            }
        }
    }
}
