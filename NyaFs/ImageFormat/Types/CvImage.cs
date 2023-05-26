using System;
using System.Collections.Generic;
using System.Text;
using Extension.Array;
using Extension.Packet;

namespace NyaFs.ImageFormat.Types
{
    class CvImage : RawPacket
    { 

        public CvImage(byte[] Data) : base(Data) { }
		public CvImage(string Filename) : base(System.IO.File.ReadAllBytes(Filename)) { }

        public CvImage(string Name, byte[] Data, UInt32 Flags) : base(Data.Length + 0x80) {
            // File header
            Magic = 0x474D4943;
            Version = 1;
            HeaderSize = 0x40;
            TotalChunk = 1;
            ContentSize = Convert.ToUInt32(0x40 + Data.Length);
            ImageName = Name;

            // Image header
            Unknown3 = 1;
            ImageSize = Convert.ToUInt32(Data.Length);
            Unknown4 = Flags;
            CRC32 = CalcCrc(Data);

            // Image data
            WriteArray(0x80, Data, Data.Length);
        }

        static UInt32 CalcCrc(byte[] data)
        {
            if (data != null)
            {
                var crc32 = new CrcSharp.Crc(new CrcSharp.CrcParameters(32, 0x04c11db7, 0xffffffff, 0xffffffff, true, true));

                return Convert.ToUInt32(crc32.CalculateAsNumeric(data));
            }
            else
                return 0;
        }

        public bool Correct => (Magic == 0x474D4943u) &&
                               (ImageSize + 0x40 == ContentSize) &&
                               (ContentSize + HeaderSize >= getLength()) &&
                               (CRC32 == CalculatedCRC32);

        /// <summary>
        /// Header magic CIMG (43 49 4D 47)
        /// </summary>
        public UInt32 Magic
        {
            get { return ReadUInt32(0x00); }
            set { WriteUInt32(0x00, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// Unknown field (01 00 00 00) - maybe images count
        /// </summary>
        public UInt32 Version
        {
            get { return ReadUInt32(0x04); }
            set { WriteUInt32(0x04, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// Header size (40 00 00 00)
        /// </summary>
        public UInt32 HeaderSize
        {
            get { return ReadUInt32(0x08); }
            set { WriteUInt32(0x08, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// Unknown field (01 00 00 00)
        /// </summary>
        public UInt32 TotalChunk
        {
            get { return ReadUInt32(0x0C); }
            set { WriteUInt32(0x0C, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// Content size - size of all images in file
        /// </summary>
        public UInt32 ContentSize
        {
            get { return ReadUInt32(0x10); }
            set { WriteUInt32(0x10, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// ImageName
        /// </summary>
        public string ImageName
        {
            get { return ReadANSIString(0x14, 0x2C); }
            set { WriteANSIString(0x14, value, 0x2C); }
        }


        /// <summary>
        /// Unknown field (01 00 00 00)
        /// </summary>
        public UInt32 Unknown3
        {
            get { return ReadUInt32(0x40); }
            set { WriteUInt32(0x40, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// Image size
        /// </summary>
        public UInt32 ImageSize
        {
            get { return ReadUInt32(0x44); }
            set { WriteUInt32(0x44, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// Unknown field (?? ?? ?? 00)
        /// </summary>
        public UInt32 Unknown4
        {
            get { return ReadUInt32(0x48); }
            set { WriteUInt32(0x48, Convert.ToUInt32(value)); }
        }


        /// <summary>
        /// Checksum
        /// </summary>
        public UInt32 CRC32
        {
            get { return ReadUInt32(0x4C); }
            set { WriteUInt32(0x4C, Convert.ToUInt32(value)); }
        }

        /// <summary>
        /// Data
        /// </summary>
        public byte[] Content
        {
            get
            {
                if (ImageSize + HeaderSize + 0x40 <= getLength())
                    return ReadArray(HeaderSize + 0x40, ImageSize);
                else
                    return null;
            }
        }

        /// <summary>
        /// Вычисленное значение CRC32
        /// </summary>
        public UInt32 CalculatedCRC32 => CalcCrc(Content);
    }
}
