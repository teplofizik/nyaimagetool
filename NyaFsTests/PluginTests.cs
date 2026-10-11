using System;
using System.Reflection;
using NyaFs.ImageFormat.Helper;
using Xunit;

namespace NyaFsTests
{
    public class PluginTests
    {
        [Theory]
        [InlineData("$1$salt$hash", "MD5")]
        [InlineData("$6$salt$hash", "SHA-512")]
        [InlineData("$5$salt$hash", "SHA-256")]
        [InlineData("$2a$10$salt$hash", "Blowfish")]
        [InlineData("$2y$10$salt$hash", "Blowfish")]
        public void User_HashType(string hash, string expected)
        {
            var user = new NyaFsLinux.Linux.Users.User("root:x:0:0:root:/root:/bin/sh");
            user.ProcessShadow($"root:{hash}:19000:0:99999:7:::");

            Assert.Equal(expected, user.HashType);
        }

        [Fact]
        public void BmuImage_Detects_Own_Magic()
        {
            var data = new byte[0x800];
            data[0] = 0x26;
            Assert.True(NyaFsBmu.Types.BmuImage.IsBmuImage(data));

            Assert.False(NyaFsBmu.Types.BmuImage.IsBmuImage(new byte[0x10]));
        }
    }
}
