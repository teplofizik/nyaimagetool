using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.RomFs.Builder.Nodes
{
    class File : Node
    {
        private byte[] Data;

        public File(string Path, uint Mode, byte[] Data) : base(Universal.Types.FilesystemItemType.File, Path, Mode)
        {
            this.Data = Data;
        }

        protected override byte[] Content => Data;
    }
}
