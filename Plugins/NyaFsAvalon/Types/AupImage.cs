using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsAvalon.Types
{
    /// <summary>
    /// Avalon Upgrade Package (AUP) v0/v1/v2 header and payload.
    /// https://github.com/HashSource/avalon_upgrade_package_extract
    /// </summary>
    public class AupImage
    {
        public const int CommonHeaderSize = 20;
        public const int FixedStringSize = 32;

        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("AUP format\0\0\0\0\0\0");

        public readonly bool Correct;
        public readonly uint Version;
        public readonly uint HeaderSize;
        public readonly uint PayloadLength;
        public readonly uint PayloadCrc;
        public readonly uint HeaderCrc;
        public readonly string FirmwareVersion = "";
        public readonly string[] HardwareTypes = Array.Empty<string>();
        public readonly string[] SoftwareTypes = Array.Empty<string>();
        public readonly byte[] Payload;
        public readonly bool PayloadCrcValid;
        public readonly bool HeaderCrcValid;

        public AupImage(byte[] Data)
        {
            if ((Data == null) || (Data.Length < CommonHeaderSize))
                return;

            for (var I = 0; I < Magic.Length; I++)
                if (Data[I] != Magic[I])
                    return;

            Version = ReadU32(Data, 16);
            if (Version > 2)
                return;

            uint PayloadOffset;
            switch (Version)
            {
                case 0:
                    if (Data.Length < 92)
                        return;
                    PayloadLength = ReadU32(Data, 20);
                    FirmwareVersion = ReadString(Data, 24, 64);
                    PayloadCrc = ReadU32(Data, 88);
                    HeaderSize = 92;
                    PayloadOffset = 92;
                    break;

                case 1:
                    if (Data.Length < 224)
                        return;
                    var HeaderLen = ReadU32(Data, 20);
                    HardwareTypes = SplitList(ReadString(Data, 24, 128));
                    PayloadLength = ReadU32(Data, 152);
                    FirmwareVersion = ReadString(Data, 156, 64);
                    PayloadCrc = ReadU32(Data, 220);
                    HeaderSize = (HeaderLen >= 224) ? HeaderLen : 224;
                    PayloadOffset = HeaderSize;
                    break;

                default:
                    if (Data.Length < 100)
                        return;
                    PayloadLength = ReadU32(Data, 20);
                    FirmwareVersion = ReadString(Data, 24, 64);
                    PayloadCrc = ReadU32(Data, 88);
                    var HwCount = ReadU32(Data, 92);
                    var SwCount = ReadU32(Data, 96);
                    var ListsSize = ((ulong)HwCount + SwCount) * FixedStringSize;
                    var HeaderSize64 = 100UL + ListsSize + 4;
                    if ((HeaderSize64 > (ulong)Data.Length) || (HeaderSize64 > int.MaxValue))
                        return;
                    HeaderSize = (uint)HeaderSize64;
                    PayloadOffset = HeaderSize;

                    var Hw = new List<string>((int)Math.Min(HwCount, 1024U));
                    var HwOff = 100;
                    for (uint I = 0; I < HwCount; I++, HwOff += FixedStringSize)
                        Hw.Add(ReadString(Data, HwOff, FixedStringSize));
                    HardwareTypes = Hw.ToArray();

                    var Sw = new List<string>((int)Math.Min(SwCount, 1024U));
                    var SwOff = HwOff;
                    for (uint I = 0; I < SwCount; I++, SwOff += FixedStringSize)
                        Sw.Add(ReadString(Data, SwOff, FixedStringSize));
                    SoftwareTypes = Sw.ToArray();

                    HeaderCrc = ReadU32(Data, (int)HeaderSize - 4);
                    HeaderCrcValid = NyaFs.ImageFormat.Helper.HashHelper.Crc32(
                        Slice(Data, 0, (int)HeaderSize - 4)) == HeaderCrc;
                    break;
            }

            if ((ulong)PayloadOffset + PayloadLength > (ulong)Data.Length)
                return;

            Payload = Slice(Data, (int)PayloadOffset, (int)PayloadLength);
            PayloadCrcValid = NyaFs.ImageFormat.Helper.HashHelper.Crc32(Payload) == PayloadCrc;
            Correct = true;
        }

        private static uint ReadU32(byte[] Data, int Offset) =>
            (uint)(Data[Offset] | (Data[Offset + 1] << 8) |
                (Data[Offset + 2] << 16) | (Data[Offset + 3] << 24));

        private static string ReadString(byte[] Data, int Offset, int Length)
        {
            var End = Offset;
            var Limit = Math.Min(Offset + Length, Data.Length);
            while ((End < Limit) && (Data[End] != 0))
                End++;
            return Encoding.UTF8.GetString(Data, Offset, End - Offset);
        }

        private static string[] SplitList(string Value)
        {
            if (String.IsNullOrEmpty(Value))
                return Array.Empty<string>();

            var Res = new List<string>();
            foreach (var Item in Value.Split(','))
            {
                var V = Item.Trim();
                if (V.Length > 0)
                    Res.Add(V);
            }
            return Res.ToArray();
        }

        private static byte[] Slice(byte[] Data, int Offset, int Length)
        {
            var Res = new byte[Length];
            Buffer.BlockCopy(Data, Offset, Res, 0, Length);
            return Res;
        }
    }
}
