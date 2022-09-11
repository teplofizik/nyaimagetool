using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder.Nodes
{
    class Block : Node
    {
        public uint Major;
        public uint Minor;

        public Block(string Path, uint User, uint Group, uint Mode, uint Major, uint Minor) : base(Types.SqInodeType.BasicBlockDevice, Path, User, Group, Mode)
        {
            this.Major = Major;
            this.Minor = Minor;
        }
    }
}
