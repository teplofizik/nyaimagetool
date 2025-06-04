using NyaIO.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsRockchip.Types
{
    internal class RkfwData : NyaIO.Data.ArrayWrapper
    {
        private uint Offset;

        private RkfwUpdateFileHeader[] Headers = new RkfwUpdateFileHeader[16];

        public RkfwData(byte[] Raw, uint Offset, uint Size) : base(Raw, Offset, Size)
        {
            this.Offset = Offset;

            if(Correct)
            {
                uint RawOffset = 0x800;

                for (int i = 0; i < FileCount; i++)
                {
                    var H = LoadHeader(i);
                    H.RawOffset = RawOffset;

                    RawOffset += H.OrigFSize.GetAligned(0x800);

                    Headers[i] = H;
                }
            }
        }

        public uint Magic => ReadUInt32(0x00);

        public bool Correct => (Magic == 0x46414B52U) && (FileCount <= 16); // RKAF

        public uint ImgLen => ReadUInt32(0x04);

        public string Model => ReadString(0x08, 34);
        public string Id => ReadString(0x2A, 30);
        public string Manufacturer => ReadString(0x48, 56);

        public uint Unknown_1 => ReadUInt32(0x80);
        public uint Version => ReadUInt32(0x84);
        public uint FileCount => ReadUInt32(0x88);

        public RkfwUpdateFileHeader GetHeader(int Index) => ((Index >= 0) && (Index < Headers.Length)) ? Headers[Index] : null;

        private RkfwUpdateFileHeader LoadHeader(int Index) => new RkfwUpdateFileHeader(Raw, Convert.ToUInt32(Offset + 0x8C + Index * RkfwUpdateFileHeader.Size));

        public byte[] GetPartition(string Name)
        {
            for(int i = 0; i < FileCount; i++)
            {
                var H = GetHeader(i);

                if(H.Name == Name)
                {
                    return ReadArray(H.RawOffset, H.OrigFSize);
                }
            }

            return null;
        }
    }
}
