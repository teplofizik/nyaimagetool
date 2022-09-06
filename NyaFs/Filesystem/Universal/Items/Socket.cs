using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal.Items
{
    public class Socket : FilesystemItem
    {
        public Socket(string Filename, uint User, uint Group, uint Mode) : base(Types.FilesystemItemType.Socket, Filename, User, Group, Mode)
        {

        }

        public override string ToString()
        {
            return $"SOCK {Filename} {User}:{Group} {Mode:x03} bytes";
        }
    }
}
