using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs.Builder.Nodes
{
    class Socket : Node
    {
        public Socket(string Path, uint User, uint Group, uint Mode) : base(Universal.Types.FilesystemItemType.Socket, Path, User, Group, Mode)
        {

        }

    }
}
