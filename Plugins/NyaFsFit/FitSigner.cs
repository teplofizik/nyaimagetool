using NyaFs.ImageFormat.Composite;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NyaFsFit
{
    /// <summary>
    /// File/key convenience wrapper around the core FIT signer.
    /// </summary>
    public static class FitSigner
    {
        public static byte[] Sign(byte[] Data, string KeyPath, out int SignedCount, out string Error)
        {
            using var Rsa = LoadKey(KeyPath);
            return FitSignature.Sign(Data, Rsa, out SignedCount, out Error);
        }

        public static FitSignatureCheck[] Verify(byte[] Data, string KeyPath, out string Error)
        {
            using var Rsa = LoadKey(KeyPath);
            return FitSignature.Verify(Data, Rsa, out Error);
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
    }
}
