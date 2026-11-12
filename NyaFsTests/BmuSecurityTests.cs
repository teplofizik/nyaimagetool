using NyaFsBmu.Types;
using System;
using System.IO;
using Xunit;

namespace NyaFsTests
{
    public class BmuSecurityTests
    {
        [Fact]
        public void Bmu_Public_Key_Is_Normalized()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                return;

            var Path = System.IO.Path.Combine(Root, "bmu", "update.bmu");
            if (!File.Exists(Path))
                return;

            var Data = File.ReadAllBytes(Path);
            Assert.NotNull(BmuSecurity.ExtractPublicKeyPem(Data));
            Assert.NotNull(BmuSecurity.NormalizePublicKey(Data));
            Assert.False(String.IsNullOrEmpty(BmuSecurity.PublicKeyFingerprint(Data)));

            var Verification = BmuSecurity.VerifySingle(Data);
            Assert.True(Verification.Valid, $"Error={Verification.Error}; Overall={Verification.OverallSignatureValid}; Digest={Verification.OverallDigest}; Files={string.Join(",", Verification.Files.ConvertAll(F => F.Name + "=" + F.Valid))}");
            var RootKey = @"G:\Projects\Асик конфиг\DATABASE\S21 XP\Образ\s21_xp\etc\bitmain.pub";
            if (File.Exists(RootKey))
            {
                var Chained = BmuSecurity.VerifySingle(Data, File.ReadAllText(RootKey));
                Assert.True(Chained.MinerKeySignatureValid, Chained.Error);
                Assert.True(Chained.Valid, Chained.Error);
            }
            Assert.True(Verification.OverallSignatureValid);
            Assert.All(Verification.Files, F => Assert.True(F.Valid, F.Name));
        }
    }
}
