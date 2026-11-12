using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace NyaFsFit
{
    public sealed class FitSignatureCheck
    {
        public string Path { get; set; }
        public bool Valid { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Signs and verifies existing FIT configuration signatures using the same
    /// region selection rules as U-Boot mkimage (hashed-nodes / hashed-strings).
    /// </summary>
    public static class FitSigner
    {
        private const uint FdtMagic = 0xd00dfeedu;
        private const int HeaderSize = 40;

        private const int FdtBeginNode = 1;
        private const int FdtEndNode = 2;
        private const int FdtProp = 3;
        private const int FdtNop = 4;
        private const int FdtEnd = 9;

        private static readonly HashSet<string> ExcludedProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            "data", "data-size", "data-position", "data-offset"
        };

        public static byte[] Sign(byte[] Data, string KeyPath, out int SignedCount, out string Error)
        {
            SignedCount = 0;
            Error = null;

            var Parsed = Parse(Data, out Error);
            if (Parsed == null)
                return null;

            var Signatures = Parsed.Nodes.Where(N => N.Name.StartsWith("signature", StringComparison.Ordinal)).ToArray();
            if (Signatures.Length == 0)
            {
                Error = "No FIT signature nodes found";
                return null;
            }

            using var Rsa = LoadKey(KeyPath);
            var Result = (byte[])Data.Clone();

            foreach (var Signature in Signatures)
            {
                if (!TryGetAlgo(Data, Signature, out var Hash, out var AlgoError))
                {
                    Error = $"{Signature.Path}: {AlgoError}";
                    return null;
                }

                if (!Signature.Properties.TryGetValue("value", out var ValueProperty))
                {
                    Error = $"{Signature.Path}: value property is missing";
                    return null;
                }

                var Blob = BuildSignedBlob(Data, Parsed, Signature, out var BlobError);
                if (Blob == null)
                {
                    Error = $"{Signature.Path}: {BlobError}";
                    return null;
                }

                var Value = Rsa.SignData(Blob, Hash, RSASignaturePadding.Pkcs1);
                if (Value.Length != ValueProperty.Length)
                {
                    Error = $"{Signature.Path}: signature size {Value.Length} does not match value size {ValueProperty.Length}";
                    return null;
                }

                Buffer.BlockCopy(Value, 0, Result, ValueProperty.ValueOffset, Value.Length);
                SignedCount++;
            }

            return Result;
        }

        public static FitSignatureCheck[] Verify(byte[] Data, string KeyPath, out string Error)
        {
            Error = null;
            var Parsed = Parse(Data, out Error);
            if (Parsed == null)
                return null;

            var Signatures = Parsed.Nodes.Where(N => N.Name.StartsWith("signature", StringComparison.Ordinal)).ToArray();
            if (Signatures.Length == 0)
            {
                Error = "No FIT signature nodes found";
                return null;
            }

            using var Rsa = LoadKey(KeyPath);
            var Result = new List<FitSignatureCheck>();

            foreach (var Signature in Signatures)
            {
                var Check = new FitSignatureCheck { Path = Signature.Path };
                try
                {
                    if (!TryGetAlgo(Data, Signature, out var Hash, out var AlgoError))
                    {
                        Check.Error = AlgoError;
                    }
                    else if (!Signature.Properties.TryGetValue("value", out var ValueProperty))
                    {
                        Check.Error = "value property is missing";
                    }
                    else
                    {
                        var Blob = BuildSignedBlob(Data, Parsed, Signature, out var BlobError);
                        if (Blob == null)
                        {
                            Check.Error = BlobError;
                        }
                        else
                        {
                            var Value = new byte[ValueProperty.Length];
                            Buffer.BlockCopy(Data, ValueProperty.ValueOffset, Value, 0, Value.Length);
                            Check.Valid = Rsa.VerifyData(Blob, Value, Hash, RSASignaturePadding.Pkcs1);
                            if (!Check.Valid)
                                Check.Error = "signature verification failed";
                        }
                    }
                }
                catch (Exception E)
                {
                    Check.Error = E.Message;
                }

                Result.Add(Check);
            }

            return Result.ToArray();
        }

        public static RSA LoadKey(string KeyPath)
        {
            var Text = File.ReadAllText(KeyPath);
            if (Text.Contains("BEGIN CERTIFICATE", StringComparison.Ordinal))
            {
                using var Certificate = X509CertificateLoader.LoadCertificateFromFile(KeyPath);
                return Certificate.GetRSAPublicKey() ?? throw new CryptographicException("Certificate has no RSA key");
            }

            var Rsa = RSA.Create();
            Rsa.ImportFromPem(Text);
            return Rsa;
        }

        private static byte[] BuildSignedBlob(byte[] Data, FdtInfo Parsed, NodeInfo Signature, out string Error)
        {
            Error = null;

            if (!Signature.Properties.TryGetValue("hashed-nodes", out var HashedNodes))
            {
                Error = "hashed-nodes property is missing (only mkimage-prepared FITs are supported)";
                return null;
            }
            if (!Signature.Properties.TryGetValue("hashed-strings", out var HashedStrings) || HashedStrings.Length < 8)
            {
                Error = "hashed-strings property is missing or invalid";
                return null;
            }

            var Included = new HashSet<string>(
                ReadStringList(Data, HashedNodes).Where(P => P.Length > 0),
                StringComparer.Ordinal);

            var Regions = BuildRegions(Data, Parsed, Included, out Error);
            if (Regions == null)
                return null;

            var StringsOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(HashedStrings.ValueOffset, 4));
            var StringsLength = (int)BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(HashedStrings.ValueOffset + 4, 4));
            if ((StringsLength < 0) || (Parsed.StringsOffset + StringsOffset < 0) ||
                (Parsed.StringsOffset + StringsOffset + StringsLength > Data.Length))
            {
                Error = "hashed-strings region is out of bounds";
                return null;
            }
            if (StringsLength > 0)
                Regions.Add((Parsed.StringsOffset + StringsOffset, StringsLength));

            var Size = Regions.Sum(R => R.Size);
            var Blob = new byte[Size];
            var Pos = 0;
            foreach (var Region in Regions)
            {
                Buffer.BlockCopy(Data, Region.Offset, Blob, Pos, Region.Size);
                Pos += Region.Size;
            }
            return Blob;
        }

        private static List<(int Offset, int Size)> BuildRegions(byte[] Data, FdtInfo Parsed,
            HashSet<string> Included, out string Error)
        {
            Error = null;
            var Regions = new List<(int Offset, int Size)>();
            var Wants = new List<int>();
            var Paths = new List<string>();
            var Start = -1;
            var Want = 0;
            var Depth = -1;
            var ExpectEnd = false;
            var Offset = Parsed.StructOffset;

            while (true)
            {
                var Current = Offset;
                var Tag = ReadTag(Data, Parsed, Current, out var Next, out var Name, out var PropertyName, out var PropertyValueOffset, out var PropertyLength, out var TagError);
                if (Tag < 0)
                {
                    Error = TagError;
                    return null;
                }

                var StopAt = Next;
                var Include = false;

                if (ExpectEnd && Tag != FdtEnd)
                {
                    Error = "FIT structure has data after the root node";
                    return null;
                }

                switch (Tag)
                {
                    case FdtProp:
                        Include = Want >= 2;
                        StopAt = Current;
                        if (ExcludedProperties.Contains(PropertyName))
                            Include = false;
                        break;

                    case FdtNop:
                        Include = Want >= 2;
                        StopAt = Current;
                        break;

                    case FdtBeginNode:
                        Depth++;
                        Wants.Add(Want);
                        Paths.Add(Name);
                        var Path = BuildPath(Paths);
                        if (Want == 1)
                            StopAt = Current;
                        if (Included.Contains(Path))
                            Want = 2;
                        else if (Want != 0)
                            Want--;
                        else
                            StopAt = Current;
                        Include = Want != 0;
                        break;

                    case FdtEndNode:
                        if (Depth < 0)
                        {
                            Error = "Invalid FIT node nesting";
                            return null;
                        }
                        Include = Want != 0;
                        Want = Wants[Wants.Count - 1];
                        Wants.RemoveAt(Wants.Count - 1);
                        Paths.RemoveAt(Paths.Count - 1);
                        Depth--;
                        if (Depth == -1)
                            ExpectEnd = true;
                        break;

                    case FdtEnd:
                        Include = true;
                        break;
                }

                if (Include && (Start < 0))
                {
                    if ((Regions.Count > 0) &&
                        (Current == Regions[Regions.Count - 1].Offset + Regions[Regions.Count - 1].Size))
                    {
                        Start = Regions[Regions.Count - 1].Offset;
                        Regions.RemoveAt(Regions.Count - 1);
                    }
                    else
                    {
                        Start = Current;
                    }
                }

                if (!Include && (Start >= 0))
                {
                    Regions.Add((Start, StopAt - Start));
                    Start = -1;
                }

                if (Tag == FdtEnd)
                {
                    Offset = Next;
                    break;
                }

                Offset = Next;
            }

            if (Start >= 0)
                Regions.Add((Start, Offset - Start));

            return Regions;
        }

        private static int ReadTag(byte[] Data, FdtInfo Parsed, int Offset,
            out int Next, out string Name, out string PropertyName, out int PropertyValueOffset,
            out int PropertyLength, out string Error)
        {
            Next = 0;
            Name = null;
            PropertyName = null;
            PropertyValueOffset = 0;
            PropertyLength = 0;
            Error = null;

            if ((Offset < Parsed.StructOffset) || (Offset + 4 > Parsed.StructOffset + Parsed.StructSize) ||
                (Offset + 4 > Data.Length))
            {
                Error = "FIT structure tag is out of bounds";
                return -1;
            }

            var Tag = (int)BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(Offset, 4));
            switch (Tag)
            {
                case FdtBeginNode:
                    {
                        var End = FindNul(Data, Offset + 4, Parsed.StructOffset + Parsed.StructSize);
                        if (End < 0)
                        {
                            Error = "FIT node name is not terminated";
                            return -1;
                        }
                        Name = Encoding.ASCII.GetString(Data, Offset + 4, End - Offset - 4);
                        Next = Align4(End + 1);
                        break;
                    }

                case FdtEndNode:
                case FdtNop:
                case FdtEnd:
                    Next = Offset + 4;
                    break;

                case FdtProp:
                    {
                        if (Offset + 12 > Parsed.StructOffset + Parsed.StructSize)
                        {
                            Error = "FIT property header is truncated";
                            return -1;
                        }
                        PropertyLength = (int)BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(Offset + 4, 4));
                        var NameOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(Offset + 8, 4));
                        PropertyValueOffset = Offset + 12;
                        if ((PropertyLength < 0) || (PropertyValueOffset + PropertyLength > Parsed.StructOffset + Parsed.StructSize))
                        {
                            Error = "FIT property value is truncated";
                            return -1;
                        }
                        var NameEnd = FindNul(Data, Parsed.StringsOffset + NameOffset, Parsed.StringsOffset + Parsed.StringsSize);
                        if (NameEnd < 0)
                        {
                            Error = "FIT property name is not terminated";
                            return -1;
                        }
                        PropertyName = Encoding.ASCII.GetString(Data, Parsed.StringsOffset + NameOffset, NameEnd - Parsed.StringsOffset - NameOffset);
                        Next = Align4(PropertyValueOffset + PropertyLength);
                        break;
                    }

                default:
                    Error = $"Unknown FIT structure tag {Tag}";
                    return -1;
            }

            return Tag;
        }

        private static FdtInfo Parse(byte[] Data, out string Error)
        {
            Error = null;
            if ((Data == null) || (Data.Length < HeaderSize))
            {
                Error = "FIT is too small";
                return null;
            }
            if (BinaryPrimitives.ReadUInt32BigEndian(Data) != FdtMagic)
            {
                Error = "Invalid FDT magic";
                return null;
            }

            var Parsed = new FdtInfo
            {
                StructOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(8, 4)),
                StringsOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(12, 4)),
                StringsSize = (int)BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(32, 4)),
                StructSize = (int)BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(36, 4))
            };
            if ((Parsed.StructOffset < 0) || (Parsed.StringsOffset < 0) ||
                (Parsed.StructOffset + Parsed.StructSize > Data.Length) ||
                (Parsed.StringsOffset + Parsed.StringsSize > Data.Length))
            {
                Error = "FIT offsets are out of bounds";
                return null;
            }

            var Root = new NodeInfo { Name = "", Path = "/" };
            Parsed.Nodes.Add(Root);
            var Stack = new List<NodeInfo> { Root };
            var Paths = new List<string>();
            var Offset = Parsed.StructOffset;

            while (true)
            {
                var Tag = ReadTag(Data, Parsed, Offset, out var Next, out var Name, out var PropertyName,
                    out var PropertyValueOffset, out var PropertyLength, out var TagError);
                if (Tag < 0)
                {
                    Error = TagError;
                    return null;
                }

                if (Tag == FdtBeginNode)
                {
                    Paths.Add(Name);
                    var Node = new NodeInfo { Name = Name, Path = BuildPath(Paths) };
                    Stack[Stack.Count - 1].Children.Add(Node);
                    Stack.Add(Node);
                    Parsed.Nodes.Add(Node);
                }
                else if (Tag == FdtEndNode)
                {
                    if (Stack.Count <= 1)
                    {
                        Error = "Invalid FIT root node";
                        return null;
                    }
                    Stack.RemoveAt(Stack.Count - 1);
                    Paths.RemoveAt(Paths.Count - 1);
                }
                else if (Tag == FdtProp)
                {
                    Stack[Stack.Count - 1].Properties[PropertyName] = new PropertyInfo
                    {
                        Name = PropertyName,
                        ValueOffset = PropertyValueOffset,
                        Length = PropertyLength
                    };
                }
                else if (Tag == FdtEnd)
                {
                    break;
                }

                Offset = Next;
            }

            return Parsed;
        }

        private static bool TryGetAlgo(byte[] Data, NodeInfo Signature, out HashAlgorithmName Hash, out string Error)
        {
            Hash = default;
            Error = null;
            if (!Signature.Properties.ContainsKey("algo"))
            {
                Error = "algo property is missing";
                return false;
            }

            var Algo = ReadString(Data, Signature.Properties["algo"]);
            var Parts = Algo.Split(',');
            if (Parts.Length < 2)
            {
                Error = $"Unsupported signature algorithm '{Algo}'";
                return false;
            }

            switch (Parts[0].ToLowerInvariant())
            {
                case "sha1": Hash = HashAlgorithmName.SHA1; break;
                case "sha256": Hash = HashAlgorithmName.SHA256; break;
                case "sha512": Hash = HashAlgorithmName.SHA512; break;
                default:
                    Error = $"Unsupported hash algorithm '{Parts[0]}'";
                    return false;
            }
            return true;
        }

        private static string ReadString(byte[] Data, PropertyInfo Property)
        {
            if ((Property == null) || (Property.Length <= 1))
                return "";
            var Length = Property.Length;
            while ((Length > 0) && (Data[Property.ValueOffset + Length - 1] == 0))
                Length--;
            return Encoding.UTF8.GetString(Data, Property.ValueOffset, Length);
        }

        private static string[] ReadStringList(byte[] Data, PropertyInfo Property)
        {
            if ((Property == null) || (Property.Length <= 0))
                return Array.Empty<string>();

            var Text = Encoding.UTF8.GetString(Data, Property.ValueOffset, Property.Length);
            return Text.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        }

        private static string BuildPath(List<string> Parts) =>
            "/" + string.Join("/", Parts.Where(P => P.Length > 0));

        private static int Align4(int Value) => (Value + 3) & ~3;

        private static int FindNul(byte[] Data, int Start, int End)
        {
            if ((Start < 0) || (End > Data.Length) || (Start >= End))
                return -1;
            for (var I = Start; I < End; I++)
            {
                if (Data[I] == 0)
                    return I;
            }
            return -1;
        }

        private sealed class FdtInfo
        {
            public int StructOffset;
            public int StructSize;
            public int StringsOffset;
            public int StringsSize;
            public List<NodeInfo> Nodes { get; } = new List<NodeInfo>();
        }

        private sealed class NodeInfo
        {
            public string Name;
            public string Path;
            public Dictionary<string, PropertyInfo> Properties { get; } = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
            public List<NodeInfo> Children { get; } = new List<NodeInfo>();
        }

        private sealed class PropertyInfo
        {
            public string Name;
            public int ValueOffset;
            public int Length;
        }
    }
}
