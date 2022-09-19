using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.RomFs.Builder.Nodes
{
    class Socket : Node
    {
        public Socket(string Path, uint Mode) : base(Universal.Types.FilesystemItemType.Socket, Path, Mode)
        {

        }

    }
}
