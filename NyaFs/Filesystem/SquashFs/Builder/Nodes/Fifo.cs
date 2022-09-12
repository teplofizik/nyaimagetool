using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder.Nodes
{
    class Fifo : Node
    {
        public Fifo(string Path, uint User, uint Group, uint Mode) : base(Types.SqInodeType.BasicFifo, Path, User, Group, Mode)
        {

        }

        public override Types.SqInode GetINode() => new Types.Nodes.BasicIPC(Types.SqInodeType.BasicFifo, Mode, UId, GId, 1);
    }
}
