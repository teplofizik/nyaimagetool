using Extension.Array;
using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.ImageFormat.Types.Android
{
    class AndroidImagev2 : AndroidImagev1
    {
        public AndroidImagev2(byte[] Raw) : base(Raw)
        {

        }

        /// <summary>
        /// dtb size in bytes 
        /// </summary>
        public uint DtboSize
        {
            get { return ReadUInt32(0x670); }
            set { WriteUInt32(0x670, value); }
        }

        /// <summary>
        /// dtb address
        /// </summary>
        public ulong DtboAddress
        {
            get { return ReadUInt64(0x674); }
            set { WriteUInt64(0x674, value); }
        }

        protected long DtboOffset => (RecoveryDtboOffset + RecoveryDtboSize).GetAligned(PageSize);

        public byte[] Dtbo => ReadArray(DtboOffset, DtboSize);
    }
}
