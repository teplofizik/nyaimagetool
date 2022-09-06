using Extension.Array;
using Extension.Packet;
using NyaFs.Filesystem.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Filesystem.Ext2
{
    public class Ext2FsReader : Ext2FsBase, Universal.IFilesystemReader
    {
        public Ext2FsReader(byte[] Data) : base(Data) { }

        public Ext2FsReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename)) { }

        private uint GetNodeSize(Types.ExtINode N)
        {
            switch(N.NodeType)
            {
                case Types.ExtINodeType.REG:
                case Types.ExtINodeType.LINK:
                    return N.SizeLo;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Read file by path
        /// </summary>
        /// <param name="Path">Path to file</param>
        /// <returns>Content of file or null if file is not exists</returns>
        public byte[] Read(string Path)
        {
            var Node = GetINodeByPath(Path);

            if (Node != null)
            {
                if (Node.NodeType == Types.ExtINodeType.REG)
                    return GetINodeContent(Node);
                else
                    return null;
            }
            else
                return null;
        }

        /// <summary>
        /// Read device information
        /// </summary>
        /// <param name="Path">Path to device</param>
        /// <returns>Device numbers (major/minor)</returns>
        public Universal.Types.DeviceInfo ReadDevice(string Path)
        {
            var Node = GetINodeByPath(Path);

            if (Node != null)
            {
                if ((Node.NodeType == Types.ExtINodeType.CHAR) || (Node.NodeType == Types.ExtINodeType.BLOCK))
                {
                    var Raw = Node.Block[0];
                    var Major = Raw & 0xff;
                    var Minor = (Raw >> 8) & 0xFf;
                    return new Universal.Types.DeviceInfo(Major, Minor); // TODO
                }
                else
                    return null;
            }
            else
                return null;
        }

        /// <summary>
        /// Read link content by path
        /// </summary>
        /// <param name="Path">Path to symlink</param>
        /// <returns>Link</returns>
        public string ReadLink(string Path)
        {
            var Node = GetINodeByPath(Path);

            if (Node != null)
            {
                if (Node.NodeType == Types.ExtINodeType.LINK)
                    return UTF8Encoding.UTF8.GetString(GetINodeContent(Node));
                else
                    return null;
            }
            else
                return null;
        }

        /// <summary>
        /// Read directory content
        /// </summary>
        /// <param name="Path">Path to directory</param>
        /// <returns>Array of entries</returns>
        public FilesystemEntry[] ReadDir(string Path)
        {
            var DirNode = GetINodeByPath(Path);

            if (DirNode != null)
            {
                var Res = new List<FilesystemEntry>();
                var Entries = GetDirEntries(DirNode);

                foreach(var E in Entries)
                {
                    if ((E.Name == ".") || (E.Name == "..")) continue;

                    var N = GetINode(E.INode);

                    //Console.WriteLine($"{E.Name} {E.INode}     {N}");
                    Res.Add(new Universal.FilesystemEntry(N.FsNodeType, Universal.Helper.FsHelper.CombinePath(Path, E.Name), N.UID, N.GID, N.Mode, GetNodeSize(N)));
                }

                return Res.ToArray();
            }
            else
                return null;
        }

        internal Types.ExtINode GetINodeByPath(string Path)
        {
            if (Path == ".") return GetRootDir();
            if (Path.Length == 0)
                throw new ArgumentException($"{Path} is empty");

            if (Path[0] == '/') Path = Path.Substring(1);

            var Parts = Path.Split("/");

            var Entries = GetDirEntries(GetRootDir());

            for (int i = 0; i < Parts.Length; i++)
            {
                var P = Parts[i];

                bool Found = false;
                foreach (var I in Entries)
                {
                    if (I.Name == P)
                    {
                        var N = GetINode(I.INode);
                        if (i == Parts.Length - 1)
                            return N;

                        if (N.NodeType == Types.ExtINodeType.DIR)
                        {
                            Entries = GetDirEntries(N);
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


        public void Dump()
        {
            var BG = GetBlockGroup(0);

            Console.WriteLine($"          Block size: 0x{Superblock.BlockSize:x04}");
            Console.WriteLine($"         INode count: {Superblock.INodesCount}");

            Console.WriteLine($"    INode table size: {Superblock.INodesCount * Superblock.BlockSize / 1024} kB");
            Console.WriteLine($"         Free inodes: {BG.FreeINodesCountLo}");
        }

        internal Types.ExtDirectoryEntry[] GetDirEntries(Types.ExtINode Dir)
        {
            if (Dir.NodeType != Types.ExtINodeType.DIR) throw new ArgumentException("Cannot read dir entries from non-dir INode");

            var DirContent = GetINodeContent(Dir);
            List<Types.ExtDirectoryEntry> Entries = new List<Types.ExtDirectoryEntry>();

            long Offset = 0;
            var Entry = new Types.ExtDirectoryEntry(DirContent, Offset);
            while(Entry.INode != 0)
            {
                Entries.Add(Entry);

                Offset += Entry.RecordLength;
                if (Offset == DirContent.Length)
                    break;

                Entry = new Types.ExtDirectoryEntry(DirContent, Offset);
            }

            return Entries.OrderBy(E => E.Name).ToArray();
        }

        internal Types.ExtINode GetRootDir()
        {
            for (uint i = 1; i < Superblock.INodesCount; i++)
            {
                var N = GetINode(i);

                if (N.NodeType == Types.ExtINodeType.DIR)
                    return N;
            }

            return null;
        }
    }
}
