using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs.Builder.Nodes
{
    class SymLink : Node
    {
        private byte[] Data;

        public SymLink(string Path, uint User, uint Group, uint Mode, string Target) : base(Universal.Types.FilesystemItemType.SymLink, Path, User, Group, Mode)
        {
            this.Data = UTF8Encoding.UTF8.GetBytes(Target);
        }

        public override byte[] Content => Data;
    }
}
