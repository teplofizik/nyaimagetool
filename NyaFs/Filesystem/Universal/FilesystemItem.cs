using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal
{
    public class FilesystemItem
    {
        private static string PreprocessFilename(string Filename)
        {
            if (Filename.Length > 1)
            {
                if ((Filename[0] == '\\') || (Filename[0] == '/'))
                    return Filename.Substring(1);
            }

            return Filename;
        }

        public FilesystemItem(Types.FilesystemItemType Type, string Filename, uint User, uint Group, uint Mode)
        {
            ItemType = Type;
            Modified = DateTime.Now;

            this.User = User;
            this.Group = Group;
            this.Mode = Mode;

            this.Filename = PreprocessFilename(Filename);
        }

        public readonly Types.FilesystemItemType ItemType;

        public uint User;
        public uint Group;

        public uint Mode; // Access rights 12 bit 4 4 4

        public string Filename;
        public string ShortFilename
        {
            get
            {
                var Idx = Filename.LastIndexOf('/');
                return (Idx > 0) ? Filename.Substring(Idx + 1) : Filename;
            }
        }

        public virtual long Size => 0;

        public DateTime Created = DateTime.UnixEpoch;
        public DateTime Modified = DateTime.UnixEpoch;
    }
}
