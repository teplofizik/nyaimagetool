using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaExt4
{
    public class ExtFs : RawPacket
    {
        public ExtFs(byte[] Data) : base(Data)
        {

        }
        public ExtFs(string Filename) : this(System.IO.File.ReadAllBytes(Filename))
        {

        }

        public Types.ExtSuperBlock SuperBlock  => new Types.ExtSuperBlock(ReadArray(0x400, 0x400));
    }
}
