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
    /// Rebuilds a fresh 140K ProDOS 8 volume (280 blocks of 512 bytes).
    /// Supports a flat root directory, seedling/sapling/tree file storage and
    /// a standard volume bitmap.
    /// </summary>
    public sealed class ProDosWriter : Writer
    {
        private const int BlockSize = 512;
        private const int TotalBlocks = 280;
        private const int RootBlock = 2;
        private const int BitmapBlock = 6;
        private const int FirstDataBlock = 7;
        private const int EntrySize = 39;
        private const int EntriesPerBlock = 13;

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
                throw new InvalidOperationException("ProDOS supports only a flat root directory");

            var Image = new byte[TotalBlocks * BlockSize];
            var Used = new bool[TotalBlocks];
            for (int I = 0; I <= BitmapBlock; I++)
                Used[I] = true;

            int NextBlock = FirstDataBlock;
            int EntryIndex = 0;
            int FileCount = 0;

            foreach (var Item in Fs.Fs.Root.Items)
            {
                if (EntryIndex >= 51)
                    throw new InvalidOperationException("ProDOS root directory is full");

                byte[] Content;
                if (Item is File File)
                    Content = File.Content ?? Array.Empty<byte>();
                else if (Item is SymLink Link)
                    Content = System.Text.Encoding.ASCII.GetBytes(Link.Target ?? "");
                else
                    continue;

                string Name = EncodeName(Item.Filename);
                if (Name.Length == 0)
                    throw new InvalidOperationException("Invalid ProDOS filename: " + Item.Filename);

                int DataBlocks = (Content.Length + BlockSize - 1) / BlockSize;
                int Storage;
                int KeyBlock;
                int BlocksUsed;

                if (DataBlocks <= 1)
                {
                    Storage = 1;
                    KeyBlock = NextBlock;
                    BlocksUsed = 1;
                    EnsureBlock(Used, NextBlock);
                    if (Content.Length > 0)
                        Array.Copy(Content, 0, Image, KeyBlock * BlockSize, Content.Length);
                    NextBlock++;
                }
                else if (DataBlocks <= 256)
                {
                    Storage = 2;
                    KeyBlock = NextBlock++;
                    BlocksUsed = 1 + DataBlocks;
                    EnsureBlock(Used, KeyBlock);
                    WriteIndex(Image, KeyBlock, Content, ref NextBlock, Used, false);
                }
                else
                {
                    Storage = 3;
                    KeyBlock = NextBlock++;
                    BlocksUsed = 1;
                    EnsureBlock(Used, KeyBlock);
                    int IndexBlocks = (DataBlocks + 255) / 256;
                    int DataIndex = 0;
                    for (int I = 0; I < IndexBlocks; I++)
                    {
                        int IndexBlock = NextBlock++;
                        EnsureBlock(Used, IndexBlock);
                        WritePointer(Image, KeyBlock, 128, I, IndexBlock);
                        int Count = Math.Min(256, DataBlocks - I * 256);
                        WriteDataBlocks(Image, IndexBlock, Content, ref DataIndex, Count,
                            ref NextBlock, Used, ref BlocksUsed);
                    }
                    BlocksUsed += IndexBlocks;
                }

                int EntryBlock;
                int EntryOffset;
                if (EntryIndex < 12)
                {
                    EntryBlock = RootBlock;
                    EntryOffset = 4 + EntrySize + EntryIndex * EntrySize;
                }
                else
                {
                    int Index = EntryIndex - 12;
                    EntryBlock = 3 + Index / EntriesPerBlock;
                    EntryOffset = 4 + (Index % EntriesPerBlock) * EntrySize;
                }

                WriteEntry(Image, EntryBlock, EntryOffset, Storage, Name, KeyBlock,
                    BlocksUsed, Content.Length);
                EntryIndex++;
                FileCount++;
            }

            WriteVolumeDirectory(Image, EntryIndex);
            WriteBitmap(Image, Used);
            FsData = Image;
        }

        private static void WriteIndex(byte[] Image, int IndexBlock, byte[] Content,
            ref int NextBlock, bool[] Used, bool Master)
        {
            int Count = Master ? 128 : 256;
            int DataBlock = 0;
            for (int I = 0; I < Count; I++)
            {
                int Offset = DataBlock * BlockSize;
                if (Offset >= Content.Length)
                    break;
                int Block = NextBlock++;
                EnsureBlock(Used, Block);
                WritePointer(Image, IndexBlock, Count, I, Block);
                int Copy = Math.Min(BlockSize, Content.Length - Offset);
                Array.Copy(Content, Offset, Image, Block * BlockSize, Copy);
                DataBlock++;
            }
        }

        private static void WriteDataBlocks(byte[] Image, int IndexBlock, byte[] Content,
            ref int DataIndex, int Count, ref int NextBlock, bool[] Used, ref int BlocksUsed)
        {
            for (int I = 0; I < Count; I++)
            {
                int Block = NextBlock++;
                EnsureBlock(Used, Block);
                WritePointer(Image, IndexBlock, 256, I, Block);
                int Offset = DataIndex * BlockSize;
                int Copy = Math.Min(BlockSize, Content.Length - Offset);
                if (Copy > 0)
                    Array.Copy(Content, Offset, Image, Block * BlockSize, Copy);
                DataIndex++;
                BlocksUsed++;
            }
        }

        private static void EnsureBlock(bool[] Used, int Block)
        {
            if ((Block < 0) || (Block >= TotalBlocks) || Used[Block])
                throw new InvalidOperationException("ProDOS image is full");
            Used[Block] = true;
        }

        private static void WritePointer(byte[] Image, int Block, int Count, int Index, int Pointer)
        {
            int Offset = Block * BlockSize;
            Image[Offset + Index] = (byte)Pointer;
            Image[Offset + Count + Index] = (byte)(Pointer >> 8);
        }

        private static void WriteEntry(byte[] Image, int Block, int Offset, int Storage,
            string Name, int KeyBlock, int BlocksUsed, int Eof)
        {
            int O = Block * BlockSize + Offset;
            Image[O] = (byte)((Storage << 4) | Name.Length);
            for (int I = 0; I < 15; I++)
                Image[O + 1 + I] = (byte)(I < Name.Length ? Name[I] : ' ');
            Image[O + 16] = (byte)FileType(Name);
            Image[O + 17] = (byte)KeyBlock;
            Image[O + 18] = (byte)(KeyBlock >> 8);
            Image[O + 19] = (byte)BlocksUsed;
            Image[O + 20] = (byte)(BlocksUsed >> 8);
            Image[O + 21] = (byte)Eof;
            Image[O + 22] = (byte)(Eof >> 8);
            Image[O + 23] = (byte)(Eof >> 16);
            Image[O + 30] = 0xE3;
            Image[O + 37] = (byte)RootBlock;
            Image[O + 38] = 0;
        }

        private static void WriteVolumeDirectory(byte[] Image, int FileCount)
        {
            int Header = RootBlock * BlockSize + 4;
            string Name = "NYAFS";
            Image[Header] = (byte)(0xF0 | Name.Length);
            for (int I = 0; I < 15; I++)
                Image[Header + 1 + I] = (byte)(I < Name.Length ? Name[I] : ' ');
            Image[Header + 31] = EntrySize;
            Image[Header + 32] = EntriesPerBlock;
            Image[Header + 33] = (byte)FileCount;
            Image[Header + 34] = (byte)(FileCount >> 8);
            Image[Header + 35] = (byte)BitmapBlock;
            Image[Header + 36] = 0;
            Image[Header + 37] = (byte)(TotalBlocks & 0xFF);
            Image[Header + 38] = (byte)(TotalBlocks >> 8);

            for (int Block = RootBlock; Block <= 5; Block++)
            {
                int O = Block * BlockSize;
                Image[O] = (byte)(Block == RootBlock ? 0 : Block - 1);
                Image[O + 1] = (byte)(Block == RootBlock ? 0 : (Block - 1) >> 8);
                int Next = Block < 5 ? Block + 1 : 0;
                Image[O + 2] = (byte)Next;
                Image[O + 3] = (byte)(Next >> 8);
            }
        }

        private static void WriteBitmap(byte[] Image, bool[] Used)
        {
            int Offset = BitmapBlock * BlockSize;
            for (int I = 0; I < TotalBlocks; I++)
            {
                if (!Used[I])
                    continue;
                Image[Offset + I / 8] |= (byte)(0x80 >> (I % 8));
            }
        }

        private static int FileType(string Name)
        {
            string Upper = Name.ToUpperInvariant();
            if (Upper.EndsWith(".TXT") || Upper.EndsWith(".T"))
                return 0x04;
            if (Upper.EndsWith(".BAS"))
                return 0xFC;
            if (Upper.EndsWith(".SYS"))
                return 0x06;
            return 0x06;
        }

        private static string EncodeName(string Path)
        {
            string Name = Path ?? "";
            int Slash = Name.LastIndexOfAny(new[] { '/', '\\' });
            if (Slash >= 0)
                Name = Name.Substring(Slash + 1);
            Name = Name.ToUpperInvariant();
            if ((Name.Length == 0) || (Name.Length > 15))
                return "";
            foreach (char C in Name)
            {
                if ((C < 0x20) || (C > 0x7E) || (C == '/') || (C == '\\'))
                    return "";
            }
            return Name;
        }
    }
}
