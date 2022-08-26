using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2
{
    public class ExtFs : RawPacket
    {
        public ExtFs(byte[] Data) : base(Data)
        {
            Init();
        }

        public ExtFs(string Filename) : this(System.IO.File.ReadAllBytes(Filename))
        {

        }

        public Types.ExtSuperBlock SuperBlock  => new Types.ExtSuperBlock(ReadArray(0x400, 0x400));
        public Types.ExtBlockGroup BlockGroup => new Types.ExtBlockGroup(ReadArray(0x800, 0x400));

        protected virtual void Init()
        {

        }


        public virtual void DumpINodes()
        {

        }

        public virtual void Dump()
        {
            var SB = SuperBlock;
            var BG = BlockGroup;

            Console.WriteLine($"          Block size: 0x{SB.BlockSize:x04}");
            Console.WriteLine($"          INode size: 0x{SB.INodeSize:x04}");
            Console.WriteLine($"         INode count: {SB.INodesCount}");

            Console.WriteLine($" INode table address: 0x{BG.INodeTableLo:x08}");
            Console.WriteLine($"    INode table size: 0x{SB.INodesCount* SB.INodeSize/1024} kB");
        }
    }
}
