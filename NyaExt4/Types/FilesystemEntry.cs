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

        public FilesystemEntry(FilesystemEntryType NodeType, string Path, uint User, uint Group, uint Mode, uint Size)
        {
            this.NodeType = NodeType;
            this.Path = Path;
            this.User = User;
            this.Group = Group;
            this.Mode = Mode;
            this.Size = Size;
        }
    }
}
