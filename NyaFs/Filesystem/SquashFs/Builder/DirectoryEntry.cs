using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder
{
    class DirectoryEntry
    {
        public string Filename;
        public Types.SqInodeType Type => Node.Type;

        public MetadataRef NodeRef;

        public Node Node = null;

        public DirectoryEntry(string Filename, Node Node)
        {
            this.Filename = Filename;
            this.Node = Node;
        }
    }
}
