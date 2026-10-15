using System;
using System.Collections.Generic;
using System.Text;
using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Types;

namespace NyaFsMcuFs
{
    /// <summary>
    /// SPIFFS builder (format + write). Default geometry: page 256, block 4096.
    /// Directories are implicit (SPIFFS stores full paths as object names).
    /// </summary>
    public class SpiffsBuilder : IFilesystemBuilder
    {
        private const int PageSize = 256;
        private const int BlockSize = 4096;
        private const int PagesPerBlock = BlockSize / PageSize;   // 16
        private const int PageHeaderSize = 5;
        private const int IxHeaderSize = 45;
        private const int NameLen = 32;
        private const int DataSize = PageSize - PageHeaderSize;   // 251
        private const int HeaderIxLen = (PageSize - IxHeaderSize) / 2; // 105

        private const int IxFlag = 0x8000;
        private const byte TypeFile = 1;

        private class FileEntry
        {
            public string Name;
            public byte[] Content;
        }

        private readonly List<FileEntry> Files = new List<FileEntry>();
        private int BlockCount;

        public SpiffsBuilder(long Size = 0) => BlockCount = (Size > 0) ? (int)(Size / BlockSize) : 0;

        public void File(string Path, byte[] Content, uint User, uint Group, uint Mode)
        {
            var P = Normalize(Path);
            if (P.Length == 0) return;
            Files.Add(new FileEntry { Name = "/" + P, Content = Content ?? new byte[0] });
        }

        public void Directory(string Path, uint User, uint Group, uint Mode) { }
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

        private static void WriteU16(byte[] B, int Off, ushort V)
        {
            B[Off] = (byte)V; B[Off + 1] = (byte)(V >> 8);
        }

        private static void WriteU32(byte[] B, int Off, uint V)
        {
            B[Off] = (byte)V; B[Off + 1] = (byte)(V >> 8); B[Off + 2] = (byte)(V >> 16); B[Off + 3] = (byte)(V >> 24);
        }

        private static void SetLookup(byte[] Image, int Page, ushort Value)
        {
            int Blk = Page / PagesPerBlock;
            int E = (Page % PagesPerBlock) - 1;
            if (E < 0) return;
            WriteU16(Image, Blk * BlockSize + E * 2, Value);
        }

        public byte[] GetFilesystemImage()
        {
            int NextPage = 0;
            int AllocPage()
            {
                do { NextPage++; } while ((NextPage % PagesPerBlock) == 0);
                return NextPage;
            }

            var Objs = new List<(int Id, string Name, byte[] Content, int IxPage, List<int> DataPages)>();

            int Id = 1;
            foreach (var F in Files)
            {
                int IxPage = AllocPage();
                int Spans = (F.Content.Length + DataSize - 1) / DataSize;
                var DataPages = new List<int>();
                for (int s = 0; s < Spans; s++) DataPages.Add(AllocPage());

                Objs.Add((Id++, F.Name, F.Content, IxPage, DataPages));
            }

            int Blocks = (NextPage / PagesPerBlock) + 1;
            if (BlockCount < Blocks) BlockCount = Blocks;

            var Image = new byte[(long)BlockCount * BlockSize];
            for (long i = 0; i < Image.Length; i++) Image[i] = 0xFF;

            foreach (var O in Objs)
            {
                int Po = O.IxPage * PageSize;

                WriteU16(Image, Po, (ushort)(O.Id | IxFlag));
                WriteU16(Image, Po + 2, 0);          // span_ix
                Image[Po + 4] = 0xF8;                // flags: USED=0 FINAL=0 INDEX=0
                Image[Po + 5] = 0; Image[Po + 6] = 0; Image[Po + 7] = 0; // align[3]
                WriteU32(Image, Po + 8, (uint)O.Content.Length);
                Image[Po + 12] = TypeFile;

                var Name = Encoding.UTF8.GetBytes(O.Name);
                int NameCopy = Math.Min(Name.Length, NameLen);
                Array.Copy(Name, 0, Image, Po + 13, NameCopy);
                for (int i = NameCopy; i < NameLen; i++) Image[Po + 13 + i] = 0; // NUL padding

                for (int s = 0; s < O.DataPages.Count; s++)
                    WriteU16(Image, Po + IxHeaderSize + s * 2, (ushort)O.DataPages[s]);

                for (int s = 0; s < O.DataPages.Count; s++)
                {
                    int Dp = O.DataPages[s] * PageSize;
                    WriteU16(Image, Dp, (ushort)O.Id);
                    WriteU16(Image, Dp + 2, (ushort)s);
                    Image[Dp + 4] = 0xFC;            // flags: USED=0 FINAL=0 INDEX=1

                    int Off = s * DataSize;
                    int Take = Math.Min(DataSize, O.Content.Length - Off);
                    if (Take > 0) Array.Copy(O.Content, Off, Image, Dp + PageHeaderSize, Take);
                }

                SetLookup(Image, O.IxPage, (ushort)(O.Id | IxFlag));
                foreach (var Dp in O.DataPages) SetLookup(Image, Dp, (ushort)O.Id);
            }

            // per-block magic (SPIFFS_USE_MAGIC_LENGTH) and erase count at the end of the lookup page
            for (int Bix = 0; Bix < BlockCount; Bix++)
            {
                long Base = (long)Bix * BlockSize;
                ushort Magic = (ushort)((0x20140529 ^ PageSize ^ (BlockCount - Bix)) & 0xFFFF);
                WriteU16(Image, (int)(Base + PageSize - 4), Magic); // magic
                WriteU16(Image, (int)(Base + PageSize - 2), 0);     // erase count
            }

            return Image;
        }
    }
}
