using NyaFs.ImageFormat;
using NyaFsWdNas.Types;
using System;
using System.Collections.Generic;

namespace NyaFsWdNas
{
    /// <summary>
    /// Rebuilds a WD DL2100 .bin container. Header metadata is preserved; image
    /// and extraction-target data, offsets, lengths and XOR checksums are refreshed.
    /// </summary>
    public sealed class WdNasWriter
    {
        public byte[] Build(BaseImageBlob Blob, out string Error)
        {
            Error = null;
            var Source = Blob.BuildPlan?.Root?.Raw;
            if (Source == null)
            {
                Error = "WD NAS writer needs an existing source image";
                return null;
            }

            var Image = new WdNasImage(Source);
            if (!Image.Correct)
            {
                Error = "WD NAS source is not a valid firmware image";
                return null;
            }

            var Root = Blob.BuildPlan.Root;
            var Changed = false;
            var Images = new List<byte[]>();
            foreach (var Item in Image.Images)
                Images.Add(Item.Data);

            for (var I = 0; I < 4; I++)
            {
                var Child = Find(Root, Image.Images[I].Name);
                if ((Child?.Raw != null) && !Same(Child.Raw, Images[I]))
                {
                    Images[I] = Child.Raw;
                    Changed = true;
                }
            }

            var SquashFs = Find(Root, "image.squashfs");
            if ((SquashFs?.Raw != null) && !Same(SquashFs.Raw, Image.CfsSquashFs))
            {
                Images[2] = BuildCfs(Image.Image("image.cfs"), SquashFs.Raw);
                Changed = true;
            }

            var TargetHeaders = new List<byte[]>();
            var TargetData = new List<byte[]>();
            for (var I = 0; I < Image.Targets.Length; I++)
            {
                var Target = Image.Targets[I];
                var Child = Find(Root, TargetName(Target, I));
                var Payload = Target.Data;
                if ((Child?.Raw != null) && !Same(Child.Raw, Payload))
                {
                    Payload = Child.Raw;
                    Changed = true;
                }
                TargetHeaders.Add(Slice(Target.Header, 0, WdNasImage.CommonHeaderSize));
                TargetData.Add(Payload);
            }

            if (!Changed && !Root.Dirty)
                return Source;

            var Header = Slice(Source, 0, WdNasImage.MainHeaderSize);
            var Body = new List<byte>();
            var Offset = WdNasImage.MainHeaderSize;

            for (var I = 0; I < 4; I++)
            {
                WdNasImage.WriteU32(Header, I * 8, (uint)Offset);
                WdNasImage.WriteU32(Header, I * 8 + 4, (uint)Images[I].Length);
                WdNasImage.WriteU32(Header, 32 + I * 4,
                    WdNasImage.XorChecksum(Images[I], 0, Images[I].Length));
                Body.AddRange(Images[I]);
                Offset += Images[I].Length;
            }

            var TargetOffsets = new List<int>();
            for (var I = 0; I < TargetHeaders.Count; I++)
            {
                TargetOffsets.Add(Offset);
                Offset += WdNasImage.CommonHeaderSize + TargetData[I].Length;
            }

            for (var I = 0; I < TargetHeaders.Count; I++)
            {
                var HeaderBytes = TargetHeaders[I];
                WdNasImage.WriteU32(HeaderBytes, 80, (uint)(TargetOffsets[I] + WdNasImage.CommonHeaderSize));
                WdNasImage.WriteU32(HeaderBytes, 84, (uint)TargetData[I].Length);
                WdNasImage.WriteU32(HeaderBytes, 88,
                    WdNasImage.XorChecksum(TargetData[I], 0, TargetData[I].Length));
                WdNasImage.WriteU32(HeaderBytes, 92,
                    (I + 1 < TargetOffsets.Count) ? (uint)TargetOffsets[I + 1] : 0);
                Body.AddRange(HeaderBytes);
                Body.AddRange(TargetData[I]);
            }

            WdNasImage.WriteU32(Header, 120, 0);
            WdNasImage.WriteU32(Header, 124,
                (TargetOffsets.Count > 0) ? (uint)TargetOffsets[0] : 0);
            WdNasImage.WriteU32(Header, 120,
                WdNasImage.XorChecksum(Header, 0, WdNasImage.MainHeaderSize));

            var Result = new byte[Header.Length + Body.Count];
            Buffer.BlockCopy(Header, 0, Result, 0, Header.Length);
            Buffer.BlockCopy(Body.ToArray(), 0, Result, Header.Length, Body.Count);
            return Result;
        }

        private static string TargetName(WdNasExtractionTarget Target, int Index) =>
            String.IsNullOrEmpty(Target.FileName) ? $"ext_{Index:0000}" : Target.FileName;

        private static byte[] BuildCfs(byte[] Source, byte[] SquashFs)
        {
            var Length = SquashFs.Length;
            var Result = new byte[WdNasImage.CfsHeaderSize + Length];
            if (Source != null)
                Buffer.BlockCopy(Source, 0, Result, 0, Math.Min(Source.Length, WdNasImage.CfsHeaderSize));
            WdNasImage.WriteU32(Result, 0, (uint)Length);
            WdNasImage.WriteU32(Result, 4, WdNasImage.XorChecksum(SquashFs, 0, Length));
            Buffer.BlockCopy(SquashFs, 0, Result, WdNasImage.CfsHeaderSize, Length);
            return Result;
        }

        private static ImageBuildNode Find(ImageBuildNode Node, string Name)
        {
            if (Node == null)
                return null;
            if (String.Equals(Node.Name, Name, StringComparison.OrdinalIgnoreCase))
                return Node;
            foreach (var Child in Node.Children)
            {
                var Result = Find(Child, Name);
                if (Result != null)
                    return Result;
            }
            return null;
        }

        private static bool Same(byte[] A, byte[] B) =>
            (A != null) && (B != null) && A.AsSpan().SequenceEqual(B);

        private static byte[] Slice(byte[] Data, int Offset, int Length)
        {
            var Result = new byte[Length];
            Buffer.BlockCopy(Data, Offset, Result, 0, Length);
            return Result;
        }
    }
}
