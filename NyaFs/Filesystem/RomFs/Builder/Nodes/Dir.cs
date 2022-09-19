using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Filesystem.RomFs.Builder.Nodes
{
    class Dir : Node
    {
        private List<Node> Nodes = new List<Node>();
        public Node[] NestedNodes => Nodes.OrderBy(E => E.Filename, StringComparer.Ordinal).ToArray();

        public long DirLink = 0;

        public Dir(string Path, uint Mode) : base(Universal.Types.FilesystemItemType.Directory, Path, Mode)
        {

        }

        public void AddNestedNode(Node N)
        {
            Nodes.Add(N);
        }
    }
}
