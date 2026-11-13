using NyaFs;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.ImageFormat.Elements.Kernel;
using NyaFs.ImageFormat.Plugins;
using NyaFsRockchip.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace NyaFsRockchip
{
    /// <summary>
    /// Writes Rockchip RKFW images. The MiniLoader and RKAF metadata are preserved
    /// from the source image; dirty partition payloads are rebuilt through the
    /// normal build plan and all metadata sizes/offsets are refreshed.
    /// </summary>
    public class RkfwWriter
    {
        public byte[] Build(BaseImageBlob Blob, out string Error)
        {
            Error = null;
            try
            {
                var Source = (Blob.BuildPlan != null) ? Blob.BuildPlan.Root.Raw : null;
                var SourceImage = (Source != null) ? new RkfwImage(Source) : null;
                if ((SourceImage == null) || !SourceImage.Correct)
                {
                    Error = "RKFW writer needs an existing source image";
                    return null;
                }

                var SourceFw = SourceImage.FirmwareData;
                if ((SourceFw == null) || !SourceFw.Correct)
                {
                    Error = "RKFW source has no valid RKAF firmware table";
                    return null;
                }

                var Parts = CollectParts(Blob, SourceFw);
                if (Parts.Count == 0)
                {
                    Error = "No RKFW partitions to write";
                    return null;
                }

                var DataOffset = 0x800;
                var ContentLength = DataOffset;
                foreach (var Part in Parts)
                    ContentLength += Align(Part.Data.Length, 0x800);

                var Data = new byte[ContentLength];
                Buffer.BlockCopy(SourceFw.HeaderBytes, 0, Data, 0, Math.Min(0x800, SourceFw.HeaderBytes.Length));

                SetU32(Data, 0x04, (uint)ContentLength);
                SetU32(Data, 0x88, (uint)Parts.Count);

                var Offset = DataOffset;
                for (var I = 0; I < Parts.Count; I++)
                {
                    var Part = Parts[I];
                    var Header = 0x8C + I * RkfwUpdateFileHeader.Size;
                    var SourceHeader = Part.SourceHeader;
                    if (SourceHeader != null)
                        Buffer.BlockCopy(SourceHeader.RawBytes, 0, Data, Header, RkfwUpdateFileHeader.Size);

                    SetString(Data, Header + 0x00, 32, Part.Name);
                    if ((SourceHeader == null) || String.IsNullOrEmpty(SourceHeader.FileName))
                        SetString(Data, Header + 0x20, 60, "Image/" + Part.Name + ".img");

                    SetU32(Data, Header + 0x68, (uint)((Part.Data.Length + 0x7FF) / 0x800));
                    SetU32(Data, Header + 0x6C, (uint)Part.Data.Length);

                    Buffer.BlockCopy(Part.Data, 0, Data, Offset, Part.Data.Length);
                    Offset += Align(Part.Data.Length, 0x800);
                }

                var SourceLoader = SourceImage.FirmwareLoader ?? Array.Empty<byte>();
                var Loader = SourceLoader;
                var LoaderNode = FindChild(Blob.BuildPlan.Root, "MiniLoaderAll.bin");
                if ((LoaderNode != null) && LoaderNode.Dirty && (LoaderNode.Raw != null))
                    Loader = LoaderNode.Raw;

                var HeaderSize = (int)ReadU32(Source, 0x04);
                if ((HeaderSize <= 0) || (HeaderSize > Source.Length) || (HeaderSize < 0x39))
                    HeaderSize = 0x66;

                var Result = new byte[HeaderSize + Loader.Length + Data.Length];
                Buffer.BlockCopy(Source, 0, Result, 0, Math.Min(HeaderSize, Source.Length));
                SetU32(Result, 0x19, (uint)HeaderSize);
                SetU32(Result, 0x1D, (uint)Loader.Length);
                SetU32(Result, 0x21, (uint)(HeaderSize + Loader.Length));
                SetU32(Result, 0x25, (uint)(Data.Length + 4));
                Buffer.BlockCopy(Loader, 0, Result, HeaderSize, Loader.Length);
                Buffer.BlockCopy(Data, 0, Result, HeaderSize + Loader.Length, Data.Length);

                var BodyLength = HeaderSize + Loader.Length + Data.Length;
                var Prefix = Encoding.ASCII.GetBytes("ru$u");
                var Tail = Encoding.ASCII.GetBytes("ru$u" + Md5Hex(Result, BodyLength, Prefix));
                var WithTail = new byte[BodyLength + Tail.Length];
                Buffer.BlockCopy(Result, 0, WithTail, 0, BodyLength);
                Buffer.BlockCopy(Tail, 0, WithTail, BodyLength, Tail.Length);
                return WithTail;
            }
            catch (Exception E)
            {
                Error = E.Message;
                Log.Error(0, $"Cannot write RKFW image: {E.Message}");
                return null;
            }
        }

        private static List<Part> CollectParts(BaseImageBlob Blob, RkfwData SourceFw)
        {
            var Result = new List<Part>();
            var Root = Blob.BuildPlan?.Root;
            if (Root == null)
                return Result;

            var Used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var I = 0; I < SourceFw.FileCount; I++)
            {
                var Header = SourceFw.GetHeader(I);
                if (Header == null)
                    continue;

                var Name = (Header.Name ?? "").Trim();
                if (Name.Length == 0)
                    Name = (Header.FileName ?? $"partition{I}").Trim();
                if (Name.Length == 0)
                    Name = $"partition{I}";

                var Child = FindChild(Root, Name);
                var Data = (Child != null) ? GetChildData(Child) : SourceFw.GetPartition(Header.Name);
                Result.Add(new Part(Name, Header, Data ?? Array.Empty<byte>()));
                Used.Add(Name);
            }

            foreach (var Child in Root.Children)
            {
                if (String.Equals(Child.Name, "MiniLoaderAll.bin", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (Used.Contains(Child.Name))
                    continue;

                var Data = GetChildData(Child);
                if ((Data == null) || (Data.Length == 0))
                    continue;
                Result.Add(new Part(Child.Name, null, Data));
            }

            return Result;
        }

        private static ImageBuildNode FindChild(ImageBuildNode Node, string Name)
        {
            if (Node == null)
                return null;
            foreach (var Child in Node.Children)
            {
                if (String.Equals(Child.Name, Name, StringComparison.OrdinalIgnoreCase))
                    return Child;
                var Res = FindChild(Child, Name);
                if (Res != null)
                    return Res;
            }
            return null;
        }

        private static byte[] GetChildData(ImageBuildNode Child)
        {
            if (!Child.Dirty && (Child.Raw != null))
                return Child.Raw;

            var Writer = new NyaFs.ImageFormat.Composite.ImageBuildPlanWriter(new ImageBuildPlan(Child));
            var Result = Writer.Build(out _);
            return (Result != null) ? Result : Child.Raw;
        }

        private static string Md5Hex(byte[] Data, int Length, byte[] Extra)
        {
            using (var Hash = MD5.Create())
            {
                Hash.TransformBlock(Data, 0, Length, null, 0);
                Hash.TransformFinalBlock(Extra, 0, Extra.Length);
                return Convert.ToHexString(Hash.Hash).ToLowerInvariant();
            }
        }

        private static int Align(int Value, int Alignment) =>
            ((Value + Alignment - 1) / Alignment) * Alignment;

        private static uint ReadU32(byte[] Data, int Offset) =>
            (uint)(Data[Offset] | (Data[Offset + 1] << 8) | (Data[Offset + 2] << 16) | (Data[Offset + 3] << 24));

        private static void SetU32(byte[] Data, int Offset, uint Value)
        {
            Data[Offset] = (byte)Value;
            Data[Offset + 1] = (byte)(Value >> 8);
            Data[Offset + 2] = (byte)(Value >> 16);
            Data[Offset + 3] = (byte)(Value >> 24);
        }

        private static void SetString(byte[] Data, int Offset, int Size, string Value)
        {
            var Bytes = Encoding.ASCII.GetBytes(Value ?? "");
            Buffer.BlockCopy(Bytes, 0, Data, Offset, Math.Min(Size, Bytes.Length));
        }

        private sealed class Part
        {
            public readonly string Name;
            public readonly RkfwUpdateFileHeader SourceHeader;
            public readonly byte[] Data;

            public Part(string Name, RkfwUpdateFileHeader SourceHeader, byte[] Data)
            {
                this.Name = Name;
                this.SourceHeader = SourceHeader;
                this.Data = Data;
            }
        }
    }
}