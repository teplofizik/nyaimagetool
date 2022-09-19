using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.RomFs.Builder.Nodes
{
    class SymLink : Node
    {
        private byte[] Data;

        public SymLink(string Path, uint Mode, string Target) : base(Universal.Types.FilesystemItemType.SymLink, Path, Mode)
        {
            this.Data = UTF8Encoding.UTF8.GetBytes(Target);
        }

        protected override byte[] Content => Data;
    }
}
