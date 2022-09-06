using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal.Items
{
    public class Char : FilesystemItem
    {
        public uint Major = 0;
        public uint Minor = 0;
        public uint RMajor = 0;
        public uint RMinor = 0;

        public Char(string Filename, uint User, uint Group, uint Mode) : base(Types.FilesystemItemType.Character, Filename, User, Group, Mode)
        {

        }

        public override string ToString()
        {
            return $"CHAR {Filename} {User}:{Group} {Mode:x03} bytes";
        }
    }
}
