using NyaIO.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsRockchip.Types
{
    internal sealed class RknanoImage
    {
        public sealed class Blob
        {
            public string Name;
            public byte[] Data;
        }

        public readonly byte[] Raw;
        public bool Correct;
        public string Vendor = "";
        public string Model = "";
        public readonly List<Blob> Blobs = new List<Blob>();

        public RknanoImage(byte[] Data)
        {
            Raw = Data;
            if ((Data == null) || (Data.Length < 512))
                return;

            if (Encoding.ASCII.GetString(Data, 504, 8) != "RKnanoFW")
                return;

            var Size = (int)ReadU32(Data, 500);
            if ((Size < 512) || (Size > Data.Length))
                return;

            Vendor = ReadString(Data, 16, 32);
            Model = ReadString(Data, 48, 32);
            if (Vendor.Length == 0)
                Vendor = "Rockchip";

            AddBlobs(Data, 80, 84, 4, "stage", true);
            AddBlobs(Data, 116, 120, 10, "font", false);
            AddBlobs(Data, 200, 204, 5, "gbk", false);
            AddBlobs(Data, 244, 248, 10, "strtbl", false);
            AddBlobs(Data, 328, 332, 10, "imageres", false);
            AddBlobs(Data, 412, 416, 10, "unk", false);
            Correct = true;
        }

        private void AddBlobs(byte[] Data, int CountOffset, int BlobOffset, int Max,
            string Prefix, bool Decode)
        {
            var Count = (int)ReadU32(Data, CountOffset);
            if (Count < 0)
                Count = 0;
            if (Count > Max)
                Count = Max;

            for (var I = 0; I < Count; I++)
            {
                var Offset = (int)ReadU32(Data, BlobOffset + I * 8);
                var Size = (int)ReadU32(Data, BlobOffset + I * 8 + 4);
                if ((Offset < 0) || (Size <= 0) || ((long)Offset + Size > Data.Length))
                    continue;

                var Content = new byte[Size];
                Buffer.BlockCopy(Data, Offset, Content, 0, Size);

                // RKnano stages 0..2 are RC4-like scrambled; stage 3 is raw.
                if (Decode && (I != 3))
                    Content = RknanoCodec.Transform(Content);

                Blobs.Add(new Blob
                {
                    Name = Prefix + I + ".bin",
                    Data = Content
                });
            }
        }

        private static string ReadString(byte[] Data, int Offset, int Size)
        {
            var End = Offset;
            while ((End < Offset + Size) && (Data[End] != 0))
                End++;
            return Encoding.ASCII.GetString(Data, Offset, End - Offset);
        }

        private static uint ReadU32(byte[] Data, int Offset) =>
            (uint)(Data[Offset] | (Data[Offset + 1] << 8) | (Data[Offset + 2] << 16) | (Data[Offset + 3] << 24));
    }

    internal static class RknanoCodec
    {
        private static readonly byte[] Key =
        {
            0x7C, 0x4E, 0x03, 0x04, 0x55, 0x05, 0x09, 0x07,
            0x2D, 0x2C, 0x7B, 0x38, 0x17, 0x0D, 0x17, 0x11
        };

        /// <summary>
        /// The RK Nano stage transform is an XOR keystream; applying it twice
        /// restores the original bytes.
        /// </summary>
        public static byte[] Transform(byte[] Input)
        {
            var Key1 = new byte[256];
            var Key2 = new byte[256];
            for (var I = 0; I < 256; I++)
            {
                Key1[I] = (byte)I;
                Key2[I] = Key[I & 0x0F];
            }

            var I3 = 0;
            for (var I = 0; I < 256; I++)
            {
                var X = Key1[I];
                I3 = (I3 + Key1[I] + Key2[I]) & 0xFF;
                Key1[I] = Key1[I3];
                Key1[I3] = X;
            }

            var Result = new byte[Input.Length];
            var Index = 0;
            for (var I = 0; I < Input.Length; I++)
            {
                var X = Key1[(I + 1) & 0xFF];
                Index = (X + Index) & 0xFF;
                Key1[(I + 1) & 0xFF] = Key1[Index];
                Key1[Index] = X;
                var Value = Key1[(Key1[(I + 1) & 0xFF] + X) & 0xFF];
                Result[I] = (byte)(Value ^ Input[I]);
            }
            return Result;
        }
    }
}
