using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs.Builder.Nodes
{
    class Fifo : Node
    {
        public Fifo(string Path, uint User, uint Group, uint Mode) : base(Universal.Types.FilesystemItemType.Fifo, Path, User, Group, Mode)
        {

        }
    }
}
