using Extension.Array;
using Extension.Packet;
using NyaFs.Filesystem.Universal;
using NyaFs.ImageFormat.Types;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Metadata blocks. Nodes must be grouped by directory
        /// </summary>
        private List<Builder.Node[]> NodesBlocks = new List<Builder.Node[]>();

        /// <summary>
        /// Export metablocks
        /// </summary>
        private List<uint> ExportBlocks = new List<uint>();

        List<uint> FragmentTablesList = new List<uint>();

        private byte[] FragmentBlock;

        private ulong FragmentDataStart = 0;

        private uint MetadataBlockSize = 0x2000u;
        private uint MaxNodesPerMetadataBlock = 200;

        public SquashFsBuilder(CompressionType Type)
        {
            Superblock.Flags |= Types.SqSuperblockFlags.UNCOMPRESSED_INODES;
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

        private uint GetIdIndex(uint Id)
        {
            for(int i = 0; i < IdTable.Count; i++)
            {
                if (IdTable[i] == Id)
                    return Convert.ToUInt32(i);
            }

            throw new ArgumentException("Unknown user/group id!");
        }

        private void PreprocessNodes()
        {
            var NewNodesList = new List<Builder.Node>();
            var NewNodes = new List<Builder.Node>();
            NewNodes.Add(Nodes[0]);

            // Process UID/GID
            foreach (var N in Nodes)
            {
                N.UId = GetIdIndex(N.User);
                N.GId = GetIdIndex(N.Group);
            }

            foreach (var N in Nodes)
            {
                if (N.Type == Types.SqInodeType.BasicDirectory)
                {
                    var D = N as Builder.Nodes.Dir;

                    // Check 
                    if(NewNodes.Count + D.Entries.Count > MaxNodesPerMetadataBlock)
                    {
                        var Arr = NewNodes.ToArray();
                        NewNodesList.AddRange(Arr);
                        NodesBlocks.Add(Arr);

                        NewNodes.Clear();
                    }
                    foreach(var DN in D.Entries)
                        NewNodes.Add(DN.Node);
                }
            }

            if(NewNodes.Count > 0)
            {
                var Arr = NewNodes.ToArray();
                NewNodesList.AddRange(Arr);
                NodesBlocks.Add(Arr);

                for(int i = 0; i < NewNodes.Count; i++)
                {
                    NewNodes[i].Index = Convert.ToUInt32(i + 1);
                }
            }

            Nodes = NewNodesList;
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

        private void PrepareFragmentTable()
        {
            var Dst = new List<byte>();

            // Write fragment content
            uint Start = Convert.ToUInt32(Dst.Count);
            var Writer = new Builder.MetadataWriter(Dst, Start, Superblock.BlockSize, Comp, false);
            foreach (var N in Nodes)
            {
                if (N.Type == Types.SqInodeType.BasicFile)
                {
                    var F = N as Builder.Nodes.File;
                    var Fragment = F.GetFragment();

                    if ((Fragment != null) && (Fragment.Length > 0))
                    {
                        F.FragmentIndex = Convert.ToUInt32(Fragments.Count);
                        F.FragmentRef = Writer.Write(Fragment);

                        uint Pos = Convert.ToUInt32(Dst.Count);
                        if (Pos != Start)
                        {
                            Fragments.Add(new Types.SqFragmentBlockEntry(Start, Pos - Start, true));
                            Start = Pos;
                        }
                    }
                }
            }
            Writer.Flush();

            {
                uint Pos = Convert.ToUInt32(Dst.Count);
                if (Pos != Start)
                {
                    Fragments.Add(new Types.SqFragmentBlockEntry(Start, Pos - Start, true));
                }
            }

            FragmentBlock = Dst.ToArray();
        }

        private void AppendFragmentTable(List<byte> Dst)
        {
            System.Diagnostics.Debug.WriteLine($"AppendFragmentTable data: {Dst.Count:x06}");
            // Write fragment tables list
            FragmentDataStart = Convert.ToUInt64(Dst.Count);
            Dst.AddRange(FragmentBlock);

            var Writer = new Builder.MetadataWriter(Dst, 0, MetadataBlockSize, Comp);
            uint LastTable = Convert.ToUInt32(Dst.Count);
            FragmentTablesList.Add(LastTable);
            //foreach (var F in Fragments)
            for (int i = 0; i < Fragments.Count; i++)
            {
                var F = Fragments[i];
                F.Start += FragmentDataStart;
                var FragmentRef = Writer.Write(F.getPacket());

                System.Diagnostics.Debug.WriteLine($"Fragment {i}: {F.Start:x08} size: {F.Size:x04}");
                uint Pos = Convert.ToUInt32(Dst.Count);
                if (Pos != LastTable)
                {
                    LastTable = Pos;
                    FragmentTablesList.Add(LastTable);
                }
            }
            Writer.Flush();

            System.Diagnostics.Debug.WriteLine($"AppendFragmentTable table: {Dst.Count:x06}");
            Superblock.FragmentTableStart = Convert.ToUInt64(Dst.Count);
            var Temp = new byte[8];
            foreach (var T in FragmentTablesList)
            {
                Temp.WriteUInt64(0, T);
                Dst.AddRange(Temp);
            }
        }

        private void AppendIdTable(List<byte> Dst)
        {
            System.Diagnostics.Debug.WriteLine($"AppendIdTable data: {Dst.Count:x06}");

            uint MdAddress = Convert.ToUInt32(Dst.Count);
            var Table = new Builder.IdTable(IdTable.ToArray());
            Dst.AddRange(Table.Data);

            System.Diagnostics.Debug.WriteLine($"AppendIdTable table: {Dst.Count:x06}");
            Superblock.IdTableStart = Convert.ToUInt64(Dst.Count);
            byte[] Temp = new byte[8];
            Temp.WriteUInt64(0, MdAddress);
            Dst.AddRange(Temp);

        }

        private void AppendDirectoryTable(List<byte> Dst)
        {
            System.Diagnostics.Debug.WriteLine($"AppendDirectoryTable data: {Dst.Count:x06}");
            Superblock.DirectoryTableStart = Convert.ToUInt64(Dst.Count);

            var Temp = new List<byte>();
            Builder.MetadataWriter Writer = new Builder.MetadataWriter(Temp, 0, MetadataBlockSize, Comp);
            Writer.FullBlocks = true;
            foreach (var N in Nodes)
            {
                if (N.Type == Types.SqInodeType.BasicDirectory)
                {
                    var D = N as Builder.Nodes.Dir;

                    D.EntriesRef = Writer.Write(D.GetEntries());
                }
            }

            Writer.Flush();
            Dst.AddRange(Temp.ToArray());

            // Update dirs data refs
            foreach (var N in Nodes)
            {
                if (N.Type == Types.SqInodeType.BasicDirectory)
                {
                    var D = N as Builder.Nodes.Dir;

                    // Fix reference
                    var DE = new Types.Nodes.BasicDirectory(ReadUncompressedMetadata(Dst, Superblock.INodeTableStart, D.Ref, 0x20));

                    DE.DirBlockStart = Convert.ToUInt32(D.EntriesRef.MetadataOffset);
                    DE.BlockOffset = Convert.ToUInt32(D.EntriesRef.UnpackedOffset);

                    System.Diagnostics.Debug.WriteLine($"Directory node {N.Index} entry data: block={DE.DirBlockStart:x06} offset={DE.BlockOffset:x06}");

                    WriteUncompressedMetadata(Dst, Superblock.INodeTableStart, D.Ref, DE.getPacket());
                }
            }
        }

        private byte[] ReadUncompressedMetadata(List<byte> Dst, ulong Start, Builder.MetadataRef Ref, uint Length)
        {
            int Offset = Convert.ToInt32(Start + Ref.MetadataOffset + 2 + Ref.UnpackedOffset);
            var Res = new byte[Length];
            for (int i = 0; i < Length; i++)
                Res[i] = Dst[Offset + i];

            return Res;
        }

        private void WriteUncompressedMetadata(List<byte> Dst, ulong Start, Builder.MetadataRef Ref, byte[] Data)
        {
            int Offset = Convert.ToInt32(Start + Ref.MetadataOffset + 2 + Ref.UnpackedOffset);
            for (int i = 0; i < Data.Length; i++)
                Dst[Offset + i] = Data[i];
        }

        private void AppendINodeTable(List<byte> Dst)
        {
            Superblock.INodeTableStart = Convert.ToUInt64(Dst.Count);
            System.Diagnostics.Debug.WriteLine($"AppendINodeTable: {Dst.Count:x06}");

            var Temp = new List<byte>();
            Builder.MetadataWriter Writer = new Builder.MetadataWriter(Temp, 0, MetadataBlockSize, null);
            for (int b = 0; b < NodesBlocks.Count; b++)
            {
                System.Diagnostics.Debug.WriteLine($"Node block {b}: offset {Temp.Count:x06}");
                var BNodes = NodesBlocks[b];
                for (int i = 0; i < BNodes.Length; i++)
                {
                    var Node = BNodes[i].GetINode();
                    Node.INodeNumber = BNodes[i].Index;

                    var Data = Node.getPacket();

                    BNodes[i].Ref = Writer.Write(Data);
                    System.Diagnostics.Debug.WriteLine($"Node {Node.INodeNumber}: offset {BNodes[i].Ref.MetadataOffset} unp: {BNodes[i].Ref.UnpackedOffset}");
                }

                Writer.Flush();
            }

            Writer.Flush();
            Dst.AddRange(Temp.ToArray());
        }

        private void AppendExportTable(List<byte> Dst)
        {
            System.Diagnostics.Debug.WriteLine($"AppendExportTable data: {Dst.Count:x06}");

            // Add export data [inode table]
            var Writer = new Builder.MetadataWriter(Dst, 0, 0x8000, Comp);
            uint Base = Convert.ToUInt32(Dst.Count);
            ExportBlocks.Add(Base);
            foreach (var N in Nodes)
            {
                Writer.Write(N.Ref.BytesValue);

                uint Pos = Convert.ToUInt32(Dst.Count);
                if (Pos != Base)
                {
                    Base = Pos;
                    ExportBlocks.Add(Base);
                }
            }

            Writer.Flush();

            System.Diagnostics.Debug.WriteLine($"AppendExportTable table: {Dst.Count:x06}");
            Superblock.ExportTableStart = Convert.ToUInt64(Dst.Count);

            byte[] Temp = new byte[8];
            for (int i = 0; i < ExportBlocks.Count; i++)
            {
                Temp.WriteUInt64(0, ExportBlocks[i]);

                Dst.AddRange(Temp);
            }
        }

        /// <summary>
        /// Write correct offsets to tables
        /// </summary>
        /// <param name="Image"></param>
        private void UpdateSuperBlock(byte[] Image)
        {
            var SB = new Types.SqSuperblock(Image, 0);

            var RootDirRef = Nodes.First().Ref;

            SB.BytesUsed = Convert.ToUInt32(Image.Length);
            SB.RootINodeRef = RootDirRef.SqRef;
            SB.IdCount = Convert.ToUInt32(IdTable.Count);
            SB.INodeCount = Convert.ToUInt32(Nodes.Count);
            SB.IdTableStart = Superblock.IdTableStart;
            SB.XAttrIdTableStart = 0xfffffffffffffffful;
            SB.FragmentTableStart = Superblock.FragmentTableStart;
            SB.FragmentEntryCount = Convert.ToUInt32(Fragments.Count);
            SB.ExportTableStart = Superblock.ExportTableStart;
            SB.INodeTableStart = Superblock.INodeTableStart;
            SB.DirectoryTableStart = Superblock.DirectoryTableStart;
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
            if ((Parent != null) || (Path == "/"))
            {
                var N = NodeGetter();
                N.Index = Convert.ToUInt32(Nodes.Count + 1);

                PreprocessId(N.User, N.Group);

                if(N.Type == Types.SqInodeType.BasicDirectory)
                {
                    var D = N as Builder.Nodes.Dir;
                    D.Parent = (Parent != null) ? Parent.Index : 1;
                }

                if (Parent != null)
                    Parent.AddEntry(new Builder.DirectoryEntry(Universal.Helper.FsHelper.GetName(Path), N));

                Nodes.Add(N);
            }
            else
                throw new InvalidOperationException($"Cannot add entry with path {Path}: no parent dir.");
        }

        /// <summary>
        /// Get builded filesystem image
        /// </summary>
        /// <returns></returns>
        public byte[] GetFilesystemImage()
        {
            var Res = new List<byte>();
            Res.AddRange(Superblock.getPacket());

            PreprocessNodes();
            PrepareFragmentTable();

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


        /// <summary>
        /// Create block device
        /// </summary>
        /// <param name="Path">Path to block device</param>
        /// <param name="Major">Major number</param>
        /// <param name="Minor">Minor number</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Block(string Path, uint Major, uint Minor, uint User, uint Group, uint Mode) => 
            AddNestedNode(Path, () => new Builder.Nodes.Block(Path, Mode, User, Group, Major, Minor));

        /// <summary>
        /// Create char device
        /// </summary>
        /// <param name="Path">Path to char device</param>
        /// <param name="Major">Major number</param>
        /// <param name="Minor">Minor number</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Char(string Path, uint Major, uint Minor, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.Char(Path, Mode, User, Group, Major, Minor));

        /// <summary>
        /// Create directory
        /// </summary>
        /// <param name="Path">Path to directory (parent dir must exists)</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Directory(string Path, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.Dir(Path, Mode, User, Group));

        /// <summary>
        /// Create fifo
        /// </summary>
        /// <param name="Path">Path to fifo</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Fifo(string Path, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.Fifo(Path, Mode, User, Group));

        /// <summary>
        /// Create file
        /// </summary>
        /// <param name="Path">Path to file</param>
        /// <param name="Content">File content</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void File(string Path, byte[] Content, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.File(Path, Mode, User, Group, Superblock.BlockSize, Content));

        /// <summary>
        /// Create socket
        /// </summary>
        /// <param name="Path">Path to socket</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Socket(string Path, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.Socket(Path, Mode, User, Group));

        /// <summary>
        /// Create symlink
        /// </summary>
        /// <param name="Path">Path to symlink</param>
        /// <param name="Target">Target path</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void SymLink(string Path, string Target, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.SymLink(Path, Mode, User, Group, Target));
    }
}
