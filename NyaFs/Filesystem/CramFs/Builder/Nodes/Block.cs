using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs.Builder.Nodes
{
    class Block : Node
    {
        uint Major;
        uint Minor;

        public Block(string Path, uint User, uint Group, uint Mode, uint Major, uint Minor) : base(Universal.Types.FilesystemItemType.Block, Path, User, Group, Mode)
        {
            this.Major = Major;
            this.Minor = Minor;
        }

    }
}
