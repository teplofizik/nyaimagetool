using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.RomFs.Builder.Nodes
{
    class Char : Node
    {
        uint Major;
        uint Minor;

        public Char(string Path, uint Mode, uint Major, uint Minor) : base(Universal.Types.FilesystemItemType.Character, Path, Mode)
        {
            this.Major = Major;
            this.Minor = Minor;
        }

        protected override uint SpecInfo => ((Major & 0xFFFF) << 16) | (Minor & 0xFFFF);
    }
}
