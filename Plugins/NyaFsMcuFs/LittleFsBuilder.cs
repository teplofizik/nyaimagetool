using System;
using System.Collections.Generic;
using System.Text;
using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;

namespace NyaFsMcuFs
{
    /// <summary>
    /// littlefs v2 builder (format + write). Files are stored as CTZ skip-lists,
    /// directories as metadata pairs. Produces a single-block-per-metadata commit
    /// (ccrc) so the result is readable by littlefs/readmdir.
    /// </summary>
    public class LittleFsBuilder : IFilesystemBuilder
    {
        private class Node
        {
            public string Name;
            public bool IsDir;
            public byte[] Content;
            public readonly List<Node> Children = new List<Node>();

            public long Pair1;
            public long Pair2;
            public long CtzStart; // physical block of file block 0
            public int CtzBlocks;
        }

        private readonly Node Root = new Node { Name = "", IsDir = true };
        private readonly Node Current;

        private int BlockSize;
        private long BlockCount;

        private long NextFree = 2; // blocks 0,1 are the root metadata pair

        public LittleFsBuilder(int BlockSize = 4096, long BlockCount = 0)
        {
            this.BlockSize = (BlockSize > 0) ? BlockSize : 4096;
            this.BlockCount = BlockCount;
            Current = Root;
        }

        private Node GetDir(string Path)
        {
            var Parts = Path.Replace('\\', '/').Split('/');
            var N = Root;
            foreach (var P in Parts)
            {
                if (P.Length == 0) continue;
                Node Next = null;
                foreach (var C in N.Children)
                    if (C.IsDir && (C.Name == P)) { Next = C; break; }
                if (Next == null) { Next = new Node { Name = P, IsDir = true }; N.Children.Add(Next); }
                N = Next;
            }
            return N;
        }

        public void Directory(string Path, uint User, uint Group, uint Mode)
        {
            var P = Normalize(Path);
            if (P.Length == 0) return;
            GetDir(P);
        }

        public void File(string Path, byte[] Content, uint User, uint Group, uint Mode)
        {
            var P = Normalize(Path);
            var Idx = P.LastIndexOf('/');
            var Dir = GetDir((Idx < 0) ? "" : P.Substring(0, Idx));
            Dir.Children.Add(new Node { Name = (Idx < 0) ? P : P.Substring(Idx + 1), IsDir = false, Content = Content ?? new byte[0] });
        }

        public void SymLink(string Path, string Target, uint User, uint Group, uint Mode) { }
        public void Block(string Path, uint Major, uint Minor, uint User, uint Group, uint Mode) { }
        public void Char(string Path, uint Major, uint Minor, uint User, uint Group, uint Mode) { }
        public void Fifo(string Path, uint User, uint Group, uint Mode) { }
        public void Socket(string Path, uint User, uint Group, uint Mode) { }

        private static string Normalize(string Path)
        {
            if (string.IsNullOrEmpty(Path)) return "";
            Path = Path.Replace('\\', '/');
            while (Path.StartsWith("./")) Path = Path.Substring(2);
            if (Path.StartsWith("/")) Path = Path.Substring(1);
            return Path.Trim('/');
        }

        private static int Ctz(int V) { int N = 0; while (((V & 1) == 0) && (N < 31)) { V >>= 1; N++; } return N; }

        private int CtzBlocks(long Size)
        {
            int N = 0; long Cap = 0;
            while (Cap < Size) { int Hdr = (N == 0) ? 0 : (Ctz(N) + 1) * 4; Cap += BlockSize - Hdr; N++; }
            return N;
        }

        private (long, long) AllocPair()
        {
            var Res = (NextFree, NextFree + 1);
            NextFree += 2;
            return Res;
        }

        // ---- CRC32 (littlefs uses the standard CRC32) ----

        private static readonly uint[] CrcTable = BuildCrcTable();

        private static uint[] BuildCrcTable()
        {
            var T = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint C = i;
                for (int k = 0; k < 8; k++) C = ((C & 1) != 0) ? (0xEDB88320u ^ (C >> 1)) : (C >> 1);
                T[i] = C;
            }
            return T;
        }

        private static uint CalcCrc(uint Crc, byte[] B, int Off, int Len)
        {
            Crc ^= 0xFFFFFFFF;
            for (int i = 0; i < Len; i++)
            {
                Crc ^= B[Off + i];
                for (int k = 0; k < 8; k++) Crc = ((Crc & 1) != 0) ? (0xEDB88320u ^ (Crc >> 1)) : (Crc >> 1);
            }
            return Crc ^ 0xFFFFFFFF;
        }

        private static uint CalcCrc(uint Crc, uint Value)
        {
            var B = new byte[4];
            B[0] = (byte)Value; B[1] = (byte)(Value >> 8); B[2] = (byte)(Value >> 16); B[3] = (byte)(Value >> 24);
            return CalcCrc(Crc, B, 0, 4);
        }

        private static void WriteU32BE(byte[] B, int Off, uint V)
        {
            B[Off] = (byte)(V >> 24); B[Off + 1] = (byte)(V >> 16); B[Off + 2] = (byte)(V >> 8); B[Off + 3] = (byte)V;
        }

        private static void WriteU32LE(byte[] B, int Off, uint V)
        {
            B[Off] = (byte)V; B[Off + 1] = (byte)(V >> 8); B[Off + 2] = (byte)(V >> 16); B[Off + 3] = (byte)(V >> 24);
        }

        private static uint Mktag(int Type, int Id, int Size) => (uint)((Type << 20) | (Id << 10) | Size);

        // ---- block assembly ----

        private class Tag
        {
            public uint Logical;
            public byte[] Data;
        }

        private byte[] BuildMetadataBlock(List<Tag> Tags)
        {
            var Block = new byte[BlockSize];
            for (int i = 0; i < BlockSize; i++) Block[i] = 0xFF;

            WriteU32LE(Block, 0, 1); // rev 1
            uint CrcVal = CalcCrc(0, Block, 0, 4);

            int Off = 4;
            uint Prev = 0xFFFFFFFF;

            foreach (var T in Tags)
            {
                var Stored = T.Logical ^ Prev;
                WriteU32BE(Block, Off, Stored);

                int Len = T.Data?.Length ?? 0;
                if (Len > 0) Array.Copy(T.Data, 0, Block, Off + 4, Len);

                CrcVal = CalcCrc(CrcVal, Block, Off, 4 + Len);
                Prev = T.Logical;
                Off += 4 + Len;
            }

            // commit: ccrc tag (data = LE(crc_after_header ^ 0xFFFFFFFF))
            uint CcrcLogical = Mktag(0x501, 0x3FF, 4);
            WriteU32BE(Block, Off, CcrcLogical ^ Prev);

            uint CrcHdr = CalcCrc(CrcVal, Block, Off, 4);
            uint Data4 = CrcHdr ^ 0xFFFFFFFF;
            WriteU32LE(Block, Off + 4, Data4);

            return Block;
        }

        private void Assign(Node Dir)
        {
            if (Dir != Root) { var P = AllocPair(); Dir.Pair1 = P.Item1; Dir.Pair2 = P.Item2; }

            foreach (var C in Dir.Children)
            {
                if (C.IsDir) Assign(C);
                else if ((C.Content.Length > 0))
                {
                    C.CtzBlocks = CtzBlocks(C.Content.Length);
                    C.CtzStart = NextFree;
                    NextFree += C.CtzBlocks;
                }
            }
        }

        public byte[] GetFilesystemImage()
        {
            Assign(Root);

            if (BlockCount <= 0)
                BlockCount = NextFree;

            var Image = new byte[BlockSize * BlockCount];
            for (long i = 0; i < Image.Length; i++) Image[i] = 0xFF;

            // root metadata pair = blocks 0,1
            WriteDir(Root, 0, 1, Image);

            // files (CTZ)
            WriteFiles(Root, Image);

            return Image;
        }

        private void WriteDir(Node Dir, long Block1, long Block2, byte[] Image)
        {
            var Tags = new List<Tag>();

            if (Dir == Root)
            {
                Tags.Add(new Tag { Logical = Mktag(0x0FF, 0, 8), Data = Encoding.ASCII.GetBytes("littlefs") });

                var Sb = new byte[24];
                WriteU32LE(Sb, 0, 0x00020000); // version 2.0
                WriteU32LE(Sb, 4, (uint)BlockSize);
                WriteU32LE(Sb, 8, (uint)BlockCount);
                WriteU32LE(Sb, 12, 32);            // name_max
                WriteU32LE(Sb, 16, 0x7FFFFFFF);    // file_max
                WriteU32LE(Sb, 20, 1022);          // attr_max
                Tags.Add(new Tag { Logical = Mktag(0x201, 0, 24), Data = Sb });
            }

            int Id = (Dir == Root) ? 1 : 0;

            foreach (var C in Dir.Children)
            {
                Tags.Add(new Tag { Logical = Mktag(0x401, Id, 0), Data = new byte[0] });

                var Name = Encoding.UTF8.GetBytes(C.Name);
                Tags.Add(new Tag { Logical = Mktag(C.IsDir ? 0x002 : 0x001, Id, Name.Length), Data = Name });

                if (C.IsDir)
                {
                    var Ds = new byte[8];
                    WriteU32LE(Ds, 0, (uint)C.Pair1);
                    WriteU32LE(Ds, 4, (uint)C.Pair2);
                    Tags.Add(new Tag { Logical = Mktag(0x200, Id, 8), Data = Ds });
                }
                else if (C.Content.Length == 0)
                {
                    Tags.Add(new Tag { Logical = Mktag(0x201, Id, 0), Data = new byte[0] });
                }
                else
                {
                    var Cs = new byte[8];
                    WriteU32LE(Cs, 0, (uint)(C.CtzStart + C.CtzBlocks - 1)); // head
                    WriteU32LE(Cs, 4, (uint)C.Content.Length);
                    Tags.Add(new Tag { Logical = Mktag(0x202, Id, 8), Data = Cs });
                }

                Id++;
            }

            var Block = BuildMetadataBlock(Tags);
            Array.Copy(Block, 0, Image, Block1 * BlockSize, BlockSize);
            Array.Copy(Block, 0, Image, Block2 * BlockSize, BlockSize);

            // recurse into subdirectories
            foreach (var C in Dir.Children)
                if (C.IsDir)
                    WriteDir(C, C.Pair1, C.Pair2, Image);
        }

        private void WriteFiles(Node Dir, byte[] Image)
        {
            foreach (var C in Dir.Children)
            {
                if (C.IsDir) { WriteFiles(C, Image); continue; }
                if (C.Content.Length == 0) continue;

                int N = C.CtzBlocks;
                for (int j = 0; j < N; j++)
                {
                    long Phys = C.CtzStart + j;
                    int Hdr = (j == 0) ? 0 : (Ctz(j) + 1) * 4;
                    int Base = (int)(Phys * BlockSize);

                    // back-pointers: pointer to j-2^x for each x where 2^x divides j
                    int P = 0;
                    if (j > 0)
                    {
                        for (int x = 0; x <= 31; x++)
                        {
                            if ((j % (1 << x)) != 0) break;
                            WriteU32LE(Image, Base + P * 4, (uint)(C.CtzStart + j - (1 << x)));
                            P++;
                        }
                    }

                    int DataOff = Base + Hdr;
                    int Cap = BlockSize - Hdr;
                    int Remaining = C.Content.Length - (int)FileOffset(j, N);
                    int Take = Math.Min(Cap, Remaining);
                    Array.Copy(C.Content, FileOffset(j, N), Image, DataOff, Take);
                }
            }
        }

        private long FileOffset(int j, int N)
        {
            long Off = 0;
            for (int i = 0; i < j; i++) Off += BlockSize - ((i == 0) ? 0 : (Ctz(i) + 1) * 4);
            return Off;
        }
    }
}
