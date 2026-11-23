using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Read-only Atari DOS 2.0/2.5 filesystem reader for .atr images. DOS 2.x
    /// has a flat directory at sector 360; files occupy contiguous sectors.
    /// </summary>
    public sealed class AtariDosReader : IFilesystemReader
    {
        private const int HeaderSize = 16;
        private const int DirectorySector = 360;
        private const int DirectoryEntries = 64;

        private sealed class Node
        {
            public string Name;
            public uint StartSector;
            public uint SectorCount;
            public uint Size;
            public readonly List<Node> Children = new List<Node>();
        }

        private readonly byte[] Data;
        private readonly int SectorSize;
        private readonly int SectorCount;
        private readonly Node Root;
        private readonly Dictionary<string, Node> Nodes = new Dictionary<string, Node>(StringComparer.Ordinal);

        public long VolumeBytes => Data.Length;

        public AtariDosReader(byte[] Data)
        {
            this.Data = Data ?? throw new ArgumentNullException(nameof(Data));
            if (!IsAtariDos(this.Data, null))
                throw new InvalidOperationException("Not an Atari DOS .atr filesystem");

            SectorSize = Data[4] | (Data[5] << 8);
            SectorCount = (Data.Length - HeaderSize) / SectorSize;
            Root = new Node { Name = "" };
            Nodes[""] = Root;
            BuildTree();
        }

        public static bool IsAtariDos(byte[] Data, string SourceName)
        {
            if ((Data == null) || (Data.Length < HeaderSize + 128 * 361))
                return false;
            if ((Data[0] != 0x96) || (Data[1] != 0x02))
                return false;
            if (!String.IsNullOrEmpty(SourceName) &&
                !SourceName.EndsWith(".atr", StringComparison.OrdinalIgnoreCase))
                return false;

            int SectorSize = Data[4] | (Data[5] << 8);
            if ((SectorSize != 128) && (SectorSize != 256))
                return false;

            int SectorCount = (Data.Length - HeaderSize) / SectorSize;
            if (SectorCount < DirectorySector + 8)
                return false;

            int Offset = HeaderSize + DirectorySector * SectorSize;
            for (int I = 0; I < DirectoryEntries; I++)
            {
                int Entry = Offset + I * 16;
                if (Entry + 16 > Data.Length)
                    break;
                if ((Data[Entry] & 0x40) != 0)
                    return true;
            }
            return false;
        }

        public byte[] Read(string Path)
        {
            var Node = Find(Path);
            if (Node == null)
                return null;

            int Offset = HeaderSize + (int)(Node.StartSector * (uint)SectorSize);
            int Length = (int)(Node.SectorCount * (uint)SectorSize);
            if ((Offset < 0) || (Offset >= Data.Length))
                return null;

            Length = Math.Min(Length, Data.Length - Offset);
            var Result = new byte[Length];
            Array.Copy(Data, Offset, Result, 0, Length);
            return Result;
        }

        public string ReadLink(string Path) => null;

        public DeviceInfo ReadDevice(string Path) => null;

        public FilesystemEntry[] ReadDir(string Path)
        {
            var Node = Find(Path);
            if (Node == null)
                return Array.Empty<FilesystemEntry>();

            var Parent = Normalize(Path);
            var Result = new List<FilesystemEntry>();
            foreach (var Child in Node.Children)
                Result.Add(new FilesystemEntry(FilesystemItemType.File, Combine(Parent, Child.Name), 0, 0, 0x1A4, Child.Size));
            return Result.ToArray();
        }

        public bool TryGetRootFsItem(out uint User, out uint Group, out uint Mode)
        {
            User = 0;
            Group = 0;
            Mode = 0x1ED;
            return true;
        }

        private void BuildTree()
        {
            int Offset = HeaderSize + DirectorySector * SectorSize;
            for (int I = 0; I < DirectoryEntries; I++)
            {
                int Entry = Offset + I * 16;
                if (Entry + 16 > Data.Length)
                    break;

                byte Flags = Data[Entry];
                if ((Flags & 0x40) == 0)
                    continue;

                uint Count = (uint)(Data[Entry + 1] | (Data[Entry + 2] << 8));
                uint Start = (uint)(Data[Entry + 3] | (Data[Entry + 4] << 8));
                string Name = DecodeName(Data, Entry + 5, 11);
                if ((Name.Length == 0) || (Count == 0) || (Start >= SectorCount))
                    continue;

                var Node = new Node
                {
                    Name = Sanitize(Name),
                    StartSector = Start,
                    SectorCount = Count,
                    Size = Count * (uint)SectorSize
                };
                Root.Children.Add(Node);
                Nodes[Node.Name] = Node;
            }
        }

        private static string DecodeName(byte[] Data, int Offset, int Length)
        {
            var Raw = new char[Length];
            for (int I = 0; I < Length; I++)
            {
                byte B = Data[Offset + I];
                Raw[I] = (B >= 0x20 && B <= 0x7E) ? (char)B : ' ';
            }
            string Text = new string(Raw).Trim();
            if (Text.Length > 8)
            {
                string Base = Text.Substring(0, 8).TrimEnd();
                string Ext = Text.Substring(8).Trim();
                return Ext.Length == 0 ? Base : Base + "." + Ext;
            }
            return Text;
        }

        private static string Sanitize(string Name) =>
            Name.Replace('/', '_').Replace('\\', '_');

        private Node Find(string Path)
        {
            var Key = Normalize(Path);
            return Nodes.TryGetValue(Key, out var Node) ? Node : null;
        }

        private static string Normalize(string Path)
        {
            if (String.IsNullOrEmpty(Path) || (Path == ".") || (Path == "/"))
                return "";
            return Path.Replace('\\', '/').Trim('/');
        }

        private static string Combine(string Parent, string Name) =>
            String.IsNullOrEmpty(Parent) ? Name : Parent + "/" + Name;
    }
}
