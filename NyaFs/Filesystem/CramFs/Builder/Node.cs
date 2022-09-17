using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs.Builder
{
    class Node
    {
        public Universal.Types.FilesystemItemType Type;
        public string Path;
        public uint User;
        public uint Group;
        public uint Mode;

        public uint Index = 0;

        public uint RelevantNodeOffset = 0;
        public Types.CrNode RelevantNode = null;

        public uint DataOffset = 0;
        public uint DataSize = 0;

        public Node(Universal.Types.FilesystemItemType Type, string Path, uint User, uint Group, uint Mode)
        {
            this.Type = Type;
            this.Path = Path;
            this.User = User;
            this.Group = Group;
            this.Mode = Mode;
        }

        public string Filename => (Path == "/") ? "" : Universal.Helper.FsHelper.GetName(Path);

        public virtual byte[] Content => null;

        public Types.CrNode GenerateNode() => new Types.CrNode(Universal.Helper.FsHelper.GetLinuxMode(Type, Mode), 
                                                               User & 0xFFFF, Group & 0xFF,
                                                               Convert.ToUInt32(Content?.Length ?? 0), 
                                                               Filename);
    }
}
