using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.RomFs.Builder.Nodes
{
    class Fifo : Node
    {
        public Fifo(string Path, uint Mode) : base(Universal.Types.FilesystemItemType.Fifo, Path, Mode)
        {

        }
    }
}
