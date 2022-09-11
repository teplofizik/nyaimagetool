using Extension.Array;
using Extension.Packet;
using NyaFs.Filesystem.Universal;
using NyaFs.ImageFormat.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.SquashFs
{
    class SquashFsBuilder : IFilesystemBuilder
    {
        private Compression.BaseCompressor Comp;

        /// <summary>
        /// Superblock
        /// </summary>
        private Types.SqSuperblock Superblock = new Types.SqSuperblock();

        /// <summary>
        /// Fragments
        /// </summary>
        private List<Types.SqFragmentBlockEntry> Fragments = new List<Types.SqFragmentBlockEntry>();

        /// <summary>
        /// Temporary Id table
        /// </summary>
        private List<uint> IdTable = new List<uint>();

        // Directory table, Export table

        /// <summary>
        /// Directory table
        /// </summary>

        private List<Builder.Node> Nodes = new List<Builder.Node>();

        public SquashFsBuilder(CompressionType Type)
        {
            InitCompressor(Type);
        }

        private void InitCompressor(CompressionType Type)
        {
            switch (Type)
            {
                case CompressionType.IH_COMP_LZMA:
                    Superblock.CompressionId = Types.SqCompressionType.Lzma;
                    Comp = new Compression.Lzma();
                    break;

                case CompressionType.IH_COMP_GZIP:
                    Superblock.CompressionId = Types.SqCompressionType.Gzip;
                    Comp = new Compression.Gzip();
                    break;

                case CompressionType.IH_COMP_LZ4:
                    Superblock.CompressionId = Types.SqCompressionType.Lz4;
                    Comp = new Compression.Lz4();
                    break;

                case CompressionType.IH_COMP_LZO:
                    Superblock.CompressionId = Types.SqCompressionType.Lzo;
                    Comp = new Compression.Lzo();
                    break;

                case CompressionType.IH_COMP_ZSTD:
                    Superblock.CompressionId = Types.SqCompressionType.Zstd;
                    Comp = new Compression.Zstd();
                    break;

                    //case Types.SqCompressionType.Xz:
                    //    Comp = new Compression.Xz();
                    //    break;
            }
        }

        private byte[] CompressBlock(byte[] Data)
        {
            var Compressed = Comp.Compress(Data);

            if (Compressed.Length >= Data.Length)
            {
                var Res = new byte[Data.Length + 2];
                Res.WriteUInt16(0, Convert.ToUInt32(Data.Length) | 0x8000);
                Res.WriteArray(2, Data, Data.Length);

                return Res;
            }
            else
            {
                var Res = new byte[Compressed.Length + 2];
                Res.WriteUInt16(0, Convert.ToUInt32(Compressed.Length));
                Res.WriteArray(2, Compressed, Compressed.Length);

                return Res;
            }
        }

        /// <summary>
        /// Add full data blocks of file...
        /// </summary>
        /// <param name="File"></param>
        /// <param name="Dst"></param>
        private void AddBlocks(List<byte> Dst, Builder.Nodes.File File)
        {
            var Blocks = File.GetBlocks();
            if (Blocks.Count > 0)
            {
                File.DataBlockOffset = Dst.Count;
                File.DataBlocksSizes = new uint[Blocks.Count];

                for(int i = 0; i < Blocks.Count; i++)
                {
                    var Compressed = Comp.Compress(Blocks[i]);
                    Dst.AddRange(Compressed);

                    File.DataBlocksSizes[i] = Convert.ToUInt32(Compressed.Length);
                }
            }
        }

        private void AppendData(List<byte> Dst)
        {
            foreach(var N in Nodes)
            {
                if(N.Type == Types.SqInodeType.BasicFile)
                {
                    var F = N as Builder.Nodes.File;

                    AddBlocks(Dst, F);
                }
            }
        }

        private void AppendFragmentTable(List<byte> Dst)
        {
            Superblock.FragmentTableStart = Convert.ToUInt64(Dst.Count);
            Builder.FragmentBlock FragmentBlock = new Builder.FragmentBlock(Dst.Count, Superblock.BlockSize);

            foreach (var N in Nodes)
            {
                if (N.Type == Types.SqInodeType.BasicFile)
                {
                    var F = N as Builder.Nodes.File;
                    var Fragment = F.GetFragment();

                    if (Fragment.Length > 0)
                    {
                        long Offset = 0;
                        F.FragmentRef = FragmentBlock.Write(Fragment, ref Offset);

                        while (Offset <= Fragment.Length)
                        {
                            if (FragmentBlock.IsFilled)
                            {
                                Dst.AddRange(CompressBlock(FragmentBlock.Data));

                                FragmentBlock = new Builder.FragmentBlock(Dst.Count, Superblock.BlockSize);
                            }

                            FragmentBlock.Write(Fragment, ref Offset);
                        }
                    }
                }
            }

            if (FragmentBlock.DataSize > 0)
                Dst.AddRange(CompressBlock(FragmentBlock.Data));
        }

        private void AppendIdTable(List<byte> Dst)
        {
            Superblock.IdTableStart = Convert.ToUInt64(Dst.Count);

            var Table = new Builder.IdTable(IdTable.ToArray());
            Dst.AddRange(Table.getPacket());
        }

        private void AppendDirectoryTable(List<byte> Dst)
        {
            Superblock.DirectoryTableStart = Convert.ToUInt64(Dst.Count);

            var DirTable = new Builder.FragmentBlock(0, Superblock.BlockSize);
            foreach (var N in Nodes)
            {
                if (N.Type == Types.SqInodeType.BasicDirectory)
                {
                    var D = N as Builder.Nodes.Dir;

                    var Data = D.GetEntries();
                    long Offset = 0;
                    D.EntriesRef = DirTable.Write(Data, ref Offset);

                    while (Offset <= Data.Length)
                    {
                        if (DirTable.IsFilled)
                        {
                            Dst.AddRange(CompressBlock(DirTable.Data));

                            DirTable = new Builder.FragmentBlock(Dst.Count, Superblock.BlockSize);
                        }

                        DirTable.Write(Data, ref Offset);
                    }
                }
            }
        }

        private void AppendINodeTable(List<byte> Dst)
        {
            Superblock.INodeTableStart = Convert.ToUInt64(Dst.Count);

            var NodeTable = new Builder.FragmentBlock(0, Superblock.BlockSize);
            foreach (var N in Nodes)
            {
                var Node = N.GetINode();
                var Data = Node.getPacket();

                long Offset = 0;
                N.Ref = NodeTable.Write(Data, ref Offset);

                while (Offset <= Data.Length)
                {
                    if (NodeTable.IsFilled)
                    {
                        Dst.AddRange(CompressBlock(NodeTable.Data));

                        NodeTable = new Builder.FragmentBlock(Dst.Count, Superblock.BlockSize);
                    }

                    NodeTable.Write(Data, ref Offset);
                }
            }
        }

        private void AppendExportTable(List<byte> Dst)
        {
            Superblock.ExportTableStart = Convert.ToUInt64(Dst.Count);

        }

        /// <summary>
        /// Write correct offsets to tables
        /// </summary>
        /// <param name="Image"></param>
        private void UpdateSuperBlock(byte[] Image)
        {
            var SB = new Types.SqSuperblock(Image, 0);

            SB.IdTableStart = Superblock.IdTableStart;
            SB.FragmentTableStart = Superblock.FragmentTableStart;
            SB.ExportTableStart = Superblock.ExportTableStart;
            SB.INodeTableStart = Superblock.INodeTableStart;
            SB.DirectoryTableStart = Superblock.DirectoryTableStart;

        }

        public byte[] GetFilesystemImage()
        {
            var Res = new List<byte>();
            Res.AddRange(Superblock.getPacket());

            AppendData(Res);
            AppendINodeTable(Res);
            AppendDirectoryTable(Res);
            AppendFragmentTable(Res);
            AppendExportTable(Res);
            AppendIdTable(Res);

            var Image = Res.ToArray();
            UpdateSuperBlock(Image);
            return Image;
        }

        private Builder.Nodes.Dir GetParentDirectory(string Path)
        {
            var Parent = Universal.Helper.FsHelper.GetParentDirPath(Path);

            foreach (var D in Nodes)
            {
                if (D.Path == Parent)
                    return D as Builder.Nodes.Dir;
            }

            return null;
        }

        // Build ID table
        private void PreprocessId(uint User, uint Group)
        {
            // Check user id
            if (!IdTable.Contains(User)) IdTable.Add(User);
            // Check group id
            if (!IdTable.Contains(Group)) IdTable.Add(Group);
        }

        private void AddNestedNode(string Path, Func<Builder.Node> NodeGetter)
        {
            var Parent = GetParentDirectory(Path);
            if (Parent != null)
            {
                var N = NodeGetter();

                PreprocessId(N.User, N.Group);

                Parent.AddEntry(new Builder.DirectoryEntry(Universal.Helper.FsHelper.GetName(Path), N));
                Nodes.Add(N);
            }
            else
                throw new InvalidOperationException($"Cannot add entry with path {Path}: no parent dir.");
        }

        public void Block(string Path, uint Major, uint Minor, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () => new Builder.Nodes.Block(Path, Mode, User, Group, Major, Minor));
        }

        public void Char(string Path, uint Major, uint Minor, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () => new Builder.Nodes.Char(Path, Mode, User, Group, Major, Minor));
        }

        public void Directory(string Path, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () => new Builder.Nodes.Dir(Path, Mode, User, Group));
        }

        public void Fifo(string Path, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () => new Builder.Nodes.Fifo(Path, Mode, User, Group));
        }

        public void File(string Path, byte[] Content, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () => new Builder.Nodes.File(Path, Mode, User, Group, Superblock.BlockSize, Content));
        }

        public void Socket(string Path, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () => new Builder.Nodes.Socket(Path, Mode, User, Group));
        }

        public void SymLink(string Path, string Target, uint User, uint Group, uint Mode)
        {
            AddNestedNode(Path, () => new Builder.Nodes.SymLink(Path, Mode, User, Group, Target));
        }
    }
}
