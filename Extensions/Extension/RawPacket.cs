using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Extension.Array;

namespace Extension.Packet
{
    public class RawPacket : RawPacketWrapper
    {
        /// <summary>
        /// Конструктор пакета по образцу
        /// </summary>
        /// <param name="Data"></param>
        public RawPacket(byte[] Data) : base(new byte[Data.Length], 0, Data.Length)
        {
            WriteArray(0, Data, Data.Length);
        }

        /// <summary>
        /// Конструктор пакета с указанием длины
        /// </summary>
        /// <param name="Length"></param>
        public RawPacket(long Length) : base (new byte[Length], 0, Length)
        {
            Fill(0);
        }
    }
}
