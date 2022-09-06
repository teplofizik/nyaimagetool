using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Types.Nodes
{
    class BasicSymLink : SqInode
    {

        public BasicSymLink(byte[] Data) : base(Data, 0x18 + Data.ReadUInt32(0x14))
        {

        }

        /// <summary>
        /// INode size
        /// </summary>
        internal override long INodeSize => 0x18 + TargetSize; // TODO: add blocks_sizes calculation...

        /// <summary>
        /// The number of hard links to this symlink
        /// u32 hard_link_count (0x10)
        /// </summary>
        public uint HardLinkCount
        {
            get { return ReadUInt32(0x10); }
            set { WriteUInt32(0x10, value); }
        }


        /// <summary>
        /// The size in bytes of the target_path this symlink points to
        /// u32 target_size (0x14)
        /// </summary>
        public uint TargetSize
        {
            get { return ReadUInt32(0x14); }
            set { WriteUInt32(0x14, value); }
        }

        /// <summary>
        /// The index of a fragment entry in the fragment table which describes the data block the fragment of this file is stored in. 
        /// If this file does not end with a fragment, this should be 0xFFFFFFFF
        /// char target_path[target_size] (0x18)
        /// </summary>
        public byte[] TargetPath
        {
            get { return ReadArray(0x18, TargetSize); }
            set { WriteArray(0x18, value, TargetSize); }
        }

        public string Target => UTF8Encoding.UTF8.GetString(TargetPath);
    }
}
