using NyaIO.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsAllwinner.Types
{
    internal class WtyImage : RawPacket
    {
        // https://github.com/Ithamar/awutils/blob/master/imagewty.h

        public WtyImage(byte[] Data) : base(Data)
        {

        }

        /// <summary>
        /// Идентификатор образа
        /// </summary>
        public string Magic
        {
            get { return ReadString(0x00, 8); }
            set { WriteString(0x00, value, 8); }
        }

        /// <summary>
        /// Версия заголовка
        /// </summary>
        public uint HeaderVersion
        {
            get { return ReadUInt32(0x08); }
            set { WriteUInt32(0x08, value); }
        }

        /// <summary>
        /// Размер заголовка
        /// </summary>
        public uint HeaderSize
        {
            get { return ReadUInt32(0x0C); }
            set { WriteUInt32(0x0C, value); }
        }

        /// <summary>
        /// Адрес в опер. памяти
        /// </summary>
        public uint RamBase
        {
            get { return ReadUInt32(0x10); }
            set { WriteUInt32(0x10, value); }
        }

        /// <summary>
        /// Версия формата IMAGEWTY_VERSION (0x100234)
        /// </summary>
        public uint Version
        {
            get { return ReadUInt32(0x14); }
            set { WriteUInt32(0x14, value); }
        }

        /// <summary>
        /// Размер образа полный (округ до 0x100?)
        /// </summary>
        public uint ImageSize
        {
            get { return ReadUInt32(0x18); }
            set { WriteUInt32(0x18, value); }
        }

        /// <summary>
        /// Размер заголовка образа, включая заполнение
        /// </summary>
        public uint ImageHeaderSize
        {
            get { return ReadUInt32(0x1C); }
            set { WriteUInt32(0x1C, value); }
        }

        /// <summary>
        /// Корректный ли образ
        /// </summary>
        public bool Correct => (Magic == "IMAGEWTY") && VersionIsSupported;

        /// <summary>
        /// Поддерживается ли версия заголовка
        /// </summary>
        private bool VersionIsSupported => (HeaderVersion == 0x0100) || (HeaderVersion == 0x0300);

        /// <summary>
        /// Смещение до данных заголовка образа
        /// </summary>
        private int ImageHeaderOffset => (HeaderVersion == 0x0300) ? 0x4 : 0x0;

        /// <summary>
        /// Размер заголовка образа, включая заполнение, ожидаемый
        /// </summary>
        private int ImageHeaderSizeEstimated => (HeaderVersion == 0x0300) ? 0x60 : 0x50;

        private ImageHeader Header => new ImageHeader(Raw, 0x20, ImageHeaderSizeEstimated, ImageHeaderOffset);

        public int GetDataOffset => 1024 * (Header.NumFiles + 1);

        public byte[] FirmwareData => null;

        public FileHeader[] FileHeaders
        {
            get
            {
                FileHeader[] Res = new FileHeader[Header.NumFiles];
                for(int i = 0; i < Header.NumFiles; i++)
                    Res[i] = GetFileHeader(i);

                return Res;
            }
        }

        private FileHeader GetFileHeader(int Index)
        {
            if (Index < Header.NumFiles)
                return new FileHeader(Raw, 1024 * (1 + Index), 0x400, HeaderVersion);
            else
                return null;
        }

        public byte[] GetPartition(int Index)
        {
            if ((Index >= 0) && (Index < FileHeaders.Length))
            {
                return GetContent(FileHeaders[Index]);
            }
            return null;
        }

        public byte[] GetPartition(string Name)
        {
            foreach(var FH in FileHeaders)
            {
                if(FH.Filename == Name)
                {
                    return GetContent(FH);
                }
            }

            int Index = 0;
            if (int.TryParse(Name, out Index))
            {
                if (Index < FileHeaders.Length)
                {
                    return GetContent(FileHeaders[Index]);
                }
            }

            return null;
        }

        public byte[] GetContent(FileHeader Header)
        {
            return ReadArray(Header.Offset, Header.OriginalLength);
        }

        public class ImageHeader : ArrayWrapper
        {
            private int DataOffset;

            public ImageHeader(byte[] Raw, int Offset, int Size, int DataOffset) : base(Raw, Offset, Size)
            {
                this.DataOffset = DataOffset;
            }

            public uint Pid => ReadUInt32(DataOffset + 0x00); /* USB peripheral ID (from image.cfg) */
            public uint Vid => ReadUInt32(DataOffset + 0x04); /* USB vendor ID (from image.cfg) */
            public uint HardwareId => ReadUInt32(DataOffset + 0x08);       /* Hardware ID (from image.cfg) */
            public uint FirmwareId => ReadUInt32(DataOffset + 0x0C);       /* Firmware ID (from image.cfg) */
            uint val1 => ReadUInt32(DataOffset + 0x10);
            uint val1024 => ReadUInt32(DataOffset + 0x14);
            public int NumFiles => (int)ReadUInt32(DataOffset + 0x18);     /* Total number of files embedded */
            uint val1024_2 => ReadUInt32(DataOffset + 0x1C);
            uint val0 => ReadUInt32(DataOffset + 0x20);
            uint val0_2 => ReadUInt32(DataOffset + 0x24);
            uint val0_3 => ReadUInt32(DataOffset + 0x28);
            uint val0_4 => ReadUInt32(DataOffset + 0x2C);
        }

        public class FileHeader : ArrayWrapper
        {
            uint HeaderVersion;

            public FileHeader(byte[] Raw, int Offset, int Size, uint HeaderVersion) : base(Raw, Offset, Size)
            {
                this.HeaderVersion = HeaderVersion;
            }

            public uint FilenameLen => ReadUInt32(0x00);
            public uint TotalHeaderSize => ReadUInt32(0x04);

            public string Maintype => ReadString(0x08, 0x08);
            public string Subtype => ReadString(0x10, 0x10);

            public uint Unknown => ReadUInt32(0x20);

            private int FieldOffset => (HeaderVersion == 0x300u) ? 0x100 : 0x00;

            public uint StoredLength => ReadUInt32(0x24 + FieldOffset);
            public uint OriginalLength => ReadUInt32(((HeaderVersion == 0x300u) ? 0x2C : 0x28) + FieldOffset);
            public uint Offset => ReadUInt32(((HeaderVersion == 0x300u) ? 0x34 : 0x2C) + FieldOffset);

            public string Filename => ReadString((HeaderVersion == 0x300u) ? 0x24 : 0x34, 0x100);
        }
    }
}
