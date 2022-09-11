using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder
{
    class IdTable : RawPacket
    {
        public IdTable(uint[] Id) : base(2 + Id.Length * 4)
        {
            // Uncompressed data
            WriteUInt16(0, Convert.ToUInt32(0x8000u + Id.Length * 4));
            for (int i = 0; i < Id.Length; i++)
                WriteUInt32(2 + i * 4, Id[i]);
        }
    }
}
