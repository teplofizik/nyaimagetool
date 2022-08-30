using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2.Types
{
    public class FilesystemEntry
    {
        public readonly FilesystemEntryType NodeType;
        public readonly string Path;
        public readonly uint User;
        public readonly uint Group;
        public readonly uint Mode;
        public readonly uint Size;

        public readonly string ShortName;

        public FilesystemEntry(FilesystemEntryType NodeType, string Path, uint User, uint Group, uint Mode, uint Size)
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

        public UInt32 HexMode
        {
            get
            {
                var M = Mode & 0xFFFU;
                uint Res = 0;
                Res |= (M & 0x7);
                Res |= ((M >> 3) & 0x7) << 4;
                Res |= ((M >> 6) & 0x7) << 8;
                Res |= ((M >> 9) & 0x7) << 12;

                return Res;
            }
        }

        private string StrNodeType
        {
            get
            {
                switch(NodeType)
                {
                    case FilesystemEntryType.Fifo: return "FIFO";
                    case FilesystemEntryType.Character: return "CHAR";
                    case FilesystemEntryType.Block: return "BLK ";
                    case FilesystemEntryType.Directory: return "DIR ";
                    case FilesystemEntryType.Link: return "LINK";
                    case FilesystemEntryType.Regular: return "FILE";
                    case FilesystemEntryType.Socket: return "SOCK";
                    case FilesystemEntryType.Invalid:
                    default:
                        return "----";
                }
            }
        }
    }
}
