using System;
using System.Collections.Generic;
using System.Text;
using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;

namespace NyaFsMcuFs
{
    /// <summary>
    /// Reader for littlefs v2 (metadata pairs + tags, inline/CTZ files).
    /// Port of the reference algorithm (littlefs SPEC.md + scripts/readmdir.py).
    /// </summary>
    public class LittleFsReader : IFilesystemReader
    {
        private const uint ValidMask = 0x7FFFFFFF;

        private readonly byte[] Data;
        private int BlockSize;
        private long BlockCount;

        /// <summary>Total filesystem size in bytes (block_size * block_count).</summary>
        public long TotalSize => (long)BlockSize * BlockCount;

        private class Tag
        {
            public uint Raw;
            public long Off;
            public byte[] Data;

            public int Type => (int)((Raw & 0x7FF00000) >> 20);
            public int Id => (int)((Raw & 0x000FFC00) >> 10);
            public int Size => (int)(Raw & 0x3FF);
            public int Dsize => 4 + ((Size != 0x3FF) ? Size : 0);
            public bool IsUnique => (Raw & 0x10000000) == 0;
            public bool IsAttr => (Raw & 0x40000000) == 0;

            public bool Is(string Name)
            {
                var M = Types[Name];
                return (Type & M.Item1) == M.Item2;
            }

            public Tag Chid(int NewId)
            {
                return new Tag { Raw = (uint)((Type << 20) | (NewId << 10) | Size), Off = Off, Data = Data };
            }

            public Tag Mkmask()
            {
                uint T = (uint)((IsUnique ? 0x700 : 0x7FF) << 20);
                uint I = (uint)((IsAttr ? 0x3FF : 0) << 10);
                return new Tag { Raw = T | I };
            }
        }

        private static readonly Dictionary<string, Tuple<int, int>> Types = new Dictionary<string, Tuple<int, int>>
        {
            { "splice", Tuple.Create(0x700, 0x400) },
            { "create", Tuple.Create(0x7FF, 0x401) },
            { "delete", Tuple.Create(0x7FF, 0x4FF) },
            { "name", Tuple.Create(0x700, 0x000) },
            { "reg", Tuple.Create(0x7FF, 0x001) },
            { "dir", Tuple.Create(0x7FF, 0x002) },
            { "superblock", Tuple.Create(0x7FF, 0x0FF) },
            { "struct", Tuple.Create(0x700, 0x200) },
            { "dirstruct", Tuple.Create(0x7FF, 0x200) },
            { "ctzstruct", Tuple.Create(0x7FF, 0x202) },
            { "inlinestruct", Tuple.Create(0x7FF, 0x201) },
            { "userattr", Tuple.Create(0x700, 0x300) },
            { "tail", Tuple.Create(0x700, 0x600) },
            { "softtail", Tuple.Create(0x7FF, 0x600) },
            { "hardtail", Tuple.Create(0x7FF, 0x601) },
            { "crc", Tuple.Create(0x700, 0x500) },
            { "ccrc", Tuple.Create(0x780, 0x500) },
            { "fcrc", Tuple.Create(0x7FF, 0x5FF) },
        };

        private class Mdir
        {
            public uint Rev;
            public List<Tag> Log = new List<Tag>();
            public List<Tag> All = new List<Tag>();
            public List<Tag> Tags = new List<Tag>();
            public List<int> Ids = new List<int>();

            public bool Get(Tag Gtag, out Tag Result)
            {
                Result = null;
                var Gmask = Gtag.Mkmask();

                int Gdiff = 0;
                for (int i = Log.Count - 1; i >= 0; i--)
                {
                    var Tag = Log[i];

                    if ((Gmask.Id != 0) && Tag.Is("splice") && (Tag.Id <= Gtag.Id - Gdiff))
                    {
                        if (Tag.Is("create") && (Tag.Id == Gtag.Id - Gdiff))
                            break;

                        int Chunk = Tag.Type & 0xFF;
                        Gdiff += (Chunk < 0x80) ? Chunk : (Chunk - 0x100);
                    }

                    var Want = Gtag.Chid(Gtag.Id - Gdiff);
                    if ((Gmask.Raw & Tag.Raw) == (Gmask.Raw & Want.Raw))
                    {
                        if (Tag.Size == 0x3FF)
                            break; // deleted
                        Result = Tag;
                        return true;
                    }
                }

                return false;
            }
        }

        private class Entry
        {
            public string Path;
            public FilesystemItemType Type;
            public byte[] Content;   // for files (resolved)
            public long Size;
        }

        private readonly List<Entry> Entries = new List<Entry>();

        public LittleFsReader(byte[] Data)
        {
            this.Data = Data;
            Init();
        }

        private byte[] Block(long Index)
        {
            var Res = new byte[BlockSize];
            long Off = Index * BlockSize;

            if ((Off >= 0) && (Off < Data.Length))
            {
                int Len = (int)Math.Min(BlockSize, Data.Length - Off);
                Array.Copy(Data, Off, Res, 0, Len);
            }
            else
            {
                for (int i = 0; i < BlockSize; i++) Res[i] = 0xFF;
            }

            return Res;
        }

        private static uint ReadU32LE(byte[] B, int Off) =>
            (uint)(B[Off] | (B[Off + 1] << 8) | (B[Off + 2] << 16) | (B[Off + 3] << 24));

        private static uint ReadU32BE(byte[] B, int Off) =>
            (uint)((B[Off] << 24) | (B[Off + 1] << 16) | (B[Off + 2] << 8) | B[Off + 3]);

        private static int TrailingZeros(int V)
        {
            int N = 0;
            while (((V & 1) == 0) && (N < 31)) { V >>= 1; N++; }
            return N;
        }

        private static uint Crc32(byte[] B, int Off, int Len, uint Crc)
        {
            Crc ^= 0xFFFFFFFF;
            for (int i = 0; i < Len; i++)
            {
                Crc ^= B[Off + i];
                for (int k = 0; k < 8; k++)
                    Crc = ((Crc & 1) != 0) ? (0xEDB88320u ^ (Crc >> 1)) : (Crc >> 1);
            }
            return Crc ^ 0xFFFFFFFF;
        }

        private static void ParseSingle(byte[] Block, int BlockLen, Mdir Res)
        {
            Res.Rev = ReadU32LE(Block, 0);
            uint Crc = Crc32(Block, 0, 4, 0);
            uint Tag = 0xFFFFFFFF;
            int Off = 4;
            bool Corrupt = false;
            long FcrcSize = 0; uint Fcrc = 0; bool HasFcrc = false;

            while (BlockLen - Off >= 4)
            {
                uint Ntag = ReadU32BE(Block, Off);
                Tag = (Tag ^ Ntag) & ValidMask;

                var T = new Tag { Raw = Tag, Off = Off + 4 };
                int Dsize = T.Dsize;

                var Data = new byte[Math.Max(0, T.Size != 0x3FF ? T.Size : 0)];
                int CopyLen = Math.Min(Data.Length, Math.Max(0, BlockLen - (Off + 4)));
                if (CopyLen > 0) Array.Copy(Block, Off + 4, Data, 0, CopyLen);
                T.Data = Data;

                if (T.Is("ccrc"))
                    Crc = Crc32(Block, Off, Math.Min(8, BlockLen - Off), Crc);
                else
                    Crc = Crc32(Block, Off, Math.Min(Dsize, BlockLen - Off), Crc);

                Off += Dsize;
                Res.All.Add(T);

                if (T.Is("fcrc") && (T.Data.Length == 8))
                {
                    FcrcSize = ReadU32LE(T.Data, 0);
                    Fcrc = ReadU32LE(T.Data, 4);
                    HasFcrc = true;
                }
                else if (T.Is("ccrc"))
                {
                    if (Crc != 0xFFFFFFFF) Corrupt = true;
                    if (!Corrupt)
                    {
                        Res.Log = new List<Tag>(Res.All);
                        if (HasFcrc)
                        {
                            long End = Off + FcrcSize;
                            uint FcrcCheck = 0xFFFFFFFF ^ Crc32(Block, Off, (int)Math.Min(FcrcSize, BlockLen - Off), 0);
                            if (FcrcCheck == Fcrc) Corrupt = true;
                        }
                    }

                    Crc = 0;
                    Tag = Tag ^ (uint)((T.Type & 1) << 31);
                    HasFcrc = false;
                }
            }

            // active ids
            int Id = 0;
            while (true)
            {
                var Name = new Tag { Raw = (uint)((0x000 << 20) | (Id << 10)) };
                if (!Res.Get(Name, out _)) break;
                Res.Ids.Add(Id);
                Id++;
            }

            // most recent tags
            foreach (var T in Res.Log)
            {
                if (T.Is("crc") || T.Is("splice")) continue;

                if (T.Id == 0x3FF)
                {
                    if (Res.Get(T, out var Found) && (Found == T)) Res.Tags.Add(T);
                }
                else
                {
                    foreach (var I in Res.Ids)
                    {
                        var Nt = T.Chid(I);
                        if (Res.Get(Nt, out var Found) && (Found == T)) Res.Tags.Add(Nt);
                    }
                }
            }

            Res.Tags.Sort((a, b) => (a.Id != b.Id) ? a.Id.CompareTo(b.Id) : a.Type.CompareTo(b.Type));
        }

        private Mdir ReadPair(long Block1, long Block2)
        {
            var A = new Mdir(); ParseSingle(Block(Block1), BlockSize, A);
            var B = new Mdir(); ParseSingle(Block(Block2), BlockSize, B);

            // pick the newer block by sequence arithmetic (littlefs rev comparison)
            var Best = A;
            if (((B.Rev - Best.Rev) & 0x80000000u) == 0) Best = B;

            return Best;
        }

        private byte[] ReadCtz(long Head, long Size)
        {
            if (Size <= 0) return new byte[0];

            // number of file blocks
            int N = 0; long Cap = 0;
            while (Cap < Size)
            {
                int Hdr = (N == 0) ? 0 : (TrailingZeros(N) + 1) * 4;
                Cap += BlockSize - Hdr;
                N++;
            }

            var Blocks = new List<long> { Head };
            for (int i = 0; i < N - 1; i++)
            {
                var B = Block(Blocks[Blocks.Count - 1]);
                Blocks.Add(ReadU32LE(B, 0));
            }
            Blocks.Reverse();

            var Res = new List<byte>();
            for (int j = 0; j < Blocks.Count; j++)
            {
                int Hdr = (j == 0) ? 0 : (TrailingZeros(j) + 1) * 4;
                var B = Block(Blocks[j]);
                int Take = (int)Math.Min(Size - Res.Count, BlockSize - Hdr);
                for (int k = 0; k < Take; k++) Res.Add(B[Hdr + k]);
            }

            return Res.ToArray();
        }

        private void Init()
        {
            if (Data.Length < 32)
                throw new InvalidOperationException("littlefs image is too small");

            // The superblock inlinestruct lives at a fixed offset in block 0
            // (name tag data at 8, inlinestruct tag at 16, its data at 20).
            BlockSize = (int)ReadU32LE(Data, 24);
            BlockCount = ReadU32LE(Data, 28);

            if ((BlockSize <= 0) || (BlockCount <= 0))
                throw new InvalidOperationException("littlefs: invalid superblock geometry");

            // superblock lives in the root metadata pair (blocks 0/1)
            var Root = ReadPair(0, 1);

            if (!Root.Get(new Tag { Raw = (uint)((0x201 << 20)) }, out var SuperStruct) || (SuperStruct.Data.Length < 24))
                throw new InvalidOperationException("littlefs superblock not found");

            uint Version = ReadU32LE(SuperStruct.Data, 0);
            if ((Version >> 16) != 2)
                throw new InvalidOperationException($"Unsupported littlefs version {Version:X8}");

            BlockSize = (int)ReadU32LE(SuperStruct.Data, 4);
            BlockCount = ReadU32LE(SuperStruct.Data, 8);

            ImportDir(Root, "");
        }

        private void ImportDir(Mdir Dir, string Prefix)
        {
            var ById = new Dictionary<int, List<Tag>>();
            foreach (var T in Dir.Tags)
            {
                if (T.Id == 0x3FF) continue;
                if (!ById.TryGetValue(T.Id, out var L)) { L = new List<Tag>(); ById[T.Id] = L; }
                L.Add(T);
            }

            foreach (var Id in ById.Keys)
            {
                var Tags = ById[Id];
                string Name = null;
                bool IsDir = false;
                byte[] Content = null;
                long CtzHead = 0, CtzSize = 0;
                long Dir1 = -1, Dir2 = -1;

                foreach (var T in Tags)
                {
                    if (T.Is("dir")) { IsDir = true; Name = Encoding.UTF8.GetString(T.Data); }
                    else if (T.Is("reg")) { IsDir = false; Name = Encoding.UTF8.GetString(T.Data); }
                    else if (T.Is("inlinestruct")) Content = T.Data;
                    else if (T.Is("ctzstruct") && (T.Data.Length >= 8)) { CtzHead = ReadU32LE(T.Data, 0); CtzSize = ReadU32LE(T.Data, 4); }
                    else if (T.Is("dirstruct") && (T.Data.Length >= 8)) { Dir1 = ReadU32LE(T.Data, 0); Dir2 = ReadU32LE(T.Data, 4); }
                }

                if (Name == null) continue;

                var Path = (Prefix.Length == 0) ? Name : (Prefix + "/" + Name);

                if (IsDir && (Dir1 >= 0))
                {
                    Entries.Add(new Entry { Path = Path, Type = FilesystemItemType.Directory });
                    ImportDir(ReadPair(Dir1, Dir2), Path);
                }
                else
                {
                    if (Content == null && CtzHead >= 0 && CtzSize >= 0)
                        Content = ReadCtz(CtzHead, CtzSize);

                    Entries.Add(new Entry { Path = Path, Type = FilesystemItemType.File, Content = Content ?? new byte[0], Size = (Content?.Length ?? 0) });
                }
            }
        }

        private Entry Find(string Path)
        {
            var P = Normalize(Path);
            foreach (var E in Entries)
                if (E.Path == P) return E;
            return null;
        }

        private static string Normalize(string Path)
        {
            if (string.IsNullOrEmpty(Path)) return "";
            Path = Path.Replace('\\', '/');
            while (Path.StartsWith("./")) Path = Path.Substring(2);
            if (Path.StartsWith("/")) Path = Path.Substring(1);
            return Path.TrimEnd('/');
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
