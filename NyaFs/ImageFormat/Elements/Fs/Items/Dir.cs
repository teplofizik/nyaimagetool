using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Items
{
    public class Dir : FilesystemItem
    {
        public List<FilesystemItem> Items = new List<FilesystemItem>();

        public Dir(string Filename, uint User, uint Group, uint Mode) : base(Types.FilesystemItemType.Dir, Filename, User, Group, Mode)
        {

        }

        public override string ToString()
        {
            return $"DIR  {Filename} {User}:{Group} {Mode:x03} items: {Items.Count}";
        }
    }
}
