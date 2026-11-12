using NyaFs.Keys;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace NyaFsTests
{
    public class KeyStoreTests
    {
        [Fact]
        public void KeyStore_Imports_Asymmetric_And_Symmetric_Profiles()
        {
            var Root = Path.Combine(Path.GetTempPath(), "nyafs-keys-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            var PrivateFile = Path.Combine(Root, "test.key.pem");
            var CertFile = Path.Combine(Root, "test.crt");
            var Secret = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var SecretFile = Path.Combine(Root, "secret.bin");

            using (var Rsa = RSA.Create(2048))
            {
                var Request = new CertificateRequest("CN=nyafs-test", Rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                using (var Cert = Request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1)))
                {
                    File.WriteAllText(PrivateFile, Rsa.ExportPkcs8PrivateKeyPem());
                    File.WriteAllBytes(CertFile, Cert.Export(X509ContentType.Cert));
                }
            }
            File.WriteAllBytes(SecretFile, Secret);

            var Store = new FileKeyStore(Path.Combine(Root, "store"));
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.KeyStore = Store;
            Proc.Process(Parser.ParseScript("t", new[]
            {
                "keys import test \"" + PrivateFile + "\" \"" + CertFile + "\"",
                "keys import-symmetric sym \"" + SecretFile + "\""
            }));

            var Record = Store.Find("test");
            Assert.NotNull(Record);
            Assert.Equal(KeyKind.Asymmetric, Record.Kind);
            Assert.False(String.IsNullOrEmpty(Record.Fingerprint));
            var Private = Store.GetPrivateMaterial(Record);
            Assert.NotNull(Private);
            Assert.True(Private.Length > 0);

            var Symmetric = Store.Find("sym");
            Assert.NotNull(Symmetric);
            Assert.Equal(KeyKind.Symmetric, Symmetric.Kind);
            Assert.Equal(Secret, Store.GetSymmetricSecret(Symmetric));
        }
    }
}
