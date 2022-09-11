using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder.Nodes
{
    class Char : Node
    {
        public uint Major;
        public uint Minor;

        public Char(string Path, uint User, uint Group, uint Mode, uint Major, uint Minor) : base(Types.SqInodeType.BasicCharDevice, Path, User, Group, Mode)
        {
            this.Major = Major;
            this.Minor = Minor;
        }
    }
}
