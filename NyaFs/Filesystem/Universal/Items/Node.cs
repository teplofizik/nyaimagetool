using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal.Items
{
    public class Node : FilesystemItem
    {
        public Node(string Filename, uint User, uint Group, uint Mode) : base(Types.FilesystemItemType.Character, Filename, User, Group, Mode)
        {

        }

        public override string ToString()
        {
            return $"NODE {Filename} {User}:{Group} {Mode:x03} bytes";
        }
    }
}
