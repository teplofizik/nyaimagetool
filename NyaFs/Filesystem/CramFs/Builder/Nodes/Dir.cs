using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Filesystem.CramFs.Builder.Nodes
{
    class Dir : Node
    {
        private List<Node> Nodes = new List<Node>();

        public Node[] NestedNodes => Nodes.OrderBy(E => E.Filename, StringComparer.Ordinal).ToArray();

        public Dir(string Path, uint User, uint Group, uint Mode) : base(Universal.Types.FilesystemItemType.Directory, Path, User, Group, Mode)
        {

        }

        public void AddNestedNode(Node N)
        {
            Nodes.Add(N);
        }
    }
}
