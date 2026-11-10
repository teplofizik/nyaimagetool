using System;
using System.Reflection;
using System.Linq;
using System.Text;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
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
        public void LinuxPlugin_Exposes_UserAdmin_Commands()
        {
            var Names = new NyaFsLinux.LinuxPlugin().GetGenerators().Select(G => G.Name).ToArray();
            Assert.Contains("lsusr", Names);
            Assert.Contains("useradd", Names);
            Assert.Contains("usermod", Names);
            Assert.Contains("userdel", Names);
            Assert.Contains("groupadd", Names);
            Assert.Contains("groupdel", Names);
        }

        [Fact]
        public void LinuxPlugin_UserAdd_Uses_Core_Identity_And_Shadow()
        {
            var Fs = new LinuxFilesystem();
            var Etc = new NyaFs.Filesystem.Universal.Items.Dir("etc", 0, 0, 493);
            var Home = new NyaFs.Filesystem.Universal.Items.Dir("home", 0, 0, 493);
            Fs.Fs.Root.Items.Add(Etc);
            Fs.Fs.Root.Items.Add(Home);
            Etc.Items.Add(new NyaFs.Filesystem.Universal.Items.File("etc/passwd", 0, 0, 420,
                Encoding.ASCII.GetBytes("root:x:0:0:root:/root:/bin/sh\n")));
            Etc.Items.Add(new NyaFs.Filesystem.Universal.Items.File("etc/group", 0, 0, 420,
                Encoding.ASCII.GetBytes("root:x:0:\n")));
            Etc.Items.Add(new NyaFs.Filesystem.Universal.Items.File("etc/shadow", 0, 0, 400,
                Encoding.ASCII.GetBytes("root:!:::::::\n")));

            var Base = new ScriptBaseAll();
            foreach (var Generator in new NyaFsLinux.LinuxPlugin().GetGenerators())
                Base.Add(Generator);
            var Parser = new ScriptParser(Base);
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.SetFs(Fs);

            Proc.Process(Parser.ParseScript("t", new[] { "useradd app 2000 2000 /home/app /bin/sh" }));

            Assert.NotNull(Fs.Identities.FindUser("app"));
            Assert.Equal(2000u, Fs.Identities.FindUser("app").Uid);
            Assert.NotNull(Fs.GetElement("/home/app"));
            Assert.Contains("app:!", Encoding.UTF8.GetString(
                ((NyaFs.Filesystem.Universal.Items.File)Fs.GetElement("etc/shadow")).Content));
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
