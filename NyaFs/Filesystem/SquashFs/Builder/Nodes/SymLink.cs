using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder.Nodes
{
    class SymLink : Node
    {
        public string Target;

        public SymLink(string Path, uint User, uint Group, uint Mode, string Target) : base(Types.SqInodeType.BasicSymlink, Path, User, Group, Mode)
        {
            this.Target = Target;
        }

        public override Types.SqInode GetINode() => new Types.Nodes.BasicSymLink(Mode, UId, GId, 1, Target);
    }
}
