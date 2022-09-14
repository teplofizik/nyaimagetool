using Extension.Array;
using Extension.Packet;
using NyaFs.Filesystem.Universal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NyaFs.Filesystem.SquashFs
{
    public class SquashFsReader : RawPacket, Universal.IFilesystemReader
    {
        private Compression.BaseCompressor Comp;
        private Dictionary<long, byte[]> MetadataTable = new Dictionary<long, byte[]>();

        Types.SqFragmentBlockEntry[] FragmentEntries = null;
        uint[] IdTable = null;

        public Types.SqCompressionType Compression => Superblock.CompressionId;

        public SquashFsReader(byte[] Data) : base(Data)
        {
            Init();
        }

        public SquashFsReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename))
        {

        }

        protected virtual void Init()
        {
            if(Superblock.IsCorrect)
            {
                InitCompressor();
                if(Superblock.IdTableStart != 0xfffffffffffffffful) ReadIdTable();
                if (Superblock.ExportTableStart != 0xfffffffffffffffful) ReadExportTable();
                if (Superblock.FragmentTableStart != 0xfffffffffffffffful) ReadFragmentTable();

                //var Root = GetRootDir();
                //var Entries = GetDirEntries(Root);

                //var Etc = ReadDir("lib/network/wwan");
                // var Init = Read("/init");
            }
        }

        private void ReadIdTable()
        {
            var IdBlocksCount = (Superblock.IdCount + 2047) / 2048;
            var Entries = new List<uint>();

            for (int i = 0; i < IdBlocksCount; i++)
            {
                var Offset = Convert.ToInt64(Superblock.IdTableStart) + 0x08 * i;
                var B = ReadMetadata((long)ReadUInt64(Offset), 0, Superblock.IdCount * 4);

                for (int e = 0; e < 2048; e++)
                {
                    var Entry = B.ReadUInt32(e * 0x04);

                    Entries.Add(Entry);
                    if (Entries.Count >= Superblock.IdCount)
                        break;
                }
            }

            IdTable = Entries.ToArray();
        }

        private void ReadExportTable()
        {
        }

        private void ReadFragmentTable()
        {
            var FragmentBlocksCount = (Superblock.FragmentEntryCount + 511) / 512;
            var Entries = new List<Types.SqFragmentBlockEntry>();

            for (int i = 0; i < FragmentBlocksCount; i++)
            {
                var Offset = Convert.ToInt64(Superblock.FragmentTableStart) + 0x08 * i;
                var B = ReadMetadata((long)ReadUInt64(Offset), 0, Superblock.FragmentEntryCount * 0x10);

                for (int e = 0; e < Superblock.FragmentEntryCount; e++)
                {
                    var Entry = new Types.SqFragmentBlockEntry(B, e * 0x10);

                    Entries.Add(Entry);
                }
            }

            FragmentEntries = Entries.ToArray();
        }

        private void InitCompressor()
        {
            switch(Superblock.CompressionId)
            {
                case Types.SqCompressionType.Lzma:
                    Comp = new Compression.Lzma();
                    break;
                case Types.SqCompressionType.Gzip:
                    Comp = (Superblock.Flags.HasFlag(Types.SqSuperblockFlags.COMPRESSOR_OPTIONS))
                        ? new Compression.Gzip(Raw, 0x60) 
                        : new Compression.Gzip();
                    break;

                case Types.SqCompressionType.Xz:
                    Comp = (Superblock.Flags.HasFlag(Types.SqSuperblockFlags.COMPRESSOR_OPTIONS))
                        ? new Compression.Xz(Raw, 0x60)
                        : new Compression.Xz();
                    break;

                case Types.SqCompressionType.Lz4:
                    if (!Superblock.Flags.HasFlag(Types.SqSuperblockFlags.COMPRESSOR_OPTIONS))
                        throw new System.IO.IOException("LZ4 compression algorithm does not have a required Compression Options metatable.");
                    Comp = new Compression.Lz4(Raw, 0x60);
                    break;

                case Types.SqCompressionType.Lzo:
                    Comp = (Superblock.Flags.HasFlag(Types.SqSuperblockFlags.COMPRESSOR_OPTIONS))
                        ? new Compression.Lzo(Raw, 0x60)
                        : new Compression.Lzo();
                    break;

                case Types.SqCompressionType.Zstd:
                    Comp = (Superblock.Flags.HasFlag(Types.SqSuperblockFlags.COMPRESSOR_OPTIONS))
                        ? new Compression.Zstd(Raw, 0x60)
                        : new Compression.Zstd();
                    break;

                default:
                    throw new System.IO.IOException($"Unsupported compression algorithm {Superblock.CompressionId}");
            }
        }

        private Types.Nodes.BasicDirectory GetRootDir() => GetNode(Superblock.RootINodeRef) as Types.Nodes.BasicDirectory;

        private Types.SqInode GetNode(Types.SqMetadataRef Ref)
        {
            var Metadata = ReadINodeMetadata(Ref, 0x10);
            var UnknownNode = new Types.SqInode(Metadata);

            switch(UnknownNode.InodeType)
            {
                case Types.SqInodeType.BasicDirectory:
                    {
                        Metadata = ReadINodeMetadata(Ref, 0x20);
                        var N = new Types.Nodes.BasicDirectory(Metadata);
                        Metadata = ReadINodeMetadata(Ref, N.INodeSize);
                        return new Types.Nodes.BasicDirectory(Metadata);
                    }
                case Types.SqInodeType.BasicFile:
                    {
                        Metadata = ReadINodeMetadata(Ref, 0x20);
                        var N = new Types.Nodes.BasicFile(Metadata, Superblock.BlockSize);
                        Metadata = ReadINodeMetadata(Ref, N.INodeSize);
                        return new Types.Nodes.BasicFile(Metadata, Superblock.BlockSize);
                    }
                case Types.SqInodeType.BasicSymlink:
                    {
                        Metadata = ReadINodeMetadata(Ref, 0x18);
                        var N = new Types.Nodes.BasicSymLink(Metadata);
                        Metadata = ReadINodeMetadata(Ref, N.INodeSize);
                        return new Types.Nodes.BasicSymLink(Metadata);
                    }

                case Types.SqInodeType.BasicBlockDevice:
                    Metadata = ReadINodeMetadata(Ref, 0x18);
                    return new Types.Nodes.BasicDevice(Metadata);

                case Types.SqInodeType.BasicCharDevice:
                    Metadata = ReadINodeMetadata(Ref, 0x18);
                    return new Types.Nodes.BasicDevice(Metadata);

                case Types.SqInodeType.BasicFifo:
                    Metadata = ReadINodeMetadata(Ref, 0x18);
                    return new Types.Nodes.BasicDevice(Metadata);

                case Types.SqInodeType.BasicSocket:
                    Metadata = ReadINodeMetadata(Ref, 0x18);
                    return new Types.Nodes.BasicIPC(Metadata);

                case Types.SqInodeType.ExtendedDirectory:
                    {
                        Metadata = ReadINodeMetadata(Ref, 0x28);
                        //var N = new Types.Nodes.ExtendedDirectory(Metadata);
                        //Metadata = ReadINodeMetadata(Ref, N.INodeSize);
                        return new Types.Nodes.ExtendedDirectory(Metadata);
                    }
                default: return UnknownNode;
            }
        }

        private byte[] ReadINodeMetadata(Types.SqMetadataRef Ref, long Size) => 
            ReadMetadata(Convert.ToInt64(Superblock.INodeTableStart) + Ref.Block, Ref.Offset, Size);

        /// <summary>
        /// Readout data from metadata blocks
        /// </summary>
        /// <param name="Address">Address of metadata block</param>
        /// <param name="Offset">Offset to data in unpacked block</param>
        /// <param name="Size">Size of data to readout</param>
        /// <returns></returns>
        private byte[] ReadMetadata(long Address, long Offset, long Size)
        {
            var Res = new byte[Size];
            long ResOffset = 0;
            while (true)
            {
                byte[] Uncompressed;
                uint Header = ReadUInt16(Address);
                long DataSize = Header & 0x7FFFu;

                if (MetadataTable.ContainsKey(Address))
                    Uncompressed = MetadataTable[Address];
                else
                {
                    bool IsCompressed = (Header & 0x8000) == 0;
                    byte[] Raw = ReadArray(Address + 2, DataSize);
                    Uncompressed = IsCompressed ? Comp.Decompress(Raw) : Raw;

                    MetadataTable[Address] = Uncompressed;
                }
                if ((Uncompressed.Length - Offset) < Size)
                {
                    long Copy = Uncompressed.Length - Offset;
                    Res.WriteArray(ResOffset, Uncompressed.ReadArray(Offset, Copy), Copy);

                    Size -= Copy;
                    ResOffset += Copy;
                    Offset = 0;
                }
                else if ((Uncompressed.Length - Offset) == Size)
                {
                    Res.WriteArray(ResOffset, Uncompressed.ReadArray(Offset, Size), Size);
                    break;
                }
                else
                {
                    Res.WriteArray(ResOffset, Uncompressed.ReadArray(Offset, Size), Size);
                    break;
                }

                Address += DataSize + 2;
            }

            return Res;
        }

        internal Types.SqDirectoryEntry[] GetDirEntries(Types.Nodes.ExtendedDirectory Dir)
        {
            var Raw = ReadMetadata(Convert.ToInt64(Superblock.DirectoryTableStart) + Dir.DirBlockStart, Dir.BlockOffset, Dir.FileSize);
            long Offset = 0;
            var DirEntries = new List<Types.SqDirectoryEntry>();

            while (Offset < Dir.FileSize - 3)
            {
                var DirHeader = new Types.SqDirectoryHeader(Raw, Offset);
                Offset += DirHeader.getLength();

                for (int i = 0; i < DirHeader.Count + 1; i++)
                {
                    var E = new Types.SqDirectoryEntry(DirHeader.INodeNumber, DirHeader.Start, Raw, Offset);
                    DirEntries.Add(E);

                    Offset += E.getLength();
                }
            }

            return DirEntries.ToArray();
        }

        internal Types.SqDirectoryEntry[] GetDirEntries(Types.Nodes.BasicDirectory Dir)
        { 
            var Raw = ReadMetadata(Convert.ToInt64(Superblock.DirectoryTableStart) + Dir.DirBlockStart, Dir.BlockOffset, Dir.FileSize - 3);
            long Offset = 0;
            var DirEntries = new List<Types.SqDirectoryEntry>();

            while (Offset < Dir.FileSize - 3)
            {
                var DirHeader = new Types.SqDirectoryHeader(Raw, Offset);
                Offset += DirHeader.getLength();

                for (int i = 0; i < DirHeader.Count + 1; i++)
                {
                    var E = new Types.SqDirectoryEntry(DirHeader.INodeNumber, DirHeader.Start, Raw, Offset);
                    DirEntries.Add(E);

                    Offset += E.getLength();
                }
            }
            
            return DirEntries.ToArray();
        }

        internal Types.SqInode GetINodeByPath(string Path)
        {
            if (Path.Length == 0)
                throw new ArgumentException($"{Path} is empty");

            var Root = GetRootDir();
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
                        var N = GetNode(I.Reference);
                        if (i == Parts.Length - 1)
                            return N;

                        if (N.InodeType == Types.SqInodeType.BasicDirectory)
                        {
                            Entries = GetDirEntries(N as Types.Nodes.BasicDirectory);
                            Found = true;
                            break;
                        }
                        else if (N.InodeType == Types.SqInodeType.ExtendedDirectory)
                        {
                            Entries = GetDirEntries(N as Types.Nodes.ExtendedDirectory);
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

        private byte[] GetINodeContent(Types.Nodes.BasicFile N)
        {
            // The offset from the start of the archive where the data blocks are stored
            var BlockOffset = N.FragmentBlockOffset;
            var BlockSizes = N.BlockSizes;
            var Res = new byte[N.FileSize];
            long Offset = 0;
            long SrcOffset = N.BlocksStart;

            for (int i = 0; i < BlockSizes.Length; i++)
            {
                var FragData = ReadArray(Convert.ToInt64(SrcOffset), BlockSizes[i]);
                var UncompressedData = Comp.Decompress(FragData);
                Res.WriteArray(Offset, UncompressedData, UncompressedData.Length);
                Offset += UncompressedData.Length;

                SrcOffset += BlockSizes[i];
            }

            if (N.FragmentBlockIndex != 0xffffffff)
            {
                var Frag = FragmentEntries[N.FragmentBlockIndex];
                var FragData = ReadArray(Convert.ToInt64(Frag.Start), Frag.Size);
                var UncompressedData = Frag.IsCompressed ? Comp.Decompress(FragData) : FragData;

                var OwnData = UncompressedData.ReadArray(N.FragmentBlockOffset, N.FragmentSize);
                Res.WriteArray(Offset, OwnData, OwnData.Length);
            }
            return Res;
        }

        private uint GetGID(uint GidId)
        {
            return IdTable[GidId];
        }

        private uint GetUID(uint UidId)
        {
            return IdTable[UidId];
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
                switch (Node.InodeType)
                {
                    case Types.SqInodeType.BasicFile:
                        return GetINodeContent(Node as Types.Nodes.BasicFile);
                    case Types.SqInodeType.ExtendedFile:
                        return null; // TODO
                    default:
                        return null;
                }
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
                switch (Node.InodeType)
                {
                    case Types.SqInodeType.BasicBlockDevice:
                    case Types.SqInodeType.BasicCharDevice:
                        {
                            var Block = Node as Types.Nodes.BasicDevice;
                            return new Universal.Types.DeviceInfo(Block.Major, Block.Minor);
                        }
                    case Types.SqInodeType.ExtendedBlockDevice:
                    case Types.SqInodeType.ExtendedCharDevice:
                        return null; // TODO
                    default:
                        return null;
                }
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
                switch (Node.InodeType)
                {
                    case Types.SqInodeType.BasicSymlink:
                        return (Node as Types.Nodes.BasicSymLink).Target;
                    default:
                        return null;
                }
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
                Types.SqDirectoryEntry[] Entries = null;

                if (DirNode.InodeType == Types.SqInodeType.BasicDirectory)
                    Entries = GetDirEntries(DirNode as Types.Nodes.BasicDirectory);
                else if (DirNode.InodeType == Types.SqInodeType.ExtendedDirectory)
                    Entries = GetDirEntries(DirNode as Types.Nodes.ExtendedDirectory);
                else
                    return null;

                foreach (var E in Entries)
                {
                    var N = GetNode(E.Reference);
                    var G = GetGID(N.GidIndex);
                    var U = GetUID(N.UidIndex);
                    // TODO: uid, gid conversion
                    switch (N.InodeType)
                    {
                        case Types.SqInodeType.BasicFile:
                            Res.Add(new FilesystemEntry(E.FsNodeType, Universal.Helper.FsHelper.CombinePath(Path, E.Name), U, G, N.Permissions, (N as Types.Nodes.BasicFile).FileSize));
                            break;
                        case Types.SqInodeType.BasicSymlink:
                            Res.Add(new FilesystemEntry(E.FsNodeType, Universal.Helper.FsHelper.CombinePath(Path, E.Name), U, G, N.Permissions, (N as Types.Nodes.BasicSymLink).TargetSize));
                            break;
                        default:
                            Res.Add(new FilesystemEntry(E.FsNodeType, Universal.Helper.FsHelper.CombinePath(Path, E.Name), U, G, N.Permissions, 0));
                            break;
                    }
                }

                return Res.ToArray();
            }
            else
                return null;
        }

        private Types.SqSuperblock Superblock => new Types.SqSuperblock(Raw, 0);
    }
}
