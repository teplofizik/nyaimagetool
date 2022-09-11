using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder.Nodes
{
    class File : Node
    {
        public byte[] Content;
        private uint BlockSize;

        public uint[] DataBlocksSizes = new uint[] { };
        public long DataBlockOffset = 0;

        public MetadataRef FragmentRef = null;

        public File(string Path, uint User, uint Group, uint Mode, uint BlockSize, byte[] Content) : base(Types.SqInodeType.BasicFile, Path, User, Group, Mode)
        {
            this.BlockSize = BlockSize;
            this.Content = Content;
        }

        public byte[] GetFragment()
        {
            long Offset = 0;
            while (Offset + BlockSize <= Content.Length)
                Offset += BlockSize;

            if (Offset < Content.Length)
                return Content.ReadArray(Offset, Content.Length - Offset);

            return null;
        }

        public List<byte[]> GetBlocks()
        {
            var Res = new List<byte[]>();

            long Offset = 0;
            while (Offset + BlockSize <= Content.Length)
            {
                Res.Add(Content.ReadArray(Offset, BlockSize));
                Offset += BlockSize;
            }

            return Res;
        }
    }
}
