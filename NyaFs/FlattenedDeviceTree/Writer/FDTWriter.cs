using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.FlattenedDeviceTree.Writer
{
    public class FDTWriter
    {
        FlattenedDeviceTree Tree;

        public FDTWriter(FlattenedDeviceTree Tree)
        {
            this.Tree = Tree;
        }

        public byte[] GetBinary()
        {
            Types.FDTCompilerState State = new Types.FDTCompilerState();
            var MemReserveBlock = GetMemReserveBlock();

            ProcessNode(State, Tree.Root);
            State.Struct.AddRange(new Types.FDTEnd().getPacket());

            byte[] StructBlock = State.Struct.ToArray();
            byte[] StringsBlock = State.Strings.ToArray();

            var Header = new Types.FDTHeader();

            // Calc block sizes
            uint HeaderSize = Convert.ToUInt32(Header.getLength());
            uint ResMemSize = Convert.ToUInt32(MemReserveBlock.Length);
            uint StructSize = Convert.ToUInt32(StructBlock.Length.GetAligned(4));
            uint StringSize = Convert.ToUInt32(StringsBlock.Length.GetAligned(4));

            // Calc block addresses
            uint ResMemAddr = HeaderSize;
            uint StructAddr = ResMemAddr + ResMemSize;
            uint StringAddr = StructAddr + StructSize;

            // Header
            Header.BootCpuIdPhys = Tree.CpuId;
            Header.MemReserveMap = ResMemAddr;
            Header.OffsetDtStruct = StructAddr;
            Header.OffsetDtStrings = StringAddr;
            Header.SizeDtStruct = StructSize;
            Header.SizeDtString = StringSize;
            Header.TotalSize = StringAddr + StringSize;

            byte[] Res = new byte[Header.TotalSize];
            Res.WriteArray(0, Header.getPacket(), Header.getLength());
            Res.WriteArray(ResMemAddr, MemReserveBlock, ResMemSize);
            Res.WriteArray(StructAddr, StructBlock, StructSize);
            Res.WriteArray(StringAddr, StringsBlock, StringSize);

            return Res;
        }


        private void ProcessNode(Types.FDTCompilerState State, NyaFs.FlattenedDeviceTree.Types.Node Node)
        {
            State.Struct.AddRange(new Types.FDTBeginNode(Node.Name).getPacket());
            foreach (var P in Node.Properties)
                State.Struct.AddRange(new Types.FDTProp(CompileString(State, P.Name), P.Value).getPacket());

            foreach (var N in Node.Nodes)
                ProcessNode(State, N);

            State.Struct.AddRange(new Types.FDTEndNode().getPacket());
        }

        private byte[] GetMemReserveBlock()
        {
            var Mems = Tree.ReserveMemory;
            int Size = Mems.Length * 16;
            byte[] Res = new byte[Size + 16];

            Res.Fill((byte)0);
            for(int i = 0; i < Mems.Length; i++)
            {
                int Offset = i * 16;

                Res.WriteUInt64BE(Offset, Mems[i].Address);
                Res.WriteUInt64BE(Offset + 8, Mems[i].Size);
            }

            return Res;
        }

        private uint CompileString(Types.FDTCompilerState State, string S)
        {
            if (State.StringCache.ContainsKey(S))
                return State.StringCache[S];
            else
            {
                byte[] RawString = new byte[S.Length + 1];
                RawString.WriteString(0, S, S.Length + 1);

                uint Offset = Convert.ToUInt32(State.Strings.Count);
                State.Strings.AddRange(RawString);

                State.StringCache[S] = Offset;
                return Offset;
            }
        }
    }
}
