using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsUboot
{
    /// <summary>
    /// U-Boot environment image. Supports the non-redundant layout
    /// (crc32 | key=value...) and the redundant/flag layouts
    /// (crc32 | flags | key=value... for one or two copies).
    /// </summary>
    public class UbootEnvironment
    {
        public class Entry
        {
            public string Key;
            public string Value;

            public Entry(string Key, string Value)
            {
                this.Key = Key;
                this.Value = Value;
            }
        }

        public const int DefaultRegionSize = 0x10000;

        public List<Entry> Entries { get; } = new List<Entry>();

        /// <summary>Size of one environment sector in bytes.</summary>
        public int RegionSize { get; set; } = DefaultRegionSize;

        /// <summary>Number of redundant sectors (1 = single copy).</summary>
        public int Copies { get; set; } = 1;

        /// <summary>Offset of the first key=value byte inside a sector (4, 5 or 8).</summary>
        public int DataOffset { get; set; } = 4;

        /// <summary>Header flags (single byte for offset 5, u32 for offset 8).</summary>
        public uint Flags { get; set; }

        /// <summary>Per-copy flags, preserved when a redundant image is reloaded.</summary>
        public uint[] CopyFlags { get; set; }

        public int Count => Entries.Count;

        public string Get(string Key)
        {
            foreach (var E in Entries)
                if (E.Key == Key)
                    return E.Value;
            return null;
        }

        public void Set(string Key, string Value)
        {
            var Found = false;
            for (var I = Entries.Count - 1; I >= 0; I--)
            {
                if (Entries[I].Key != Key)
                    continue;

                if (!Found)
                {
                    Entries[I].Value = Value;
                    Found = true;
                }
                else
                    Entries.RemoveAt(I);
            }

            if (!Found)
                Entries.Add(new Entry(Key, Value));
        }

        public bool Unset(string Key)
        {
            var Removed = false;
            for (var I = Entries.Count - 1; I >= 0; I--)
            {
                if (Entries[I].Key != Key)
                    continue;
                Entries.RemoveAt(I);
                Removed = true;
            }
            return Removed;
        }

        public void Replace(IEnumerable<Entry> NewEntries)
        {
            Entries.Clear();
            Entries.AddRange(NewEntries);
        }

        public static bool TryParse(byte[] Raw, out UbootEnvironment Env, out string Error)
        {
            Env = null;
            Error = null;

            if ((Raw == null) || (Raw.Length < 0x40))
            {
                Error = "Environment image is too short";
                return false;
            }

            foreach (var Region in RegionCandidates(Raw.Length))
            {
                var Copies = Raw.Length / Region;
                if ((Copies < 1) || (Copies > 2) || (Raw.Length % Region != 0))
                    continue;

                if (!CheckCopy(Raw, 0, Region))
                    continue;

                if ((Copies == 2) && !CheckCopy(Raw, Region, Region))
                    continue;

                if (!TryReadLayout(Raw, 0, Region, out var DataOffset, out var Flags))
                    continue;

                var Result = new UbootEnvironment
                {
                    RegionSize = Region,
                    Copies = Copies,
                    DataOffset = DataOffset,
                    Flags = Flags,
                    CopyFlags = new uint[Copies]
                };

                for (var I = 0; I < Copies; I++)
                {
                    if (DataOffset == 8)
                        Result.CopyFlags[I] = ReadU32(Raw, I * Region + 4);
                    else if (DataOffset == 5)
                        Result.CopyFlags[I] = Raw[I * Region + 4];
                }

                if (!ReadEntries(Raw, 0, Region, DataOffset, Result.Entries, out Error))
                {
                    Env = null;
                    return false;
                }

                Env = Result;
                return true;
            }

            Error = "CRC32 does not match a U-Boot environment";
            return false;
        }

        private static IEnumerable<int> RegionCandidates(int Length)
        {
            var Seen = new HashSet<int>();
            var Values = new List<int> { Length, Length / 2, 0x40000, 0x20000, 0x10000, 0x8000, 0x4000, 0x2000, 0x1000 };
            foreach (var Value in Values)
            {
                if ((Value < 0x40) || (Value > Length) || !Seen.Add(Value))
                    continue;
                yield return Value;
            }
        }

        private static bool CheckCopy(byte[] Raw, int Start, int Region)
        {
            var Stored = ReadU32(Raw, Start);
            return Crc32(Raw, Start + 4, Region - 4) == Stored;
        }

        private static bool TryReadLayout(byte[] Raw, int Start, int Region, out int DataOffset, out uint Flags)
        {
            DataOffset = 4;
            Flags = 0;

            var Mark = Raw[Start + 4];
            var Mark32 = ReadU32(Raw, Start + 4);

            if ((Mark32 <= 1) && IsValidRegion(Raw, Start, Region, 8))
            {
                DataOffset = 8;
                Flags = Mark32;
                return true;
            }

            if ((Mark <= 1) && IsValidRegion(Raw, Start, Region, 5))
            {
                DataOffset = 5;
                Flags = Mark;
                return true;
            }

            return IsValidRegion(Raw, Start, Region, 4);
        }

        private static bool IsValidRegion(byte[] Raw, int Start, int Region, int DataOffset) =>
            ReadEntries(Raw, Start, Region, DataOffset, null, out _);

        private static bool ReadEntries(byte[] Raw, int Start, int Region, int DataOffset,
            List<Entry> Entries, out string Error)
        {
            Error = null;
            var Begin = Start + DataOffset;
            var End = Start + Region;

            var Term = -1;
            for (var I = Begin; I + 1 < End; I++)
            {
                if ((Raw[I] == 0) && (Raw[I + 1] == 0))
                {
                    Term = I;
                    break;
                }
            }

            if (Term < 0)
            {
                Error = "Environment has no double-NUL terminator";
                return false;
            }

            if (Term == Begin)
                return true;

            var Position = Begin;
            while (Position < Term)
            {
                var Next = Array.IndexOf(Raw, (byte)0, Position, Term - Position);
                if (Next < 0)
                    Next = Term;

                if (Next == Position)
                {
                    Error = "Environment contains an empty entry";
                    return false;
                }

                var Text = Encoding.UTF8.GetString(Raw, Position, Next - Position);
                var Eq = Text.IndexOf('=');
                if (Eq <= 0)
                {
                    Error = $"Environment entry is not key=value: '{Text}'";
                    return false;
                }

                Entries?.Add(new Entry(Text.Substring(0, Eq), Text.Substring(Eq + 1)));
                Position = Next + 1;
            }

            return true;
        }

        public bool TryParseText(string Text, out string Error)
        {
            Error = null;
            var Parsed = new List<Entry>();
            var Lines = (Text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (var I = 0; I < Lines.Length; I++)
            {
                var Line = Lines[I];
                if (Line.Length == 0)
                    continue;
                if (Line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var Eq = Line.IndexOf('=');
                if (Eq <= 0)
                {
                    Error = $"Line {I + 1}: expected key=value";
                    return false;
                }

                Parsed.Add(new Entry(Line.Substring(0, Eq), Line.Substring(Eq + 1)));
            }

            Replace(Parsed);
            return true;
        }

        public string ToText()
        {
            var Sb = new StringBuilder();
            foreach (var E in Entries)
                Sb.Append(E.Key).Append('=').Append(E.Value).Append('\n');
            return Sb.ToString();
        }

        public byte[] Build(out string Error)
        {
            Error = null;

            var Body = BuildBody();
            var Space = RegionSize - DataOffset;
            if (Space <= 0)
            {
                Error = "Environment region is too small";
                return null;
            }
            if (Body.Length > Space)
            {
                Error = $"Environment does not fit into {RegionSize} bytes ({Body.Length} > {Space})";
                return null;
            }

            if ((Copies != 1) && (Copies != 2))
            {
                Error = "Only 1 or 2 environment copies are supported";
                return null;
            }

            var Result = new byte[RegionSize * Copies];
            for (var I = 0; I < Copies; I++)
            {
                var Sector = new byte[RegionSize];
                var Flags = GetCopyFlags(I);

                if (DataOffset >= 8)
                    WriteU32(Sector, 4, Flags);
                else if (DataOffset >= 5)
                    Sector[4] = (byte)Flags;

                Buffer.BlockCopy(Body, 0, Sector, DataOffset, Body.Length);
                WriteU32(Sector, 0, Crc32(Sector, 4, RegionSize - 4));
                Buffer.BlockCopy(Sector, 0, Result, I * RegionSize, RegionSize);
            }

            return Result;
        }

        private uint GetCopyFlags(int Index)
        {
            if ((CopyFlags != null) && (Index < CopyFlags.Length))
                return CopyFlags[Index];

            if (Copies > 1)
                return (Index == 0) ? 1u : 0u;

            return Flags;
        }

        private byte[] BuildBody()
        {
            using (var Stream = new System.IO.MemoryStream())
            {
                foreach (var E in Entries)
                {
                    var Bytes = Encoding.UTF8.GetBytes(E.Key + "=" + E.Value);
                    Stream.Write(Bytes, 0, Bytes.Length);
                    Stream.WriteByte(0);
                }
                Stream.WriteByte(0);
                return Stream.ToArray();
            }
        }

        public static uint Crc32(byte[] Data, int Offset, int Count)
        {
            var Crc = 0xFFFFFFFFu;
            for (var I = 0; I < Count; I++)
            {
                Crc ^= Data[Offset + I];
                for (var Bit = 0; Bit < 8; Bit++)
                    Crc = (Crc >> 1) ^ (0xEDB88320u & (uint)(-(int)(Crc & 1)));
            }
            return ~Crc;
        }

        private static uint ReadU32(byte[] Data, int Offset) =>
            (uint)(Data[Offset] | (Data[Offset + 1] << 8) | (Data[Offset + 2] << 16) | (Data[Offset + 3] << 24));

        private static void WriteU32(byte[] Data, int Offset, uint Value)
        {
            Data[Offset] = (byte)Value;
            Data[Offset + 1] = (byte)(Value >> 8);
            Data[Offset + 2] = (byte)(Value >> 16);
            Data[Offset + 3] = (byte)(Value >> 24);
        }
    }
}
