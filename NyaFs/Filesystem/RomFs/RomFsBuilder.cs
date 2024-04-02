using Extension.Array;
using NyaFs.Filesystem.Universal;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.RomFs
{
    public class RomFsBuilder : IFilesystemBuilder
    {
        List<uint> InvalidGid = new List<uint>();
        List<uint> InvalidUid = new List<uint>();

        private List<Builder.Node> Nodes = new List<Builder.Node>();
        private Builder.Nodes.Dir Root = null;

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
            if (Gid != 0)
            {
                if (!InvalidGid.Contains(Gid)) InvalidGid.Add(Gid);
            }
            if (Gid != 0)
            {
                if (!InvalidUid.Contains(Gid)) InvalidUid.Add(Gid);
            }
        }

        private void AddNestedNode(string Path, Func<Builder.Node> NodeGetter)
        {
            var Parent = GetParentDirectory(Path);
            if ((Parent != null) || (Path == "/"))
            {
                var N = NodeGetter();

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
            AddNestedNode(Path, () => new Builder.Nodes.Block(Path, Mode, Major, Minor));

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
            AddNestedNode(Path, () => new Builder.Nodes.Char(Path, Mode, Major, Minor));

        /// <summary>
        /// Create directory
        /// </summary>
        /// <param name="Path">Path to directory (parent dir must exists)</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Directory(string Path, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.Dir(Path, Mode));

        /// <summary>
        /// Create fifo
        /// </summary>
        /// <param name="Path">Path to fifo</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Fifo(string Path, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.Fifo(Path, Mode));

        /// <summary>
        /// Create file
        /// </summary>
        /// <param name="Path">Path to file</param>
        /// <param name="Content">File content</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void File(string Path, byte[] Content, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.File(Path, Mode, Content));

        /// <summary>
        /// Create socket
        /// </summary>
        /// <param name="Path">Path to socket</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void Socket(string Path, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.Socket(Path, Mode));

        /// <summary>
        /// Create symlink
        /// </summary>
        /// <param name="Path">Path to symlink</param>
        /// <param name="Target">Target path</param>
        /// <param name="User">Owner user</param>
        /// <param name="Group">Owner group</param>
        /// <param name="Mode">Access mode</param>
        public void SymLink(string Path, string Target, uint User, uint Group, uint Mode) =>
            AddNestedNode(Path, () => new Builder.Nodes.SymLink(Path, Mode, Target));

        public byte[] GetFilesystemImage()
        {
            if (InvalidGid.Count > 0)
            {
                Log.Warning(0, $"Filesystem containg group id which cannot be saved in romfs image: {String.Join(", ", Array.ConvertAll(InvalidGid.ToArray(), G => $"{G}"))}");
            }
            if (InvalidUid.Count > 0)
            {
                Log.Warning(0, $"Filesystem containg user id which cannot be saved in romfs image: {String.Join(", ", Array.ConvertAll(InvalidUid.ToArray(), U => $"{U}"))}");
            }

            var Res = new List<byte>();
            AppendSuperblock(Res);
            AppendNodes(Res);

            var PreparedImage = Res.ToArray();
            UpdateNodes(PreparedImage);
            UpdateSuperblock(PreparedImage);

            return PreparedImage;
        }

        private void AppendSuperblock(List<byte> Res)
        {
            var SB = new Types.RmSuperblock();

            Res.AddRange(SB.getPacket());
        }

        private void AppendNodes(List<byte> Res)
        {
            Root.FileOffset = Res.Count;
            AddDirectoryNodes(Res, Root);
        }

        private void  AddNodeWithContent(List<byte> Res, List<Builder.Node> LocalNodes, Builder.Node Node)
        {
            Node.FileOffset = Res.Count;

            var NodeData = Node.GetHeader();
            Res.AddRange(NodeData.getPacket());
            Res.AddRange(Node.GetContent());

            LocalNodes.Add(Node);
        }

        private void AddParentNode(List<byte> Res, List<Builder.Node> LocalNodes, long Offset)
        {
            var N = new Builder.Nodes.Parent(Offset);
            AddNodeWithContent(Res, LocalNodes, N);

            Nodes.Add(N);
        }

        private void AddDirectoryNodes(List<byte> Res, Builder.Nodes.Dir Dir)
        {
            List<Builder.Node> LocalNodes = new List<Builder.Node>();

            var StartOffset = Res.Count;
            // dir
            AddNodeWithContent(Res, LocalNodes, Dir);
            var NestedOffset = Res.Count;
            // ..
            AddParentNode(Res, LocalNodes, StartOffset);

            // <nested files>
            foreach (var N in Dir.NestedNodes)
            {
                if (N.Type == Universal.Types.FilesystemItemType.Directory)
                {
                    N.FileOffset = Res.Count;
                    LocalNodes.Add(N);

                    AddDirectoryNodes(Res, N as Builder.Nodes.Dir);
                }
                else
                    AddNodeWithContent(Res, LocalNodes, N);
            }

            // Update offsets
            for(int i = LocalNodes.Count - 1; i > 0; i--)
                LocalNodes[i - 1].NextOffset = LocalNodes[i].FileOffset;
            
            Dir.DirLink = (Dir.Path == "/") ? StartOffset : NestedOffset;
                
        }

        private uint CalcChecksum(byte[] preparedImage)
        {
            int Count = Math.Min(512, preparedImage.Length) / 4;
            uint Res = 0;

            for(int i = 0; i < Count; i++)
                Res += preparedImage.ReadUInt32BE(i * 4);

            Res = Convert.ToUInt32((-Res) & 0xFFFFFFFF);
            return Res;
        }

        private void UpdateSuperblock(byte[] preparedImage)
        {
            var SB = new Types.RmSuperblock(preparedImage, 0);

            SB.FullSize = Convert.ToUInt32(preparedImage.Length);
            SB.Checksum = CalcChecksum(preparedImage);
        }

        private void UpdateNodes(byte[] preparedImage)
        {
            foreach(var N in Nodes)
            {
                var Node = new Types.RmNode(preparedImage, N.FileOffset);
                Node.NextHeader = Convert.ToUInt32(N.NextOffset);
                if(N.Type == Universal.Types.FilesystemItemType.Directory)
                    Node.SpecInfo = Convert.ToUInt32((N as Builder.Nodes.Dir).DirLink);

                Node.CalcChecksum();
            }
        }
    }
}