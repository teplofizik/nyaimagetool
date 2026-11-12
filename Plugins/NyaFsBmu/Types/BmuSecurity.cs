using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NyaFsBmu.Types
{
    public sealed class BmuFileVerification
    {
        public string Name { get; set; }
        public bool Valid { get; set; }
    }

    public sealed class BmuVerificationResult
    {
        public bool OverallSignatureValid { get; set; }
        public bool? MinerKeySignatureValid { get; set; }
        public string OverallDigest { get; set; }
        public string RootKeyFingerprint { get; set; }
        public List<BmuFileVerification> Files { get; } = new List<BmuFileVerification>();
        public string KeyFingerprint { get; set; }
        public string Error { get; set; }
        public bool Valid => (Error == null) && OverallSignatureValid && (MinerKeySignatureValid != false) && Files.TrueForAll(F => F.Valid);
    }

    /// <summary>
    /// Helpers for the public key embedded as PEM text in a BMU header.
    /// A single BMU signs every payload file and the final hash list with
    /// PKCS#1 v1.5 over SHA-256 (same layout as BitmainFirmwareUnpacker).
    /// </summary>
    public static class BmuSecurity
    {
        public static string ExtractPublicKeyPem(byte[] Data)
        {
            if (Data == null)
                return null;

            var Text = Encoding.ASCII.GetString(Data, 0, Math.Min(Data.Length, 0x2000));
            var Begin = Text.IndexOf("-----BEGIN PUBLIC KEY-----", StringComparison.Ordinal);
            if (Begin < 0)
                Begin = Text.IndexOf("-----BEGIN RSA PUBLIC KEY-----", StringComparison.Ordinal);
            if (Begin < 0)
                return null;

            var EndMarker = Text.Substring(Begin).StartsWith("-----BEGIN RSA PUBLIC KEY-----", StringComparison.Ordinal)
                ? "-----END RSA PUBLIC KEY-----"
                : "-----END PUBLIC KEY-----";
            var End = Text.IndexOf(EndMarker, Begin, StringComparison.Ordinal);
            if (End < 0)
                return null;

            return Text.Substring(Begin, End + EndMarker.Length - Begin);
        }

        public static byte[] NormalizePublicKey(byte[] Data)
        {
            var Pem = ExtractPublicKeyPem(Data);
            if (Pem == null)
                return null;

            using var Rsa = RSA.Create();
            Rsa.ImportFromPem(Pem);
            return Rsa.ExportSubjectPublicKeyInfo();
        }

        public static string PublicKeyFingerprint(byte[] Data)
        {
            var Spki = NormalizePublicKey(Data);
            if (Spki == null)
                return null;
            return BitConverter.ToString(SHA256.HashData(Spki)).Replace("-", "").ToLowerInvariant();
        }

        public static BmuVerificationResult VerifySingle(byte[] Data, string RootPublicKeyPem = null)
        {
            var Result = new BmuVerificationResult();
            if ((Data == null) || (Data.Length < 0x800) || (Data[0] != 0x26))
            {
                Result.Error = "Not a single BMU image";
                return Result;
            }

            var FileCount = Data[0x518];
            var Files = new List<Tuple<string, long, int>>();
            long Offset = 0x800;
            for (var I = 0; I < FileCount; I++)
            {
                var Descriptor = 0x51D + I * 5;
                if (Descriptor + 5 > Data.Length)
                {
                    Result.Error = "BMU file descriptor table is truncated";
                    return Result;
                }

                var Size = ReadUInt32BE(Data, Descriptor + 1);
                if ((Size > Int32.MaxValue) || (Offset + Size > Data.Length))
                {
                    Result.Error = "BMU payload is truncated";
                    return Result;
                }

                var Name = FileName(Data[Descriptor]);
                Files.Add(Tuple.Create(Name, Offset, (int)Size));
                Result.Files.Add(new BmuFileVerification { Name = Name });
                Offset += Size;
            }

            var SignatureBase = Offset;
            var OverallSignatureOffset = Data.Length - 256;
            if ((SignatureBase + FileCount * 256 != OverallSignatureOffset) || (OverallSignatureOffset < 0x800))
            {
                Result.Error = "BMU signature table size does not match file count";
                return Result;
            }

            try
            {
                var Pem = ExtractPublicKeyPem(Data);
                if (Pem == null)
                {
                    Result.Error = "BMU public key is missing";
                    return Result;
                }

                using var Rsa = RSA.Create();
                Rsa.ImportFromPem(Pem);
                var Spki = Rsa.ExportSubjectPublicKeyInfo();
                Result.KeyFingerprint = BitConverter.ToString(SHA256.HashData(Spki)).Replace("-", "").ToLowerInvariant();

                if (!string.IsNullOrEmpty(RootPublicKeyPem))
                {
                    using var RootRsa = RSA.Create();
                    RootRsa.ImportFromPem(RootPublicKeyPem);
                    Result.RootKeyFingerprint = BitConverter.ToString(SHA256.HashData(RootRsa.ExportSubjectPublicKeyInfo())).Replace("-", "").ToLowerInvariant();

                    var PemLength = (Data[22] << 8) | Data[23];
                    if ((PemLength <= 0) || (24 + PemLength > 1048))
                    {
                        Result.Error = "BMU miner.pem length is invalid";
                        return Result;
                    }

                    var PemBytes = new byte[PemLength];
                    Buffer.BlockCopy(Data, 24, PemBytes, 0, PemLength);
                    var PemSignature = new byte[256];
                    Buffer.BlockCopy(Data, 1048, PemSignature, 0, 256);
                    Result.MinerKeySignatureValid = RootRsa.VerifyData(PemBytes, PemSignature,
                        HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }

                for (var I = 0; I < Files.Count; I++)
                {
                    var File = Files[I];
                    var Content = new byte[File.Item3];
                    Buffer.BlockCopy(Data, (int)File.Item2, Content, 0, Content.Length);
                    var Signature = new byte[256];
                    Buffer.BlockCopy(Data, (int)(SignatureBase + I * 256), Signature, 0, Signature.Length);
                    Result.Files[I].Valid = Rsa.VerifyData(Content, Signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }

                var Check = new byte[32 + FileCount * 64];
                Buffer.BlockCopy(SHA256.HashData(Data.AsSpan(0, 0x800).ToArray()), 0, Check, 0, 32);
                for (var I = 0; I < Files.Count; I++)
                {
                    var Content = new byte[Files[I].Item3];
                    Buffer.BlockCopy(Data, (int)Files[I].Item2, Content, 0, Content.Length);
                    Buffer.BlockCopy(SHA256.HashData(Content), 0, Check, 32 + I * 32, 32);
                }
                for (var I = 0; I < Files.Count; I++)
                {
                    var Signature = new byte[256];
                    Buffer.BlockCopy(Data, (int)(SignatureBase + I * 256), Signature, 0, Signature.Length);
                    Buffer.BlockCopy(SHA256.HashData(Signature), 0, Check, 32 + FileCount * 32 + I * 32, 32);
                }

                var SignatureOverall = new byte[256];
                Buffer.BlockCopy(Data, OverallSignatureOffset, SignatureOverall, 0, 256);
                Result.OverallDigest = BitConverter.ToString(SHA256.HashData(Check)).Replace("-", "").ToLowerInvariant();
                Result.OverallSignatureValid = Rsa.VerifyData(Check, SignatureOverall,
                    HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception E)
            {
                Result.Error = E.Message;
            }

            return Result;
        }

        private static uint ReadUInt32BE(byte[] Data, int Offset) =>
            (uint)((Data[Offset] << 24) | (Data[Offset + 1] << 16) | (Data[Offset + 2] << 8) | Data[Offset + 3]);

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
    }
}
