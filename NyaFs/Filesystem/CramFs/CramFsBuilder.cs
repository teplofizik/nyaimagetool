using Extension.Array;
using NyaFs.Filesystem.Universal;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.CramFs
{
    public class CramFsBuilder : IFilesystemBuilder
    {
        private int BlockSize = 4096;

        List<uint> InvalidGid = new List<uint>();
        private List<Builder.Node> Nodes = new List<Builder.Node>();
        private Builder.Nodes.Dir Root = null;
        private uint BlocksCount = 0;

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

        private void CheckId(uint Uid, uint Gid)
        {
            if(Gid > 255)
            {
                if (!InvalidGid.Contains(Gid)) InvalidGid.Add(Gid);
            }
        }

        private void AddNestedNode(string Path, Func<Builder.Node> NodeGetter)
        {
            var Parent = GetParentDirectory(Path);
            if ((Parent != null) || (Path == "/"))
            {
                var N = NodeGetter();
                N.Index = Convert.ToUInt32(Nodes.Count + 1);

                CheckId(N.User, N.Group);

                if (Parent != null)
                    Parent.AddNestedNode(N);
                else
                    Root = N as Builder.Nodes.Dir;

                Nodes.Add(N);
            }
            else
                throw new InvalidOperationException($"Cannot add entry with path {Path}: no parent dir.");
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
            AddNestedNode(Path, () => new Builder.Nodes.Block(Path, User, Group, Mode, Major, Minor));

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
            AddNestedNode(Path, () => new Builder.Nodes.Char(Path, User, Group, Mode, Major, Minor));

        /// <summary>
        /// Create directory
        /// </summary>
        /// <param name="Path">Path to directory (parent dir must exists)</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Directory(string Path, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.Dir(Path, User, Group, Mode));

        /// <summary>
        /// Create fifo
        /// </summary>
        /// <param name="Path">Path to fifo</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Fifo(string Path, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.Fifo(Path, User, Group, Mode));

        /// <summary>
        /// Create file
        /// </summary>
        /// <param name="Path">Path to file</param>
        /// <param name="Content">File content</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void File(string Path, byte[] Content, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.File(Path, User, Group, Mode, Content));

        /// <summary>
        /// Create socket
        /// </summary>
        /// <param name="Path">Path to socket</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Socket(string Path, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.Socket(Path, User, Group, Mode));

        /// <summary>
        /// Create symlink
        /// </summary>
        /// <param name="Path">Path to symlink</param>
        /// <param name="Target">Target path</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void SymLink(string Path, string Target, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.SymLink(Path, User, Group, Mode, Target));

        public byte[] GetFilesystemImage()
        {
            if(InvalidGid.Count > 0)
            {
                Log.Warning(0, $"Filesystem containg group id which cannot be saved in cramfs image: {String.Join(", ", Array.ConvertAll(InvalidGid.ToArray(), G => $"{G}"))}");
            }

            var Res = new List<byte>();
            AppendSuperblock(Res);
            AppendNodes(Res);
            AppendData(Res);

            var PreparedImage = Res.ToArray();
            UpdateINodes(PreparedImage);
            UpdateSuperblock(PreparedImage);

            return PreparedImage;
        }

        private void AppendSuperblock(List<byte> Res)
        {
            var SB = new Types.CrSuperblock();

            Res.AddRange(SB.getPacket());
        }

        private void PreprocessNodes(List<Builder.Nodes.Dir> Dirs, Builder.Nodes.Dir Base)
        {
            foreach (var N in Base.NestedNodes)
            {
                if (N.Type == Universal.Types.FilesystemItemType.Directory)
                {
                    var D = N as Builder.Nodes.Dir;
                    Dirs.Add(D);

                    PreprocessNodes(Dirs, D);
                }
            }
        }

        private void AppendNodes(List<byte> Res)
        {
            List<Builder.Nodes.Dir> Dirs = new List<Builder.Nodes.Dir>();

            WriteNode(Res, Root);
            WriteDirEntries(Res, Root);

            PreprocessNodes(Dirs, Root);

            foreach (var D in Dirs)
                WriteDirEntries(Res, D);
        }

        private void WriteNode(List<byte> Res, Builder.Node N)
        {
            N.RelevantNodeOffset = Convert.ToUInt32(Res.Count);
            var Data = N.GenerateNode();

            Res.AddRange(Data.getPacket());
        }

        private void WriteDirEntries(List<byte> Res, Builder.Nodes.Dir D)
        {
            D.DataOffset = Convert.ToUInt32(Res.Count);
            foreach (var N in D.NestedNodes)
            {
                WriteNode(Res, N);
            }
            D.DataSize = Convert.ToUInt32(Res.Count) - D.DataOffset;
        }

        private void AppendData(List<byte> Res)
        {
            foreach (var N in Nodes)
            {
                var Data = N.Content;
                if(Data != null)
                {
                    N.DataOffset = Convert.ToUInt32(Res.Count);
                    var Blocks = ConvertToBlocks(Data, N.DataOffset);

                    N.DataSize = Convert.ToUInt32(Data.Length);
                    Res.AddRange(Blocks);
                }
            }
        }

        private List<byte[]> GetBlocks(byte[] Data)
        {
            long Offset = 0;
            var Res = new List<byte[]>();
            while(Offset < Data.Length)
            {
                long ToCopy = Data.Length - Offset;
                long Size = ToCopy > BlockSize ? BlockSize : ToCopy;

                byte[] Unpacked = Data.ReadArray(Offset, Size);
                byte[] Packed = Compression.Gzip.Compress(Unpacked);
                Res.Add(Packed);

                Offset += Size;
            }
            return Res;
        }

        /// <summary>
        /// Build table of blocks end!..
        /// </summary>
        /// <param name="Blocks"></param>
        /// <param name="BlocksOffset"></param>
        /// <returns></returns>
        private byte[] GetDataHeader(List<byte[]> Blocks, uint BlocksOffset)
        {
            var Res = new byte[Blocks.Count * 4];
            for (int i = 0; i < Blocks.Count; i++)
            {
                BlocksOffset = Convert.ToUInt32(BlocksOffset + Blocks[i].Length);

                Res.WriteUInt32(i * 4, BlocksOffset);
            }
            return Res;
        }

        private byte[] ConvertToBlocks(byte[] Data, uint BaseOffset)
        {
            // Build data blocks...
            var Blocks = GetBlocks(Data);

            // Build blocks table...
            var Header = GetDataHeader(Blocks, Convert.ToUInt32(BaseOffset + Blocks.Count * 4));

            // Join all together...
            var Res = new List<byte>();
            Res.AddRange(Header);
            for(int i = 0; i < Blocks.Count; i++)
                Res.AddRange(Blocks[i]);

            // Add padding
            while (Res.Count % 4 != 0) Res.Add(0);

            BlocksCount += Convert.ToUInt32(Blocks.Count);
            return Res.ToArray();
        }

        private void UpdateSuperblock(byte[] preparedImage)
        {
            var SB = new Types.CrSuperblock(preparedImage, 0);

            SB.Flags = 3;
            SB.FsIDFiles = Convert.ToUInt32(Nodes.Count);
            SB.FsIDBlocks = BlocksCount;
            SB.Size = Convert.ToUInt32(preparedImage.Length);

            // Calc image crc
            SB.FsIDCrc = Crc32.CalcCrc(preparedImage);
        }

        private void UpdateINodes(byte[] preparedImage)
        {
            foreach(var N in Nodes)
            {
                var Node = new Types.CrNode(preparedImage, N.RelevantNodeOffset);

                Node.Offset = N.DataOffset;
                if (Node.FsNodeType == Universal.Types.FilesystemItemType.Directory)
                {
                    Node.Size = N.DataSize;
                }
            }
        }
    }
}
