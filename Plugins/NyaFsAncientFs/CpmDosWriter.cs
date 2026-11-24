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
    /// Rebuilds a fresh CP/M 2.2 8" (ibm-3740) image: 77 tracks, 26 sectors of
    /// 128 bytes, 1 KiB blocks, 64 directory entries.
    /// </summary>
    public sealed class CpmDosWriter : Writer
    {
        private const int SectorSize = 128;
        private const int Tracks = 77;
        private const int SectorsPerTrack = 26;
        private const int BlockSize = 1024;
        private const int DirectoryEntries = 64;
        private const int ReservedTracks = 2;
        private const int Skew = 6;
        private const int SectorsPerBlock = BlockSize / SectorSize;

        private static readonly int[] SkewTable = BuildSkewTable();
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
                throw new InvalidOperationException("CP/M supports only flat files");

            int TotalBytes = Tracks * SectorsPerTrack * SectorSize;
            var Image = new byte[TotalBytes];
            Array.Fill(Image, (byte)0xE5);

            int BlockCount = (TotalBytes - ReservedTracks * SectorsPerTrack * SectorSize) / BlockSize;
            var Directory = new byte[DirectoryEntries * 32];
            Array.Fill(Directory, (byte)0xE5);
            int Entry = 0;
            int NextBlock = 2;

            foreach (var Item in Fs.Fs.Root.Items)
            {
                byte[] Content;
                if (Item is File File)
                    Content = File.Content ?? Array.Empty<byte>();
                else if (Item is SymLink Link)
                    Content = System.Text.Encoding.ASCII.GetBytes(Link.Target ?? "");
                else
                    continue;

                if (!EncodeName(Item.Filename, out var Name, out var Ext))
                    throw new InvalidOperationException("Invalid CP/M filename: " + Item.Filename);

                int Records = (Content.Length + 127) / 128;
                int Extents = Math.Max(1, (Records + 127) / 128);
                for (int Extent = 0; Extent < Extents; Extent++)
                {
                    if (Entry >= DirectoryEntries)
                        throw new InvalidOperationException("CP/M directory is full");

                    int FirstRecord = Extent * 128;
                    int RecordsHere = Math.Min(128, Records - FirstRecord);
                    if (RecordsHere <= 0)
                        RecordsHere = 1;

                    int Blocks = (RecordsHere * 128 + BlockSize - 1) / BlockSize;
                    if ((NextBlock + Blocks > BlockCount) || (Blocks > 16))
                        throw new InvalidOperationException("CP/M image is full");

                    int DirOffset = Entry * 32;
                    Directory[DirOffset] = 0;
                    for (int I = 0; I < 8; I++) Directory[DirOffset + 1 + I] = (byte)Name[I];
                    for (int I = 0; I < 3; I++) Directory[DirOffset + 9 + I] = (byte)Ext[I];
                    Directory[DirOffset + 12] = (byte)(Extent & 0x1F);
                    Directory[DirOffset + 13] = 0;
                    Directory[DirOffset + 14] = (byte)((Extent >> 5) & 0x3F);
                    Directory[DirOffset + 15] = (byte)RecordsHere;

                    for (int Block = 0; Block < Blocks; Block++)
                    {
                        Directory[DirOffset + 16 + Block] = (byte)(NextBlock + Block);
                        var BlockData = new byte[BlockSize];
                        Array.Fill(BlockData, (byte)0x1A);
                        int Start = (FirstRecord + Block * (BlockSize / 128)) * 128;
                        int Count = Math.Min(BlockSize, Content.Length - Start);
                        if (Count > 0)
                            Array.Copy(Content, Start, BlockData, 0, Count);
                        WriteBlock(Image, NextBlock + Block, BlockData);
                    }

                    NextBlock += Blocks;
                    Entry++;
                }
            }

            WriteBlock(Image, 0, Slice(Directory, 0, BlockSize));
            WriteBlock(Image, 1, Slice(Directory, BlockSize, BlockSize));
            FsData = Image;
        }

        private static void WriteBlock(byte[] Image, int Block, byte[] Data)
        {
            int FirstSector = Block * SectorsPerBlock + ReservedTracks * SectorsPerTrack;
            int Track = FirstSector / SectorsPerTrack;
            int Sector = FirstSector % SectorsPerTrack;
            for (int I = 0; I < SectorsPerBlock; I++)
            {
                int Physical = SkewTable[Sector];
                long Offset = ((long)Track * SectorsPerTrack + Physical) * SectorSize;
                Array.Copy(Data, I * SectorSize, Image, Offset, SectorSize);
                Sector++;
                if (Sector >= SectorsPerTrack)
                {
                    Sector = 0;
                    Track++;
                }
            }
        }

        private static byte[] Slice(byte[] Data, int Offset, int Length)
        {
            var Result = new byte[Length];
            Array.Copy(Data, Offset, Result, 0, Length);
            return Result;
        }

        private static int[] BuildSkewTable()
        {
            var Result = new int[SectorsPerTrack];
            int Physical = 0;
            for (int I = 0; I < SectorsPerTrack; I++)
            {
                while (Array.IndexOf(Result, Physical, 0, I) >= 0)
                    Physical = (Physical + 1) % SectorsPerTrack;
                Result[I] = Physical;
                Physical = (Physical + Skew) % SectorsPerTrack;
            }
            return Result;
        }

        private static bool EncodeName(string Path, out char[] Name, out char[] Ext)
        {
            Name = new char[8];
            Ext = new char[3];
            Array.Fill(Name, ' ');
            Array.Fill(Ext, ' ');
            string Text = Path ?? "";
            int Slash = Text.LastIndexOfAny(new[] { '/', '\\' });
            if (Slash >= 0)
                Text = Text.Substring(Slash + 1);
            string Base = Text;
            string Extension = "";
            int Dot = Text.LastIndexOf('.');
            if (Dot >= 0)
            {
                Base = Text.Substring(0, Dot);
                Extension = Text.Substring(Dot + 1);
            }
            if ((Base.Length == 0) || (Base.Length > 8) || (Extension.Length > 3))
                return false;

            Base = Base.ToUpperInvariant();
            Extension = Extension.ToUpperInvariant();
            for (int I = 0; I < Base.Length; I++)
                Name[I] = IsValid(Base[I]) ? Base[I] : '_';
            for (int I = 0; I < Extension.Length; I++)
                Ext[I] = IsValid(Extension[I]) ? Extension[I] : '_';
            return true;
        }

        private static bool IsValid(char C) =>
            ((C >= 'A') && (C <= 'Z')) ||
            ((C >= '0') && (C <= '9')) ||
            (C == '_') || (C == '-') || (C == '$');
    }
}
