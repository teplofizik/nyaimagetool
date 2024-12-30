using NyaIO.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsBmu.Types
{
    internal class BmuImage : RawPacket
    {
        public BmuImage(byte[] Data) : base(Data)
        {

        }

        public int ImagesCount => ReadByte(0x518);

        public byte[] GetImageByType(BmuImageType Type)
        {
            int Base = 0x51D;

            uint RawOffset = 0x800;
            for(int i = 0; i < ImagesCount; i++)
            {
                int Offset = Base + i * 5;

                int T = ReadByte(Offset);
                uint S = ReadUInt32BE(Offset + 1);

                if (T == Convert.ToInt32(Type))
                    return ReadArray(RawOffset, S);

                RawOffset += S;
            }

            return null;
        }

        public bool Correct => (getLength() > 0x800);
    }
}
