using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder.Nodes
{
    class Fifo : Node
    {
        public byte[] Content;

        public Fifo(string Path, uint User, uint Group, uint Mode) : base(Types.SqInodeType.BasicFifo, Path, User, Group, Mode)
        {

        }
    }
}
