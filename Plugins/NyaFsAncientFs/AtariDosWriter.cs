using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Items;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.ImageFormat.Elements.Fs.Writer;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;

namespace NyaFsAncientFs
{
    /// <summary>
    /// Rebuilds a fresh Atari DOS 2.x ATR image from a filesystem tree. The
    /// format is flat: files are placed contiguously from sector 4, with the
    /// 64-entry directory at sector 360.
    /// </summary>
    public sealed class AtariDosWriter : Writer
    {
        private const int HeaderSize = 16;
        private const int SectorSize = 128;
        private const int SectorCount = 720;
        private const int DirectorySector = 360;
        private const int DataStartSector = 4;
        private const int DirectoryEntries = 64;

        private byte[] FsData;

        public override bool HasRawStreamData => FsData != null;
        public override byte[] RawStream => FsData;

        public override bool CheckFilesystem(LinuxFilesystem Fs)
        {
            if ((Fs == null) || (Fs.Fs?.Root == null))
                return false;

            foreach (var Item in Fs.Fs.Root.Items)
            {
                if (Item.ItemType == FilesystemItemType.Directory)
                    return false;
                if ((Item.ItemType != FilesystemItemType.File) && (Item.ItemType != FilesystemItemType.SymLink))
                    return false;
            }
            return true;
        }

        public override void WriteFs(LinuxFilesystem Fs)
        {
            if (!CheckFilesystem(Fs))
                throw new InvalidOperationException("Atari DOS supports only flat files");

            var Data = new byte[HeaderSize + SectorCount * SectorSize];
            Array.Fill(Data, (byte)0);

            int TotalSectors = (Data.Length - HeaderSize) / SectorSize;
            Data[0] = 0x96;
            Data[1] = 0x02;
            int Paragraphs = (Data.Length - HeaderSize) / 16;
            Data[2] = (byte)Paragraphs;
            Data[3] = (byte)(Paragraphs >> 8);
            Data[4] = (byte)SectorSize;
            Data[5] = (byte)(SectorSize >> 8);
            Data[6] = (byte)TotalSectors;
            Data[7] = (byte)(TotalSectors >> 8);

            int NextSector = DataStartSector;
            int Entry = HeaderSize + DirectorySector * SectorSize;
            int Count = 0;

            foreach (var Item in Fs.Fs.Root.Items)
            {
                if (Count >= DirectoryEntries)
                    throw new InvalidOperationException("Atari DOS directory is full");

                byte[] Content;
                if (Item is File File)
                    Content = File.Content ?? Array.Empty<byte>();
                else if (Item is SymLink Link)
                    Content = System.Text.Encoding.ASCII.GetBytes(Link.Target ?? "");
                else
                    continue;

                int Sectors = (Content.Length + SectorSize - 1) / SectorSize;
                if ((NextSector + Sectors > DirectorySector) || (NextSector + Sectors > TotalSectors))
                    throw new InvalidOperationException("Atari DOS image is full");

                string Name = EncodeName(Item.Filename, out bool Valid);
                if (!Valid)
                    throw new InvalidOperationException("Invalid Atari DOS filename: " + Item.Filename);

                Data[Entry] = 0x40;
                Data[Entry + 1] = (byte)Sectors;
                Data[Entry + 2] = (byte)(Sectors >> 8);
                Data[Entry + 3] = (byte)NextSector;
                Data[Entry + 4] = (byte)(NextSector >> 8);
                for (int I = 0; I < 11; I++)
                    Data[Entry + 5 + I] = (byte)Name[I];

                int Offset = HeaderSize + NextSector * SectorSize;
                Array.Copy(Content, 0, Data, Offset, Content.Length);
                NextSector += Sectors;
                Entry += 16;
                Count++;
            }

            FsData = Data;
        }

        private static string EncodeName(string Path, out bool Valid)
        {
            Valid = true;
            string Name = Path ?? "";
            int Slash = Name.LastIndexOf('/');
            if (Slash >= 0)
                Name = Name.Substring(Slash + 1);
            Slash = Name.LastIndexOf('\\');
            if (Slash >= 0)
                Name = Name.Substring(Slash + 1);

            string Base = Name;
            string Ext = "";
            int Dot = Name.LastIndexOf('.');
            if (Dot >= 0)
            {
                Base = Name.Substring(0, Dot);
                Ext = Name.Substring(Dot + 1);
            }

            Base = Base.ToUpperInvariant();
            Ext = Ext.ToUpperInvariant();
            if ((Base.Length == 0) || (Base.Length > 8) || (Ext.Length > 3))
                Valid = false;

            var Result = new char[11];
            Array.Fill(Result, ' ');
            for (int I = 0; I < Math.Min(8, Base.Length); I++)
                Result[I] = IsValid(Base[I]) ? Base[I] : '_';
            for (int I = 0; I < Math.Min(3, Ext.Length); I++)
                Result[8 + I] = IsValid(Ext[I]) ? Ext[I] : '_';
            return new string(Result);
        }

        private static bool IsValid(char C) =>
            ((C >= 'A') && (C <= 'Z')) ||
            ((C >= '0') && (C <= '9')) ||
            (C == '_') || (C == '-') || (C == '$');
    }
}
