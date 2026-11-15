using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Helper;
using NyaFsAvalon.Types;
using System;
using System.Linq;

namespace NyaFsAvalon
{
    /// <summary>
    /// Rebuilds an Avalon AUP package. The header metadata is preserved; only the
    /// payload, its length/CRC and (for v2) the header CRC are refreshed.
    /// </summary>
    public class AupWriter
    {
        public byte[] Build(BaseImageBlob Blob, out string Error)
        {
            Error = null;

            var Source = Blob.BuildPlan?.Root?.Raw;
            if (Source == null)
            {
                Error = "AUP writer needs an existing source image";
                return null;
            }

            var Image = new AupImage(Source);
            if (!Image.Correct)
            {
                Error = "AUP source is not a valid package";
                return null;
            }

            var Child = FindPayload(Blob.BuildPlan.Root);
            var Payload = Child?.Raw ?? Image.Payload;
            if (Payload.SequenceEqual(Image.Payload) && !Blob.BuildPlan.Root.Dirty)
                return Source;

            var Header = new byte[Image.HeaderSize];
            Buffer.BlockCopy(Source, 0, Header, 0, Header.Length);

            switch (Image.Version)
            {
                case 0:
                    WriteU32(Header, 20, (uint)Payload.Length);
                    WriteU32(Header, 88, HashHelper.Crc32(Payload));
                    break;
                case 1:
                    WriteU32(Header, 152, (uint)Payload.Length);
                    WriteU32(Header, 220, HashHelper.Crc32(Payload));
                    break;
                case 2:
                    WriteU32(Header, 20, (uint)Payload.Length);
                    WriteU32(Header, 88, HashHelper.Crc32(Payload));
                    WriteU32(Header, Header.Length - 4, HashHelper.Crc32(
                        Header.Take(Header.Length - 4).ToArray()));
                    break;
            }

            var Result = new byte[Header.Length + Payload.Length];
            Buffer.BlockCopy(Header, 0, Result, 0, Header.Length);
            Buffer.BlockCopy(Payload, 0, Result, Header.Length, Payload.Length);
            return Result;
        }

        private static ImageBuildNode FindPayload(ImageBuildNode Node)
        {
            if (Node == null)
                return null;

            foreach (var Child in Node.Children)
            {
                if (String.Equals(Child.Name, "payload.bin", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(Child.Name, "firmware.bin", StringComparison.OrdinalIgnoreCase))
                    return Child;
            }

            foreach (var Child in Node.Children)
            {
                var Res = FindPayload(Child);
                if (Res != null)
                    return Res;
            }
            return null;
        }

        private static void WriteU32(byte[] Data, int Offset, uint Value)
        {
            Data[Offset + 0] = (byte)Value;
            Data[Offset + 1] = (byte)(Value >> 8);
            Data[Offset + 2] = (byte)(Value >> 16);
            Data[Offset + 3] = (byte)(Value >> 24);
        }
    }
}
