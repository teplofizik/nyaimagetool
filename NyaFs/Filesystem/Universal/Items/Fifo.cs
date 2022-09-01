using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal.Items
{
    public class Fifo : FilesystemItem
    {
        public Fifo(string Filename, uint User, uint Group, uint Mode) : base(Types.FilesystemItemType.Fifo, Filename, User, Group, Mode)
        {

        }

        public override string ToString()
        {
            return $"FIFO {Filename} {User}:{Group} {Mode:x03} bytes";
        }
    }
}
