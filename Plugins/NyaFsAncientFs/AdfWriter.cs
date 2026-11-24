using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Items;
using NyaFs.Filesystem.Universal.Types;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.ImageFormat.Elements.Fs.Writer;
using System;
using System.Collections.Generic;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Rebuilds a fresh 880K Amiga FFS floppy image. Root block is 880; file
    /// headers and raw FFS data blocks are allocated from block 1.
    /// </summary>
    public sealed class AdfWriter : Writer
    {
        private const int BlockSize = 512;
        private const int TotalBlocks = 1760;
        private const int RootBlock = 880;
        private const int BitmapBlock = 881;
        private const int MaxDataBlocks = 72;
        private const int RootType = 2;
        private const int FileType = -3;

        private byte[] FsData;

        public override bool HasRawStreamData => FsData != null;
        public override byte[] RawStream => FsData;

        public override bool CheckFilesystem(LinuxFilesystem Fs)
        {
            if ((Fs == null) || (Fs.Fs?.Root == null))
                return false;
            foreach (var Item in Fs.Fs.Root.Items)
            {
                if ((Item.ItemType != FilesystemItemType.File) &&
                    (Item.ItemType != FilesystemItemType.SymLink))
                    return false;
            }
            return true;
        }

        public override void WriteFs(LinuxFilesystem Fs)
        {
            if (!CheckFilesystem(Fs))
                throw new InvalidOperationException("ADF supports only a flat root directory");

            var Image = new byte[TotalBlocks * BlockSize];
            var Used = new bool[TotalBlocks];
            Used[0] = true;
            Used[RootBlock] = true;
            Used[BitmapBlock] = true;

            Image[0] = (byte)'D';
            Image[1] = (byte)'O';
            Image[2] = (byte)'S';
            Image[3] = 1; // FFS
            WriteBe32(Image, 8, RootBlock);

            int NextBlock = 2;
            var HashBuckets = new int[72];
            Array.Fill(HashBuckets, 0);
            var HashTails = new int[72];
            Array.Fill(HashTails, 0);

            foreach (var Item in Fs.Fs.Root.Items)
            {
                byte[] Content;
                if (Item is File File)
                    Content = File.Content ?? Array.Empty<byte>();
                else if (Item is SymLink Link)
                    Content = System.Text.Encoding.ASCII.GetBytes(Link.Target ?? "");
                else
                    continue;

                string Name = EncodeName(Item.Filename);
                if (Name.Length == 0)
                    throw new InvalidOperationException("Invalid Amiga filename: " + Item.Filename);

                int DataBlocks = (Content.Length + BlockSize - 1) / BlockSize;
                if (DataBlocks > MaxDataBlocks)
                    throw new InvalidOperationException("ADF file is too large: " + Item.Filename);

                int HeaderBlock = NextBlock++;
                Used[HeaderBlock] = true;
                int Header = HeaderBlock * BlockSize;
                WriteBe32(Image, Header, RootType);
                Image[Header + 432] = (byte)Name.Length;
                for (int I = 0; I < Name.Length; I++)
                    Image[Header + 433 + I] = (byte)Name[I];
                WriteBe32(Image, Header + 320, 0xFFFFFFFF);
                WriteBe32(Image, Header + 324, Content.Length);
                WriteBe32(Image, Header + 508, unchecked((uint)FileType));

                int FirstData = NextBlock;
                for (int I = 0; I < DataBlocks; I++)
                {
                    int Block = NextBlock++;
                    Used[Block] = true;
                    if (I == 0)
                        WriteBe32(Image, Header + 16, Block);
                    WriteBe32(Image, Header + 24 + (MaxDataBlocks - 1 - I) * 4, Block);
                    int Offset = I * BlockSize;
                    int Copy = Math.Min(BlockSize, Content.Length - Offset);
                    Array.Copy(Content, Offset, Image, Block * BlockSize, Copy);
                }
                if (DataBlocks == 0)
                    WriteBe32(Image, Header + 16, 0);
                WriteBe32(Image, Header + 504, 0);
                WriteBe32(Image, Header + 496, 0);

                int Bucket = Hash(Name) % HashBuckets.Length;
                if (HashBuckets[Bucket] == 0)
                {
                    HashBuckets[Bucket] = HeaderBlock;
                }
                else
                {
                    WriteBe32(Image, HashTails[Bucket] * BlockSize + 496, HeaderBlock);
                }
                HashTails[Bucket] = HeaderBlock;
            }

            int Root = RootBlock * BlockSize;
            WriteBe32(Image, Root, RootType);
            WriteBe32(Image, Root + 12, 72);
            for (int I = 0; I < 72; I++)
                WriteBe32(Image, Root + 24 + I * 4, HashBuckets[I]);
            Image[Root + 432] = 5;
            for (int I = 0; I < 5; I++)
                Image[Root + 433 + I] = (byte)"NyaFS"[I];
            WriteBe32(Image, Root + 312, 1);
            WriteBe32(Image, Root + 316, BitmapBlock);
            WriteBe32(Image, Root + 508, RootType);

            for (int I = 0; I < TotalBlocks; I++)
            {
                if (Used[I])
                    Image[BitmapBlock * BlockSize + I / 8] |= (byte)(0x80 >> (I % 8));
            }

            FsData = Image;
        }

        private static int Hash(string Name)
        {
            int Value = 0;
            foreach (char C in Name.ToUpperInvariant())
                Value = ((Value * 13) + C) & 0x7FF;
            return Value;
        }

        private static string EncodeName(string Path)
        {
            string Name = Path ?? "";
            int Slash = Name.LastIndexOfAny(new[] { '/', '\\' });
            if (Slash >= 0)
                Name = Name.Substring(Slash + 1);
            if ((Name.Length == 0) || (Name.Length > 30))
                return "";
            foreach (char C in Name)
            {
                if ((C < 0x20) || (C > 0x7E) || (C == '/') || (C == '\\'))
                    return "";
            }
            return Name;
        }

        private static void WriteBe32(byte[] Data, int Offset, int Value) =>
            WriteBe32(Data, Offset, unchecked((uint)Value));

        private static void WriteBe32(byte[] Data, int Offset, uint Value)
        {
            Data[Offset] = (byte)(Value >> 24);
            Data[Offset + 1] = (byte)(Value >> 16);
            Data[Offset + 2] = (byte)(Value >> 8);
            Data[Offset + 3] = (byte)Value;
        }
    }
}
