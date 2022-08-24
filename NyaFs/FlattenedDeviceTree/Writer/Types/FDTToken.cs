using Extension.Array;
using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.FlattenedDeviceTree.Writer.Types
{
    class FDTToken : RawPacket
    {
        public FDTToken(int Size) : base(GetAlignedSize(Size)) { }

        protected static int GetAlignedSize(int UnpaddedSize) => UnpaddedSize.GetAligned(4);
    }

    class FDTBeginNode : FDTToken
    {
        public FDTBeginNode(string Name) : base(4 + Name.Length + 1)
        {
            WriteUInt32BE(0, 0x01);
            WriteString(4, Name, Name.Length + 1);
        }
    }

    class FDTEndNode : FDTToken
    {
        public FDTEndNode() : base(4) { WriteUInt32BE(0, 0x02); }
    }

    class FDTProp : FDTToken
    {
        public FDTProp(uint StringOffset, byte[] Data) : base(12 + Data.Length) { 
            WriteUInt32BE(0, 0x03);
            WriteUInt32BE(4, Convert.ToUInt32(Data.Length));
            WriteUInt32BE(8, StringOffset);
            WriteArray(12, Data, Data.Length);
        }
    }

    class FDTNop : FDTToken
    {
        public FDTNop() : base(4) { WriteUInt32BE(0, 0x03); }
    }

    class FDTEnd : FDTToken
    {
        public FDTEnd() : base(4) { WriteUInt32BE(0, 0x09); }
    }

}
