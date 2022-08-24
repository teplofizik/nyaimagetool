using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Items
{
    public class Block : FilesystemItem
    {
        public Block(string Filename, uint User, uint Group, uint Mode) : base(Types.FilesystemItemType.Block, Filename, User, Group, Mode)
        {

        }

        public override string ToString()
        {
            return $"BLOCK {Filename} {User}:{Group} {Mode:x03} bytes";
        }
    }
}
