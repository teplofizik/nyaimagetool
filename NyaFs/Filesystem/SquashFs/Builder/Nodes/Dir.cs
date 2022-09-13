using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFs.Filesystem.SquashFs.Builder.Nodes
{
    class Dir : Node
    {
        public MetadataRef EntriesRef;
        public List<DirectoryEntry> Entries = new List<DirectoryEntry>();

        public uint Parent = 0;

        public Dir(string Path, uint User, uint Group, uint Mode) : base(Types.SqInodeType.BasicDirectory, Path, User, Group, Mode)
        {

        }

        public void AddEntry(DirectoryEntry Entry)
        {
            Entries.Add(Entry);
        }

        private uint GetEntryBlock()
        {
            if (Entries.Count > 0)
                return Convert.ToUInt32(Entries.First().NodeRef.MetadataOffset);
            else
                return 0;
        }

        private Types.SqDirectoryHeader GetHeader() => new Types.SqDirectoryHeader(Convert.ToUInt32(Entries.Count), GetEntryBlock(), 1);

        public byte[] GetEntries()
        {
            var Res = new List<byte>();
            var Header = GetHeader();
            Res.AddRange(Header.getPacket());

            foreach (var E in Entries.OrderBy(E => E.Filename, StringComparer.Ordinal))
            {
                var DE = new Types.SqDirectoryEntry(Header.INodeNumber, 
                                                    Convert.ToInt64(E.NodeRef.MetadataOffset), 
                                                    E.Type, 
                                                    Convert.ToUInt32(E.NodeRef.UnpackedOffset), 
                                                    E.Node.Index - Header.INodeNumber, 
                                                    E.Filename);

                Res.AddRange(DE.getPacket());
            }

            return Res.ToArray();
        }

        public override Types.SqInode GetINode() => new Types.Nodes.BasicDirectory(Mode, UId, GId,
            Convert.ToUInt32(EntriesRef?.MetadataOffset ?? 0),
            Convert.ToUInt32(EntriesRef?.UnpackedOffset ?? 0),
            Convert.ToUInt32(Entries.Count + 2),
            Convert.ToUInt32(GetEntries().Length)+3,
            Parent);
    }
}
