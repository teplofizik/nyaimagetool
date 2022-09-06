using Extension.Array;
using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Ext2.Types
{
    class ExtDirectoryEntry : ArrayWrapper
    {
        /// <summary>
        /// Wrapper for directory struct
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Offset"></param>
        public ExtDirectoryEntry(byte[] Data, long Offset) : base(Data, Offset, 255) // ext2, ext3 => 128 bytes
        {

        }

        public ExtDirectoryEntry(uint INode, ExtINodeType Type, string Name) : base(new byte[8 + Name.Length.GetAligned(4)], 0, 8 + Name.Length.GetAligned(4))
        {
            this.INode = INode;
            RecordLength = Convert.ToUInt32(getLength());
            NameLength = Convert.ToUInt32(Name.Length);
            NodeType = Type;
            WriteString(8, Name, Name.Length);
        }

        /// <summary>
        /// Number of the inode that this directory entry points to.
        /// </summary>
        public uint INode
        {
            get { return ReadUInt32(0); }
            set { WriteUInt32(0, value); }
        }

        /// <summary>
        /// Length of this directory entry. Must be a multiple of 4.
        /// </summary>
        public uint RecordLength
        {
            get { return ReadUInt16(4); }
            set { WriteUInt16(4, value); }
        }

        /// <summary>
        /// Length of the file name.
        /// </summary>
        public uint NameLength
        {
            get { return ReadByte(6); }
            set { WriteByte(6, value); }
        }

        /// <summary>
        /// Node type (cached), is not null
        /// </summary>
        public ExtINodeType NodeType
        {
            get { 
                var type = ReadByte(7); 
                switch(type)
                {
                    case 1: return ExtINodeType.REG;
                    case 2: return ExtINodeType.DIR;
                    case 3: return ExtINodeType.CHAR;
                    case 4: return ExtINodeType.BLOCK;
                    case 5: return ExtINodeType.FIFO;
                    case 6: return ExtINodeType.SOCK;
                    case 7: return ExtINodeType.LINK;

                    default: return ExtINodeType.NONE;
                }
            }
            set {
                switch (value)
                {
                    case ExtINodeType.REG: WriteByte(7, 1); break;
                    case ExtINodeType.DIR: WriteByte(7, 2); break;
                    case ExtINodeType.CHAR: WriteByte(7, 3); break;
                    case ExtINodeType.BLOCK: WriteByte(7, 4); break;
                    case ExtINodeType.FIFO: WriteByte(7, 5); break;
                    case ExtINodeType.SOCK: WriteByte(7, 6); break;
                    case ExtINodeType.LINK: WriteByte(7, 7); break;
                    default: WriteByte(7, 0); break;
                }
            }
        }

        public string Name => ReadString(8, NameLength);

    }
}
