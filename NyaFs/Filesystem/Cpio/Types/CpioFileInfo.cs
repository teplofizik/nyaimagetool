using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Cpio.Types
{
    class CpioFileInfo
    {
        private byte[] Raw;
        private long Offset;

        public CpioFileInfo(byte[] Raw, long Offset)
        {
            this.Raw = Raw;
            this.Offset = Offset;
        }

        public bool IsCorrectMagic => Magic == "070701";

        private UInt32 GetAsciiValue(long HeaderOffset, int Size)
        {
            var Text = Raw.ReadString(Offset + HeaderOffset, Size);
            return Convert.ToUInt32(Text, 16);
        }

        /// <summary>
        /// The string 070701 for new ASCII, the string 070702 for new ASCII with CRC
        /// </summary>
        public string Magic => Raw.ReadString(Offset, 6); 

        // https://developer.adobe.com/experience-manager/reference-materials/6-4/javadoc/org/apache/commons/compress/archivers/cpio/CpioArchiveEntry.html
        public UInt32 INode => GetAsciiValue(6, 8);
        public UInt32 Mode => GetAsciiValue(14, 8);
        public UInt32 UserId => GetAsciiValue(22, 8);
        public UInt32 GroupId => GetAsciiValue(30, 8);
        public UInt32 NumLink => GetAsciiValue(38, 8);
        public UInt32 ModificationTime => GetAsciiValue(46, 8);

        /// <summary>
        /// must be 0 for FIFOs and directories
        /// </summary>
        public UInt32 FileSize => GetAsciiValue(54, 8);


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

        public bool IsTrailer => Path == "TRAILER!!!";

        public long PathPadding => (110L + NameSize).MakeSizeAligned(4);
        public long HeaderWithPathSize => 110 + NameSize + PathPadding;

        public string Path => Raw.ReadString(Offset + 110, NameSize - 1);

        public long FilePadding => (HeaderWithPathSize + FileSize).MakeSizeAligned(4);

        public long FullFileBlockSize => HeaderWithPathSize + FileSize + FilePadding;

    }
}
