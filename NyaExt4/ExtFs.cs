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

        internal Types.ExtSuperBlock SuperBlock  => new Types.ExtSuperBlock(Raw, 0x400);
        /// <summary>
        /// TODO: BlockSize is 0x40 in 64bit extension...
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        internal Types.ExtBlockGroup GetBlockGroup(uint Id) => new Types.ExtBlockGroup(Raw, 0x800 + Id * 0x20);

        protected virtual void Init()
        {

        }


        public virtual void DumpINodes()
        {

        }

        public virtual void Dump()
        {
            var SB = SuperBlock;
            var BG = GetBlockGroup(0);

            Console.WriteLine($"          Block size: 0x{SB.BlockSize:x04}");
            Console.WriteLine($"          INode size: 0x{0x80:x04}");
            Console.WriteLine($"         INode count: {SB.INodesCount}");

            Console.WriteLine($" INode table address: 0x{BG.INodeTableLo:x08}");
            Console.WriteLine($"    INode table size: 0x{SB.INodesCount* 0x80/1024} kB");
        }
    }
}
