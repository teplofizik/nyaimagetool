using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.RomFs.Builder.Nodes
{
    class Parent : Node
    {
        private long Offset;

        public Parent(long Offset) : base(Universal.Types.FilesystemItemType.HardLink, "..", 0)
        {
            this.Offset = Offset;
        }

        protected override uint SpecInfo => Convert.ToUInt32(Offset);
    }
}
