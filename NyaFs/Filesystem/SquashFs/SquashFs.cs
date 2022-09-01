using Extension.Packet;
using NyaFs.Filesystem.Universal;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs
{
    public class SquashFs : RawPacket, Universal.IFilesystemGetter
    {
        private Compression.BaseCompressor Comp;

        public SquashFs(byte[] Data) : base(Data)
        {
            Init();
        }

        public SquashFs(string Filename) : this(System.IO.File.ReadAllBytes(Filename))
        {

        }

        protected virtual void Init()
        {
            if(Superblock.IsCorrect)
            {
                InitCompressor();
                ReadIdTable();
                ReadExportTable();
                ReadINodeTable();
                ReadFragmentTable();

                var Root = GetRootDir();
                var Entries = GetDirEntries(Root);

                var Etc = ReadDir("/etc");
            }
        }

        private void ReadIdTable()
        {

        }

        private void ReadINodeTable()
        {

        }

        private void ReadExportTable()
        {

        }

        private void ReadFragmentTable()
        {

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
            var Raw = ReadMetadata(Convert.ToInt64(Superblock.INodeTableStart) + Ref.Block);
            var UnknownNode = new Types.SqInode(Raw, Ref.Offset);

            switch(UnknownNode.InodeType)
            {
                case Types.SqInodeType.BasicDirectory: return new Types.Nodes.BasicDirectory(Raw, Ref.Offset);
                default: return UnknownNode;
            }
        }
 
        private byte[] ReadMetadata(long Address)
        {
            uint Header = ReadUInt16(Address);
            long DataSize = Header & 0x7FFFu;
            bool IsCompressed = (Header & 0x8000) == 0;

            byte[] Raw = ReadArray(Address + 2, DataSize);

            return IsCompressed ? Comp.Decompress(Raw) : Raw;
        }

        internal Types.SqDirectoryEntry[] GetDirEntries(Types.Nodes.BasicDirectory Dir)
        {
            var Raw = ReadMetadata(Convert.ToInt64(Superblock.DirectoryTableStart) + Dir.DirBlockStart);
            long Offset = 0;
            var DirEntries = new List<Types.SqDirectoryEntry>();

            while (Offset < Dir.FileSize - 3)
            {
                var DirHeader = new Types.SqDirectoryHeader(Raw, Dir.BlockOffset + Offset);
                Offset += DirHeader.getLength();

                for (int i = 0; i < DirHeader.Count + 1; i++)
                {
                    var E = new Types.SqDirectoryEntry(DirHeader.Start, DirHeader.INodeNumber, Raw, Dir.BlockOffset + Offset);
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

            if (Path[0] == '/') Path = Path.Substring(1);

            var Parts = Path.Split("/");

            var Root = GetRootDir();
            if (Path == ".") return Root;

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

                        if (I.Type == Types.SqInodeType.BasicDirectory)
                        {
                            Entries = GetDirEntries(N as Types.Nodes.BasicDirectory);
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
            throw new NotImplementedException();
        }

        public FilesystemEntry[] ReadDir(string Path)
        {
            var DirNode = GetINodeByPath(Path);

            if ((DirNode != null) && (DirNode.InodeType == Types.SqInodeType.BasicDirectory))
            {
                var Res = new List<FilesystemEntry>();
                var Entries = GetDirEntries(DirNode as Types.Nodes.BasicDirectory);

                foreach (var E in Entries)
                {
                    var N = GetNode(E.Reference);
                    // TODO: size...
                    // TODO: uid, gid conversion
                    Res.Add(new FilesystemEntry(E.FsNodeType, Universal.Helper.FsHelper.CombinePath(Path, E.Name), N.UidIndex, N.GidIndex, N.Permissions, 0));
                }

                return Res.ToArray();
            }
            else
                return null;
        }

        private Types.SqSuperblock Superblock => new Types.SqSuperblock(Raw, 0);
    }
}
