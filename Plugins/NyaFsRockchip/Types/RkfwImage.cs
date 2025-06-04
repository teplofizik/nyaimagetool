using NyaIO.Data;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;

namespace NyaFsRockchip.Types
{
    internal class RkfwImage : RawPacket
    {
        public RkfwImage(byte[] Data) : base(Data)
        {

        }

        private uint Magic => ReadUInt32(0x00);
        private uint LoaderOffset => ReadUInt32(0x19);
        private uint LoaderLength => ReadUInt32(0x1D);
        private uint DataOffset => ReadUInt32(0x21);
        private uint DataLength => ReadUInt32(0x25);


        public bool Correct => (Magic == 0x57464B52U); // RKFW

        public RkfwHeader Header => new RkfwHeader(Raw);

        public byte[] FirmwareLoader => ReadArray(LoaderOffset, LoaderLength);

        public RkfwData FirmwareData => new RkfwData(Raw, DataOffset, DataLength);
    }
}
