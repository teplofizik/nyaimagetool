using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAncientFs;
using System;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace NyaFsTests
{
    public class V6FsTests
    {
        private static string Sample()
        {
            var Path = Environment.GetEnvironmentVariable("NYAIMAGETOOL_V6_IMAGE");
            if (!string.IsNullOrEmpty(Path) && File.Exists(Path))
                return Path;

            foreach (var Candidate in new[]
            {
                @"F:\TEMP\v6_samples\ra1boot.rx50",
                @"F:\TEMP\v6\ra1boot.rx50"
            })
                if (File.Exists(Candidate))
                    return Candidate;

            return null;
        }

        [Fact]
        public void V6_ReadSample()
        {
            var Source = Sample();
            if (Source == null)
                return;

            var Data = File.ReadAllBytes(Source);
            Assert.True(V6FsReader.IsV6(Data));

            var Reader = new V6FsReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Equal(9, Root.Length);
            Assert.Contains(Root, E => E.ShortName == "bin");
            Assert.Contains(Root, E => E.ShortName == "etc");
            Assert.Contains(Root, E => E.ShortName == "unix");

            Assert.Equal(4900, Reader.Read("/bin/ls").Length);
            Assert.Equal("177cf8131f066b3ab35d017e09e4fc8f6b672b7f10445ad3ccf816b13e6d1da4",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/bin/ls"))).ToLowerInvariant());
            Assert.Equal(37452, Reader.Read("/unix").Length);
            Assert.Equal("04c6cf542d61933c66bfe346263ac0ceb4cdbea2d221fceaed89e26831224420",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/unix"))).ToLowerInvariant());
            Assert.Equal("root::0:", System.Text.Encoding.ASCII.GetString(Reader.Read("/etc/passwd")).Substring(0, 8));
        }

        [Fact]
        public void V6_Loads_Via_Pipeline()
        {
            var Source = Sample();
            if (Source == null)
                return;

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_v6_sample.rx50");
            File.Copy(Source, Path, true);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new V6FsFilesystemPlugin());

                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Assert.True(Fs.Loaded);
                Assert.Equal("v6", Fs.FilesystemFormat);
                Assert.NotNull(Fs.GetElement("unix"));
                Assert.Equal(37452, Fs.GetElement("unix").Size);
            }
            finally
            {
                File.Delete(Path);
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }
    }
}
