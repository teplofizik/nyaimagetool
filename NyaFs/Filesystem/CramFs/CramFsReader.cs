using Extension.Array;
using Extension.Packet;
using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs
{
    class CramFsReader : RawPacket, IFilesystemReader
    {
        private int BlockSize = 4096;
        private Types.CrSuperblock Superblock;
        private bool Loaded = false;

        public CramFsReader(byte[] Data) : base(Data)
        {
            Superblock = new Types.CrSuperblock(Data, 0);

            var Copy = ReadArray(0, Raw.Length);
            Copy.WriteUInt32(0x20, 0);
            var Crc = Crc32.CalcCrc(Copy);

            Loaded = (Crc == Superblock.FsIDCrc);
            if(!Loaded)
            {
                Log.Error(0, $"Invalid CRC in cramfs image header.");
            }
        }

        private Types.CrNode GetRootNode() => new Types.CrNode(Raw, 0x40);

        public CramFsReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename))
        {

        }

        internal Types.CrNode[] GetDirEntries(Types.CrNode Dir)
        {
            List<Types.CrNode> Nodes = new List<Types.CrNode>();
            
            long Offset = 0x00;
            while (Offset < Dir.Size)
            {
                var Node = new Types.CrNode(Raw, Dir.Offset + Offset);

                Nodes.Add(Node);
                Offset += Node.NodeSize;
            }

            return Nodes.ToArray();
        }

        private Types.CrNode GetINodeByPath(string Path)
        {
            if (Path.Length == 0)
                throw new ArgumentException($"{Path} is empty");

            var Root = GetRootNode();
            if ((Path == ".") || (Path == "/")) return Root;

            if (Path[0] == '/') Path = Path.Substring(1);
            var Parts = Path.Split("/");

            var Entries = GetDirEntries(Root);

            for (int i = 0; i < Parts.Length; i++)
            {
                var P = Parts[i];

                bool Found = false;
                foreach (var I in Entries)
                {
                    if (I.Name == P)
                    {
                        if (i == Parts.Length - 1)
                            return I;

                        if (I.FsNodeType == FilesystemItemType.Directory)
                        {
                            Entries = GetDirEntries(I);
                            Found = true;
                            break;
                        }
                        else
                            return null;
                    }
                }
                if (!Found)
                    return null;
            }

            return null;
        }


        public byte[] Read(string Path)
        {
            if (Loaded)
            {
                var Node = GetINodeByPath(Path);
                var Data = ReadFileData(Node.Offset, Node.Size);

                return Data;
            }
            else
                return null;
        }

        public DeviceInfo ReadDevice(string Path)
        {
            if (Loaded)
            {
                var Node = GetINodeByPath(Path);

                return new DeviceInfo(Node.Size >> 20, Node.Size & 0xFFFFF);
            }
            else
                return null;
        }

        public FilesystemEntry[] ReadDir(string Path)
        {
            if (Loaded)
            {
                var Node = GetINodeByPath(Path);
                if (Node != null)
                {
                    var Entries = GetDirEntries(Node);
                    var Res = new List<FilesystemEntry>();

                    foreach (var E in Entries)
                    {
                        var Size = ((E.FsNodeType == FilesystemItemType.File) || (E.FsNodeType == FilesystemItemType.SymLink)) ? E.Size : 0;

                        var R = new FilesystemEntry(E.FsNodeType, Universal.Helper.FsHelper.CombinePath(Path, E.Name), E.UId, E.GId, E.Mode, Size);
                        Res.Add(R);
                    }

                    return Res.ToArray();
                }
            }

            return new FilesystemEntry[] { };
        }

        public string ReadLink(string Path)
        {
            if (Loaded)
            {
                var Node = GetINodeByPath(Path);
                var Data = ReadFileData(Node.Offset, Node.Size);

                return UTF8Encoding.UTF8.GetString(Data);
            }
            else
                return null;
        }

        private byte[] ReadFileData(uint Offset, uint Size)
        {
            if (Loaded)
            {
                long BlockCount = (Size - 1) / BlockSize + 1;
                // Array of pointer to blocks end
                uint[] Pointers = ReadUInt32Array(Offset, BlockCount);

                var Res = new List<byte>();
                // Start of first block
                long Start = Offset + BlockCount * 4;
                for (long i = 0; i < BlockCount; i++)
                {
                    var Data = ReadArray(Start, Pointers[i] - Start);
                    var Uncompressed = Compression.Gzip.Decompress(Data);

                    Res.AddRange(Uncompressed);
                }

                return Res.ToArray();
            }
            else
                return null;
        }
    }
}
