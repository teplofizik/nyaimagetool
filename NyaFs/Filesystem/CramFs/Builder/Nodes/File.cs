using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs.Builder.Nodes
{
    class File : Node
    {
        private byte[] Data;

        public File(string Path, uint User, uint Group, uint Mode, byte[] Data) : base(Universal.Types.FilesystemItemType.File, Path, User, Group, Mode)
        {
            this.Data = Data;
        }

        public override byte[] Content => Data;
    }
}
