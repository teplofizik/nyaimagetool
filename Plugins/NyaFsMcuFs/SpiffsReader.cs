using System;
using System.Collections.Generic;
using System.Text;
using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;

namespace NyaFsMcuFs
{
    /// <summary>
    /// Reader for SPIFFS (object lookup pages + object index pages + data spans).
    /// Geometry (page/block size) is not stored in a SPIFFS image, so defaults to 256/4096.
    /// </summary>
    public class SpiffsReader : IFilesystemReader
    {
        private const int ObjIdSize = 2;
        private const int PageIxSize = 2;
        private const int PageHeaderSize = 5;    // obj_id(2) + span_ix(2) + flags(1)
        private const int IxHeaderSize = 45;     // p_hdr(5) + align(3) + size(4) + type(1) + name(32)
        private const int IxSize = 8;            // p_hdr(5) + align(3)
        private const int NameLen = 32;

        private const int IxFlag = 0x8000;
        private const ushort ObjFree = 0xFFFF;
        private const ushort ObjDeleted = 0x0000;

        private const byte TypeFile = 1;
        private const byte TypeDir = 2;

        private readonly byte[] Data;
        private readonly int PageSize;
        private readonly int BlockSize;
        private readonly int PagesPerBlock;

        public long TotalSize => (long)Data.Length;

        private class Obj
        {
            public int Id;
            public string Name;
            public uint Size;
            public byte Type;
            public readonly Dictionary<int, ushort[]> Index = new Dictionary<int, ushort[]>(); // span_ix -> page indices
        }

        private readonly List<Obj> Objects = new List<Obj>();
        private readonly List<Entry> Entries = new List<Entry>();

        private class Entry
        {
            public string Path;
            public FilesystemItemType Type;
            public byte[] Content;
            public long Size;
        }

        public SpiffsReader(byte[] Data, int PageSize = 256, int BlockSize = 4096)
        {
            this.Data = Data;
            this.PageSize = PageSize;
            this.BlockSize = BlockSize;
            PagesPerBlock = BlockSize / PageSize;
            Init();
        }

        /// <summary>
        /// Heuristic SPIFFS detection: the image must be a whole number of blocks and every
        /// used object-lookup entry must match the obj_id stored in the page it points to,
        /// with at least one well-formed object index header.
        /// </summary>
        public static bool LooksLikeSpiffs(byte[] Data, int PageSize = 256, int BlockSize = 4096)
        {
            if ((Data.Length < 2 * BlockSize) || ((Data.Length % BlockSize) != 0))
                return false;

            int PPB = BlockSize / PageSize;
            int LookupEntries = PPB - 1;
            int IndexHeaders = 0;

            for (int Blk = 0; Blk < Data.Length / BlockSize; Blk++)
            {
                long Base = (long)Blk * BlockSize;

                for (int E = 0; E < LookupEntries; E++)
                {
                    ushort V = (ushort)(Data[Base + E * 2] | (Data[Base + E * 2 + 1] << 8));
                    if ((V == ObjFree) || (V == ObjDeleted)) continue;

                    int Page = Blk * PPB + 1 + E;
                    long Po = (long)Page * PageSize;
                    if (Po + PageSize > Data.Length) return false;

                    // the page's stored obj_id must match the lookup entry
                    ushort Stored = (ushort)(Data[Po] | (Data[Po + 1] << 8));
                    if (Stored != V) return false;

                    if ((V & IxFlag) != 0)
                    {
                        int SpanIx = (ushort)(Data[Po + 2] | (Data[Po + 3] << 8));
                        if (SpanIx == 0)
                        {
                            byte Type = Data[Po + 12];
                            if ((Type != TypeFile) && (Type != TypeDir)) return false;

                            int Len = 0;
                            while ((Len < NameLen) && (Data[Po + 13 + Len] != 0))
                            {
                                byte Ch = Data[Po + 13 + Len];
                                if ((Ch < 0x20) || (Ch > 0x7E)) return false;
                                Len++;
                            }
                            if (Len == 0) return false;
                            IndexHeaders++;
                        }
                    }
                }
            }

            return IndexHeaders > 0;
        }

        private ushort U16(long Off) => (ushort)(Data[Off] | (Data[Off + 1] << 8));
        private uint U32(long Off) => (uint)(Data[Off] | (Data[Off + 1] << 8) | (Data[Off + 2] << 16) | (Data[Off + 3] << 24));

        private void Init()
        {
            int Blocks = Data.Length / BlockSize;
            int LookupEntries = PagesPerBlock - 1;

            for (int Blk = 0; Blk < Blocks; Blk++)
            {
                long Base = (long)Blk * BlockSize;

                for (int E = 0; E < LookupEntries; E++)
                {
                    ushort V = U16(Base + E * ObjIdSize);
                    if ((V == ObjFree) || (V == ObjDeleted)) continue;

                    bool IsIndex = (V & IxFlag) != 0;
                    if (!IsIndex) continue;

                    int Oid = V & 0x7FFF;
                    int Page = Blk * PagesPerBlock + 1 + E;
                    long Po = (long)Page * PageSize;

                    int SpanIx = U16(Po + 2);

                    var O = FindOrCreate(Oid);
                    if (SpanIx == 0)
                    {
                        O.Size = U32(Po + 8);
                        O.Type = Data[Po + 12];
                        O.Name = ReadName(Po + 13);
                        O.Index[0] = ReadEntries(Po, IxHeaderSize);
                    }
                    else
                    {
                        O.Index[SpanIx] = ReadEntries(Po, IxSize);
                    }
                }
            }

            BuildEntries();
        }

        private Obj FindOrCreate(int Id)
        {
            foreach (var O in Objects)
                if (O.Id == Id) return O;

            var N = new Obj { Id = Id };
            Objects.Add(N);
            return N;
        }

        private string ReadName(long Off)
        {
            int Len = 0;
            while ((Len < NameLen) && (Data[Off + Len] != 0)) Len++;
            return Encoding.UTF8.GetString(Data, (int)Off, Len);
        }

        private ushort[] ReadEntries(long Po, int HeaderSize)
        {
            int Count = (PageSize - HeaderSize) / PageIxSize;
            var Res = new ushort[Count];
            for (int i = 0; i < Count; i++)
                Res[i] = U16(Po + HeaderSize + i * PageIxSize);
            return Res;
        }

        private int HeaderIxLen => (PageSize - IxHeaderSize) / PageIxSize;
        private int IxLen => (PageSize - IxSize) / PageIxSize;

        private byte[] ReadObject(Obj O)
        {
            var Res = new List<byte>();
            uint Size = O.Size;

            for (int Span = 0; Res.Count < Size; Span++)
            {
                int IxSpan = (Span < HeaderIxLen) ? 0 : (1 + (Span - HeaderIxLen) / IxLen);
                int IxEntry = (Span < HeaderIxLen) ? Span : ((Span - HeaderIxLen) % IxLen);

                if (!O.Index.TryGetValue(IxSpan, out var Entries)) break;
                if (IxEntry >= Entries.Length) break;

                ushort Pix = Entries[IxEntry];
                if ((Pix == ObjFree) || (Pix == 0)) break;

                long Po = (long)Pix * PageSize;
                int DataLen = PageSize - PageHeaderSize;
                int Take = (int)Math.Min(DataLen, Size - Res.Count);
                for (int i = 0; i < Take; i++) Res.Add(Data[Po + PageHeaderSize + i]);
            }

            return Res.ToArray();
        }

        private void BuildEntries()
        {
            var Dirs = new HashSet<string>();

            foreach (var O in Objects)
            {
                if (O.Name == null) continue;

                var Path = Normalize(O.Name);
                if (Path.Length == 0) continue;

                // implicit parent directories
                var Idx = Path.IndexOf('/');
                while (Idx > 0)
                {
                    var Dir = Path.Substring(0, Idx);
                    if (Dirs.Add(Dir))
                        Entries.Add(new Entry { Path = Dir, Type = FilesystemItemType.Directory });
                    Idx = Path.IndexOf('/', Idx + 1);
                }

                if (O.Type == TypeDir)
                {
                    if (Dirs.Add(Path))
                        Entries.Add(new Entry { Path = Path, Type = FilesystemItemType.Directory });
                }
                else
                {
                    var Content = ReadObject(O);
                    Entries.Add(new Entry { Path = Path, Type = FilesystemItemType.File, Content = Content, Size = Content.Length });
                }
            }
        }

        private static string Normalize(string Path)
        {
            if (string.IsNullOrEmpty(Path)) return "";
            Path = Path.Replace('\\', '/');
            while (Path.StartsWith("./")) Path = Path.Substring(2);
            if (Path.StartsWith("/")) Path = Path.Substring(1);
            return Path.TrimEnd('/');
        }

        private Entry Find(string Path)
        {
            var P = Normalize(Path);
            foreach (var E in Entries)
                if (E.Path == P) return E;
            return null;
        }

        public byte[] Read(string Path)
        {
            var E = Find(Path);
            return ((E != null) && (E.Type == FilesystemItemType.File)) ? E.Content : null;
        }

        public string ReadLink(string Path) => null;
        public DeviceInfo ReadDevice(string Path) => null;

        public FilesystemEntry[] ReadDir(string Path)
        {
            var Res = new List<FilesystemEntry>();
            var P = Normalize(Path);

            foreach (var E in Entries)
            {
                if (E.Path.Length == 0) continue;
                int Idx = E.Path.LastIndexOf('/');
                var Parent = (Idx < 0) ? "" : E.Path.Substring(0, Idx);
                if (Parent == P)
                    Res.Add(new FilesystemEntry(E.Type, E.Path, 0, 0, (E.Type == FilesystemItemType.Directory) ? 0x1EDu : 0x1A4u, (uint)E.Size));
            }

            return Res.ToArray();
        }
    }
}
