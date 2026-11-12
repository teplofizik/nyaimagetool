using NyaFsFit;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace NyaFsTests
{
    public class FitSignerTests
    {
        [Fact]
        public void Sign_Matches_Mkimage_And_Verifies()
        {
            var Dir = Environment.GetEnvironmentVariable("NYAIMAGETOOL_FIT_FIXTURE") ?? @"F:\TEMP\fitsign";
            var Fit = Path.Combine(Dir, "test.fit");
            var Key = Path.Combine(Dir, "keys", "test.key");
            var Cert = Path.Combine(Dir, "keys", "test.crt");
            if (!File.Exists(Fit) || !File.Exists(Key) || !File.Exists(Cert))
                return;

            var Original = File.ReadAllBytes(Fit);
            var Signed = FitSigner.Sign(Original, Key, out var Count, out var Error);
            Assert.True(Signed != null, Error);
            Assert.Equal(1, Count);
            Assert.Equal(Original, Signed);

            var Checks = FitSigner.Verify(Signed, Cert, out Error);
            Assert.True(Checks != null, Error);
            Assert.Single(Checks);
            Assert.True(Checks[0].Valid, Checks[0].Error);
        }
    }
}
