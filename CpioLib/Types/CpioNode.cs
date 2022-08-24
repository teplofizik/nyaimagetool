using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Extension.Array;
using Extension.Packet;

namespace CpioLib.Types
{
    public class CpioNode : RawPacket
    {
        public static UInt32 MaxNodeId = 0;

        protected static long CalcPacketSize(string Path, int DataSize)
        {
            var HeaderSize = 110;
            var PathSize = Path.Length + 1;
            var HeaderWithPathAlighedSize = Convert.ToInt64(HeaderSize + PathSize).GetAligned(4);
            return (HeaderWithPathAlighedSize + DataSize).GetAligned(4);
        }

        public CpioNode(byte[] Raw) : base(Raw) { }

        public CpioNode(string Path, 
                        byte[] Data, 
                        DateTime ModTime, 
                        uint Mode,
                        uint RMajor = 0,
                        uint RMinor = 0,
                        uint Major = 8,
                        uint Minor = 1) 
            : base(CalcPacketSize(Path, Data.Length))
        {
            var PathBytes = UTF8Encoding.UTF8.GetBytes(Path);
            MaxNodeId++;

            WriteArray(0, UTF8Encoding.UTF8.GetBytes("070701"), 6); // Header
            SetAsciiValue(6, 8, MaxNodeId); // INode []
            SetAsciiValue(14, 8, Mode); // Mode  [0x41ed dir, 0x81a4 file]

            SetAsciiValue(22, 8, 0); // UserId
            SetAsciiValue(30, 8, 0); // GroupId
            SetAsciiValue(38, 8, 1); // NumLink ???
            SetAsciiValue(46, 8, GetUnixTimestamp(ModTime)); // ModificationTime
            SetAsciiValue(54, 8, Convert.ToUInt32(Data.Length)); // FileSize
            SetAsciiValue(62, 8, Major); // Major
            SetAsciiValue(70, 8, Minor); // Minor
            SetAsciiValue(78, 8, RMajor); // RMajor
            SetAsciiValue(86, 8, RMinor); // RMinor
            SetAsciiValue(94, 8, Convert.ToUInt32(PathBytes.Length + 1)); // NameSize
            WriteArray(110, PathBytes, PathBytes.Length);
            WriteArray(HeaderWithPathSize, Data, Data.Length);
        }

        public CpioNode UpdateContent(byte[] Data)
        {
            var NewDataSize = HeaderWithPathSize + Data.Length;
            var NewRawSize = NewDataSize.GetAligned(4);

            var NewRaw = new byte[NewRawSize];
            var Header = ReadArray(0, HeaderWithPathSize);
            Array.Copy(Header, NewRaw, HeaderWithPathSize);
            Array.Copy(Data, 0, NewRaw, HeaderWithPathSize, Data.Length);

            var File = new CpioNode(NewRaw);
            File.FileSize = Convert.ToUInt32(Data.Length);

            return File;
        }

        public byte[] Content => ReadArray(HeaderWithPathSize, FileSize);

        public bool IsCorrectMagic => Magic == "070701";

        private UInt32 GetAsciiValue(long HeaderOffset, int Size)
        {
            var Text = ReadString(HeaderOffset, Size);
            return Convert.ToUInt32(Text, 16);
        }

        private void SetAsciiValue(long HeaderOffset, int Size, UInt32 value)
        {
            var Text = $"{value:X08}";
            var Array = UTF8Encoding.UTF8.GetBytes(Text);
            WriteArray(HeaderOffset, Array, Size);
        }

        /// <summary>
        /// The string 070701 for new ASCII, the string 070702 for new ASCII with CRC
        /// </summary>
        public string Magic => ReadString(0, 6);

        // https://developer.adobe.com/experience-manager/reference-materials/6-4/javadoc/org/apache/commons/compress/archivers/cpio/CpioArchiveEntry.html
        public UInt32 INode => GetAsciiValue(6, 8);
        public UInt32 Mode
        {
            get
            {
                return GetAsciiValue(14, 8);
            }
            set
            {
                SetAsciiValue(14, 8, value);
            }
        }

        public UInt32 HexMode
        {
            get
            {
                var M = Mode & 0xFFFU;
                uint Res = 0;
                Res |= (M & 0x7);
                Res |= ((M >> 3) & 0x7) << 4;
                Res |= ((M >> 6) & 0x7) << 8;
                Res |= ((M >> 9) & 0x7) << 12;

                return Res;
            }
            set
            {
                var M = Mode & ~0x1FFU;
                M |= (value & 0x7);
                M |= ((value >> 4) & 0x7) << 3;
                M |= ((value >> 8) & 0x7) << 6;
                M |= ((value >> 12) & 0x7) << 9;

                Mode = M;
            }
        }

        public string StrMode
        {
            get
            {
                // S: sticky 1 << 12, SGID: 13 SUID 14
                var Res = "";
                for (int i = 0; i < 3; i++)
                {
                    UInt32 Part = (HexMode >> (2 - i) * 4) & 0xF;

                    Res += ((Part & 0x04) != 0) ? "r" : "-";
                    Res += ((Part & 0x02) != 0) ? "w" : "-";
                    Res += ((Part & 0x01) != 0) ? ((((HexMode >> 12 >> (2 - i)) & 0x1) != 1) ? "x" : "s") : "-";
                }
                return Res;
            }
            set
            {
                {
                    if ((value != null) && (value.Length == 9))
                    {
                        UInt32 ModeX = 0;
                        for (int i = 0; i < 3; i++)
                        {
                            int Offset = i * 3;

                            var R = value[Offset + 0];
                            var W = value[Offset + 1];
                            var X = value[Offset + 2];

                            if (R == 'r') ModeX |= 4U << ((2 - i) * 4);
                            if (W == 'w') ModeX |= 2U << ((2 - i) * 4);
                            if (X == 'x') 
                                ModeX |= 1U << ((2 - i) * 4);
                            else if (X == 's')
                            {
                                ModeX |= 1U << ((2 - i) * 4);
                                ModeX |= 1U << 12 << (2 - i);
                            }

                        }
                        HexMode = ModeX;
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid file mode: {value}");
                    }
                }
            }
        }
    

        public UInt32 UserId
        {
            get
            {
                return GetAsciiValue(22, 8);
            }
            set
            {
                SetAsciiValue(22, 8, value);
            }
        }
        public UInt32 GroupId
        {
            get
            {
                return GetAsciiValue(30, 8);
            }
            set
            {
                SetAsciiValue(30, 8, value);
            }
        }

        public UInt32 NumLink
        {
            get
            {
                return GetAsciiValue(38, 8);
            }
            set
            {
                SetAsciiValue(38, 8, value);
            }
        }

        public UInt32 ModificationTime
        {
            get
            {
                return GetAsciiValue(46, 8);
            }
            set
            {
                SetAsciiValue(46, 8, value);
            }
        }

        /// <summary>
        /// must be 0 for FIFOs and directories
        /// </summary>
        public UInt32 FileSize
        {
            get { return GetAsciiValue(54, 8); }
            set { SetAsciiValue(54, 8, value); }
        }


        public UInt32 Major => GetAsciiValue(62, 8);
        public UInt32 Minor => GetAsciiValue(70, 8);

        /// <summary>
        /// only valid for chr and blk special files
        /// </summary>
        public UInt32 RMajor => GetAsciiValue(78, 8);

        /// <summary>
        /// only valid for chr and blk special files
        /// </summary>
        public UInt32 RMinor => GetAsciiValue(86, 8);

        /// <summary>
        /// count includes terminating NUL in pathname
        /// </summary>
        public UInt32 NameSize => GetAsciiValue(94, 8);

        /// <summary>
        /// 0 for "new" portable format; for CRC format, the sum of all the bytes in the file
        /// </summary>
        public UInt32 Check => GetAsciiValue(102, 8);

        public CpioModeFlags Flags => (CpioModeFlags)(Mode & 0xFFF);

        public CpioModeFileType FileType => (CpioModeFileType)(Mode & 0x3F000);

        public bool IsTrailer => Path == "TRAILER!!!";

        public long PathPadding => (110L + NameSize).MakeSizeAligned(4);
        public long HeaderWithPathSize => 110 + NameSize + PathPadding;

        public string Path => ReadString(110, Convert.ToInt32(NameSize - 1));

        public long FilePadding => (HeaderWithPathSize + FileSize).MakeSizeAligned(4);

        public long FullFileBlockSize => HeaderWithPathSize + FileSize + FilePadding;

        public override string ToString()
        {
            return $"CPIO: {Path} (sz: {FileSize}, u: {UserId}, g: {GroupId}, m: {StrMode})";
        }

        protected UInt32 GetUnixTimestamp(DateTime T) => Convert.ToUInt32(((DateTimeOffset)T).ToUnixTimeSeconds());
    }
}
