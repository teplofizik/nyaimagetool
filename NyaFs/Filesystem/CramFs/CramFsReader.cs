using Extension.Packet;
using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs
{
    class CramFsReader : RawPacket, Universal.IFilesystemReader
    {
        private Types.CrSuperblock Superblock;

        public CramFsReader(byte[] Data) : base(Data)
        {
            Superblock = new Types.CrSuperblock(Data, 0);

        }

        public CramFsReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename))
        {

        }


        public byte[] Read(string Path)
        {
            throw new NotImplementedException();
        }

        public DeviceInfo ReadDevice(string Path)
        {
            throw new NotImplementedException();
        }

        public FilesystemEntry[] ReadDir(string Path)
        {
            throw new NotImplementedException();
        }

        public string ReadLink(string Path)
        {
            throw new NotImplementedException();
        }
    }
}
