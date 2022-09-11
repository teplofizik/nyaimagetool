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

        public Dir(string Path, uint User, uint Group, uint Mode) : base(Types.SqInodeType.BasicDirectory, Path, User, Group, Mode)
        {

        }

        public void AddEntry(DirectoryEntry Entry)
        {
            Entries.Add(Entry);
        }

        private Types.SqDirectoryHeader GetHeader() => new Types.SqDirectoryHeader(Convert.ToUInt32(Entries.Count), 0, 0);

        public byte[] GetEntries()
        {
            var Res = new List<byte>();
            var Header = GetHeader();
            Res.AddRange(Header.getPacket());

            foreach(var E in Entries)
            {
                var DE = new Types.SqDirectoryEntry(Header.INodeNumber, 
                                                    E.NodeRef.MetadataOffset, 
                                                    E.Type, 
                                                    Convert.ToUInt32(E.NodeRef.UnpackedOffset), 
                                                    E.Node.Index - Header.INodeNumber, 
                                                    E.Filename);

                Res.AddRange(DE.getPacket());
            }

            return Res.ToArray();
        }
    }
}
