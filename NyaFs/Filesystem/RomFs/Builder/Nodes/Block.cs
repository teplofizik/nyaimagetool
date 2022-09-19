using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.RomFs.Builder.Nodes
{
    class Block : Node
    {
        uint Major;
        uint Minor;

        public Block(string Path, uint Mode, uint Major, uint Minor) : base(Universal.Types.FilesystemItemType.Block, Path, Mode)
        {
            this.Major = Major;
            this.Minor = Minor;
        }

        protected override uint SpecInfo => ((Major & 0xFFFF) << 16) | (Minor & 0xFFFF);
    }
}
