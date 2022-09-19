using System;
using System.Collections.Generic;
using System.Text;
using Extension.Array;

namespace NyaFs.Filesystem.RomFs.Builder
{
    class Node
    {
        public Universal.Types.FilesystemItemType Type;
        public string Path;
        public uint Mode;

        public long FileOffset = 0;
        public long NextOffset = 0;

        public bool Executable => (Mode & 0x100) != 0;

        public Node(Universal.Types.FilesystemItemType Type, string Path, uint Mode)
        {
            this.Type = Type;
            this.Path = Path;
            this.Mode = Mode;
        }

        public string Filename => (Path == "/") ? "." : Universal.Helper.FsHelper.GetName(Path);

        protected virtual byte[] Content => null;

        protected virtual uint SpecInfo => 0;

        private uint ContentSize => Convert.ToUInt32(Content?.Length ?? 0);

        public Types.RmNode GetHeader() => new Types.RmNode(Filename, Type, Executable, ContentSize, SpecInfo);
        
        public byte[] GetContent()
        {
            if (ContentSize > 0)
            {
                var Res = new byte[ContentSize.GetAligned(0x10)];
                Res.WriteArray(0, Content, Content.Length);
                return Res;
            }
            else
                return new byte[] { };
        }
    }
}
