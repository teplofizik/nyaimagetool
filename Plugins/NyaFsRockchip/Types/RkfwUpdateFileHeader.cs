using NyaIO.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsRockchip.Types
{
    internal class RkfwUpdateFileHeader : ArrayWrapper
    {
        public const int Size = 112;

        public RkfwUpdateFileHeader(byte[] Raw, uint Offset) : base(Raw, Offset, Size)
        {

        }

        public uint RawOffset = 0;

        public string Name => ReadString(0, 32);
        public string FileName => ReadString(32, 60);

        public uint NandSize => ReadUInt32(92);
        public uint Pos => ReadUInt32(96);
        public uint NandAddr => ReadUInt32(100);
        public uint ImgFSize => ReadUInt32(104);
        public uint OrigFSize => ReadUInt32(108);

    }
}
