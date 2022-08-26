using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt2.Implementations
{
    public class Ext2Fs : ExtFs
    {
        private uint BlockSize;
        private uint INodesCount;
        private uint INodeTable;

        private uint INodeSize => 128;

        private uint INodeTableOffset => INodeTable * BlockSize;

        public Ext2Fs(byte[] Data) : base(Data) { }

        public Ext2Fs(string Filename) : base(Filename) { }

        protected override void Init()
        {
            base.Init();

            var SB = SuperBlock;
            var BG = BlockGroup;

            BlockSize = SB.BlockSize;
            INodesCount = SB.INodesCount;
            INodeTable = BG.INodeTableLo;
        }

        private Types.ExtINode GetINode(uint Id) => new Types.ExtINode(ReadArray(INodeTableOffset + INodeSize * (Id - 1), INodeSize));

        public override void Dump()
        {
            var SB = SuperBlock;
            var BG = BlockGroup;

            Console.WriteLine($"          Block size: 0x{BlockSize:x04}");
            Console.WriteLine($"         INode count: {INodesCount}");

            Console.WriteLine($" INode table address: 0x{INodeTableOffset:x08}");
            Console.WriteLine($"    INode table size: {INodesCount * BlockSize / 1024} kB");
        }

        public override void DumpINodes()
        {
            Console.WriteLine($"INode table:");
            for (uint i = 1; i < INodesCount; i++)
            {
                var N = GetINode(i);


                Console.WriteLine($"{i:0000}: m{N.Mode:x04} u:{N.UID} g:{N.GID} s:{N.SizeLo} l:{N.LinksCount} f:{N.Flags}");
            }
        }
    }
}
