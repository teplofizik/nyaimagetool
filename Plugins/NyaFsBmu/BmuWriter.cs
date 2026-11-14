using NyaFs;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Composite;
using NyaFs.ImageFormat.Plugins;
using NyaFs.Keys;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NyaFsBmu
{
    /// <summary>
    /// Rebuilds a single Bitmain BMU from the loaded plan and signs it with a
    /// KeyStore profile. The embedded miner.pem may be signed by a separate root key.
    /// </summary>
    public class BmuWriter
    {
        private const int HeaderSize = 0x800;
        private const int PemOffset = 0x18;
        private const int PemSignatureOffset = 0x418;
        private const int FileCountOffset = 0x518;
        private const int DescriptorOffset = 0x51D;

        public static byte[] Build(BaseImageBlob Blob, out string Error)
        {
            Error = null;
            try
            {
                var Source = Blob.BuildPlan?.Root.Raw;
                if (Source == null)
                {
                    Error = "BMU writer needs an existing BMU source";
                    return null;
                }

                if (BmuMergeWriter.IsMerge(Source))
                    return BmuMergeWriter.Build(Blob, Source, out Error);

                if (Source.Length < HeaderSize)
                {
                    Error = "BMU writer needs an existing BMU source";
                    return null;
                }

                var Files = ReadFiles(Source, Blob);
                if (Files.Count == 0)
                {
                    Error = "BMU has no payload files";
                    return null;
                }
                if (Files.Count > 255)
                {
                    Error = "BMU has too many payload files";
                    return null;
                }

                var SignProfile = Blob.GetContainerOption("bmu", "sign-key", null);
                if (String.IsNullOrEmpty(SignProfile))
                {
                    Error = "BMU signing key is not set (set container bmu.sign-key <profile>)";
                    return null;
                }

                var SignRecord = PluginHost.Current.KeyStore.Find(SignProfile);
                if (SignRecord == null)
                {
                    Error = "BMU signing key profile not found: " + SignProfile;
                    return null;
                }

                using var SignKey = FitSignature.LoadPrivateKey(PluginHost.Current.KeyStore.GetPrivateMaterial(SignRecord));
                var PublicPem = ExportPublicPem(SignKey);
                var PemBytes = Encoding.ASCII.GetBytes(PublicPem);
                if (PemBytes.Length > (PemSignatureOffset - PemOffset))
                {
                    Error = "BMU public key is too large";
                    return null;
                }

                var RootProfile = Blob.GetContainerOption("bmu", "root-key", null);
                RSA RootKey = null;
                if (!String.IsNullOrEmpty(RootProfile))
                {
                    var RootRecord = PluginHost.Current.KeyStore.Find(RootProfile);
                    if (RootRecord == null)
                    {
                        Error = "BMU root key profile not found: " + RootProfile;
                        return null;
                    }
                    RootKey = FitSignature.LoadPrivateKey(PluginHost.Current.KeyStore.GetPrivateMaterial(RootRecord));
                }

                var Total = HeaderSize;
                foreach (var File in Files)
                    Total += File.Data.Length;
                Total += Files.Count * 256 + 256;

                var Result = new byte[Total];
                Buffer.BlockCopy(Source, 0, Result, 0, HeaderSize);
                Array.Clear(Result, PemOffset, PemSignatureOffset - PemOffset);
                Buffer.BlockCopy(PemBytes, 0, Result, PemOffset, PemBytes.Length);
                Result[0] = 0x26;
                SetU16BE(Result, 0x16, (ushort)PemBytes.Length);
                Result[FileCountOffset] = (byte)Files.Count;

                var Offset = HeaderSize;
                for (var I = 0; I < Files.Count; I++)
                {
                    var Descriptor = DescriptorOffset + I * 5;
                    Result[Descriptor] = (byte)Files[I].Code;
                    SetU32BE(Result, Descriptor + 1, (uint)Files[I].Data.Length);
                    Buffer.BlockCopy(Files[I].Data, 0, Result, Offset, Files[I].Data.Length);
                    Offset += Files[I].Data.Length;
                }

                if ((RootKey != null) && (RootKey != SignKey))
                {
                    var MinerSignature = SignData(RootKey, PemBytes, SHA256.Create());
                    Buffer.BlockCopy(MinerSignature, 0, Result, PemSignatureOffset, 256);
                    RootKey.Dispose();
                }

                var SignatureOffset = Offset;
                using (var Sha = SHA256.Create())
                {
                    for (var I = 0; I < Files.Count; I++)
                        Buffer.BlockCopy(SignData(SignKey, Files[I].Data, Sha), 0, Result,
                            SignatureOffset + I * 256, 256);

                    var Check = new List<byte>();
                    Check.AddRange(Sha.ComputeHash(Result.AsSpan(0, HeaderSize).ToArray()));
                    for (var I = 0; I < Files.Count; I++)
                        Check.AddRange(Sha.ComputeHash(Files[I].Data));
                    for (var I = 0; I < Files.Count; I++)
                    {
                        var Signature = new byte[256];
                        Buffer.BlockCopy(Result, SignatureOffset + I * 256, Signature, 0, 256);
                        Check.AddRange(Sha.ComputeHash(Signature));
                    }

                    var Overall = SignHash(SignKey, Sha.ComputeHash(Check.ToArray()));
                    Buffer.BlockCopy(Overall, 0, Result, Result.Length - 256, 256);
                }

                return Result;
            }
            catch (Exception E)
            {
                Error = E.Message;
                return null;
            }
        }

        private static List<BmuFile> ReadFiles(byte[] Source, BaseImageBlob Blob)
        {
            var Count = Source[FileCountOffset];
            var Result = new List<BmuFile>();
            var Plan = Blob.BuildPlan?.Root;
            for (var I = 0; I < Count; I++)
            {
                var Descriptor = DescriptorOffset + I * 5;
                if (Descriptor + 5 > Source.Length)
                    break;
                var Code = Source[Descriptor];
                var Size = (int)ReadU32BE(Source, Descriptor + 1);
                var Name = FileName(Code);
                var Child = FindChild(Plan, Name);
                var Data = (Child != null) ? GetChildData(Child) : null;
                if (Data == null)
                {
                    var Offset = HeaderSize;
                    for (var J = 0; J < I; J++)
                        Offset += (int)ReadU32BE(Source, DescriptorOffset + J * 5 + 1);
                    Data = new byte[Size];
                    Buffer.BlockCopy(Source, Offset, Data, 0, Math.Min(Size, Source.Length - Offset));
                }
                Result.Add(new BmuFile(Code, Name, Data));
            }
            return Result;
        }

        private static ImageBuildNode FindChild(ImageBuildNode Root, string Name)
        {
            if (Root == null)
                return null;
            foreach (var Child in Root.Children)
                if (Child.Name == Name)
                    return Child;
            return null;
        }

        private static byte[] GetChildData(ImageBuildNode Child)
        {
            if (!Child.Dirty && (Child.Raw != null))
                return Child.Raw;
            var Writer = new ImageBuildPlanWriter(new ImageBuildPlan(Child));
            return Writer.Build(out _) ?? Child.Raw;
        }

        private static string FileName(int Code)
        {
            switch (Code)
            {
                case 0: return "BOOT.bin";
                case 1: return "devicetree.dtb";
                case 2: return "uImage";
                case 3: return "minerfs.image.gz";
                case 4: return "update.image.gz";
                case 5: return "crl.tar.gz";
                case 6: return "miner.btm.tar.gz";
                case 7: return "reserve";
                case 9: return "datafile";
                default: return $"file_{Code}";
            }
        }

        private static string ExportPublicPem(RSA Rsa)
        {
            var Spki = Rsa.ExportSubjectPublicKeyInfo();
            return "-----BEGIN PUBLIC KEY-----\n" +
                Convert.ToBase64String(Spki, Base64FormattingOptions.InsertLineBreaks) +
                "\n-----END PUBLIC KEY-----\n";
        }

        private static byte[] SignData(RSA Rsa, byte[] Data, HashAlgorithm Sha) =>
            SignHash(Rsa, Sha.ComputeHash(Data));

        private static byte[] SignHash(RSA Rsa, byte[] Hash) =>
            Rsa.SignHash(Hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        private static uint ReadU32BE(byte[] Data, int Offset) =>
            (uint)((Data[Offset] << 24) | (Data[Offset + 1] << 16) | (Data[Offset + 2] << 8) | Data[Offset + 3]);

        private static void SetU16BE(byte[] Data, int Offset, ushort Value)
        {
            Data[Offset] = (byte)(Value >> 8);
            Data[Offset + 1] = (byte)Value;
        }

        private static void SetU32BE(byte[] Data, int Offset, uint Value)
        {
            Data[Offset] = (byte)(Value >> 24);
            Data[Offset + 1] = (byte)(Value >> 16);
            Data[Offset + 2] = (byte)(Value >> 8);
            Data[Offset + 3] = (byte)Value;
        }

        private sealed class BmuFile
        {
            public readonly int Code;
            public readonly string Name;
            public readonly byte[] Data;
            public BmuFile(int Code, string Name, byte[] Data) { this.Code = Code; this.Name = Name; this.Data = Data; }
        }
    }
}