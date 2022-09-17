using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs.Builder.Nodes
{
    class Char : Node
    {
        uint Major;
        uint Minor;

        public Char(string Path, uint User, uint Group, uint Mode, uint Major, uint Minor) : base(Universal.Types.FilesystemItemType.Character, Path, User, Group, Mode)
        {
            this.Major = Major;
            this.Minor = Minor;
        }

    }
}
