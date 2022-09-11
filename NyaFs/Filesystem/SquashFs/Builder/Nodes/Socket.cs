using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder.Nodes
{
    class Socket : Node
    {
        public byte[] Content;

        public Socket(string Path, uint User, uint Group, uint Mode) : base(Types.SqInodeType.BasicSocket, Path, User, Group, Mode)
        {

        }
    }
}
