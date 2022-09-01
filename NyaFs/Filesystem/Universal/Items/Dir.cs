using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal.Items
{
    public class Dir : FilesystemItem
    {
        public List<FilesystemItem> Items = new List<FilesystemItem>();

        public Dir(string Filename, uint User, uint Group, uint Mode) : base(Types.FilesystemItemType.Directory, Filename, User, Group, Mode)
        {

        }

        public override string ToString()
        {
            return $"DIR  {Filename} {User}:{Group} {Mode:x03} items: {Items.Count}";
        }
    }
}
