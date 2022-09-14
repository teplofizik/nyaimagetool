using Extension.Array;
using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.ImageFormat.Types.Android
{
    class AndroidImagev1 : LegacyAndroidImage
    {
        public AndroidImagev1(byte[] Raw) : base(Raw)
        {

        }

        /// <summary>
        /// Recovery dtb size in bytes 
        /// </summary>
        public uint RecoveryDtboSize
        {
            get { return ReadUInt32(0x660); }
            set { WriteUInt32(0x660, value); }
        }

        /// <summary>
        /// Recovery dtb address
        /// </summary>
        public ulong RecoveryDtboAddress
        {
            get { return ReadUInt64(0x664); }
            set { WriteUInt64(0x664, value); }
        }

        public override long HeaderSize => ReadUInt32(0x66C);

        protected long RecoveryDtboOffset => (SecondOffset + SecondSize).GetAligned(PageSize);

        public byte[] RecoveryDtbo => ReadArray(RecoveryDtboOffset, RecoveryDtboSize);
    }
}
