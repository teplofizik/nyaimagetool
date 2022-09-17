using Extension.Array;
using Extension.Packet;
using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.RomFs
{
    class RomFsReader : RawPacket, IFilesystemReader
    {
        Types.RmSuperblock Superblock;

        public RomFsReader(byte[] Data) : base(Data)
        {
            Superblock = new Types.RmSuperblock(Data, 0);
        }

        public RomFsReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename))
        {

        }

        private Types.RmNode GetRootNode() => new Types.RmNode(Raw, Superblock.SuperblockSize);

        internal Types.RmNode[] GetDirEntries(Types.RmNode Dir)
        {
            List<Types.RmNode> Nodes = new List<Types.RmNode>();

            if(Dir.FsNodeType == FilesystemItemType.Directory)
            {
                var Offset = Dir.SpecInfo;
                var Header = new Types.RmNode(Raw, Offset);
                while (true)
                {
                    Nodes.Add(Header);

                    Offset = Header.NextHeader;
                    if (Offset == 0)
                        break;

                    Header = new Types.RmNode(Raw, Offset);
                }
            }

            return Nodes.ToArray();
        }

        private Types.RmNode GetINodeByPath(string Path)
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
            var Node = GetINodeByPath(Path);
            if ((Node != null) && (Node.FsNodeType == FilesystemItemType.File))
            {
                var Offset = Node.getOffset() + Node.HeaderSize;

                return ReadArray(Offset, Node.Size);
            }

            return null;
        }

        public DeviceInfo ReadDevice(string Path)
        {
            var Node = GetINodeByPath(Path);

            if ((Node != null) && ((Node.FsNodeType == FilesystemItemType.Block) || (Node.FsNodeType == FilesystemItemType.Character)))
                return new DeviceInfo((Node.SpecInfo >> 16) & 0xffff, Node.SpecInfo & 0xffff);
            else
                return null;
        }

        public FilesystemEntry[] ReadDir(string Path)
        {
            var Node = GetINodeByPath(Path);
            if ((Node != null) && (Node.FsNodeType == FilesystemItemType.Directory))
            {
                var Entries = GetDirEntries(Node);
                var Res = new List<FilesystemEntry>();

                foreach (var E in Entries)
                {
                    var Size = ((E.FsNodeType == FilesystemItemType.File) || (E.FsNodeType == FilesystemItemType.SymLink)) ? E.Size : 0;

                    if ((E.Name != ".") && (E.Name != ".."))
                    {
                        var R = new FilesystemEntry(E.FsNodeType, Universal.Helper.FsHelper.CombinePath(Path, E.Name), 0, 0, E.Mode, Size);
                        Res.Add(R);
                    }
                }

                return Res.ToArray();
            }

            return new FilesystemEntry[] { };
        }

        public string ReadLink(string Path)
        {
            var Node = GetINodeByPath(Path);
            if ((Node != null) && (Node.FsNodeType == FilesystemItemType.SymLink))
            {
                var Offset = Node.getOffset() + Node.HeaderSize;
                var Data = ReadArray(Offset, Node.Size);

                return UTF8Encoding.UTF8.GetString(Data);
            }

            return null;
        }
    }
}
