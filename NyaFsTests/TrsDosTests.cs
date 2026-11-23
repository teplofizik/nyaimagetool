using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAncientFs;
using System;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace NyaFsTests
{
    public class TrsDosTests
    {
        private static string Sample(string Name)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_DMK_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\dmk";
            var Path = System.IO.Path.Combine(Root, Name);
            return File.Exists(Path) ? Path : null;
        }

        [Fact]
        public void TrsDos_ReadChurchData()
        {
            var Path = Sample("sample0.dmk");
            if (Path == null)
                return;

            var Data = File.ReadAllBytes(Path);
            Assert.True(TrsDosReader.TryDetect(Data, Path));

            var Reader = new TrsDosReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Equal(17, Root.Length);
            Assert.Contains(Root, E => E.ShortName == "BOOT.SYS");
            Assert.Contains(Root, E => E.ShortName == "DIR.SYS");
            Assert.Contains(Root, E => E.ShortName == "CHURCH00.DA2");
            Assert.Contains(Root, E => E.ShortName == "MONEY000.PR1");

            Assert.Equal(518, Reader.Read("/BOOT.SYS").Length);
            Assert.Equal(2067, Reader.Read("/DIR.SYS").Length);
            Assert.Equal(411, Reader.Read("/CHURCH00.KEY").Length);
            Assert.Equal(1290, Reader.Read("/CHURCH00.LB1").Length);
            Assert.Equal(514, Reader.Read("/CHURCH00.MAP").Length);
            Assert.Equal(1290, Reader.Read("/CHURCH00.PM1").Length);
            Assert.Equal(316, Reader.Read("/MONEY000.KEY").Length);
            Assert.Equal(1289, Reader.Read("/MONEY000.PR1").Length);
            Assert.Equal(0, Reader.Read("/CHURCH00.DA2").Length);
            Assert.Equal("94ef4b0db28a3117a2b1a36f6c8e4342064a62266d50c7f59c753ceaf5b1f77b",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/CHURCH00.PM1"))).ToLowerInvariant());
        }

        [Fact]
        public void TrsDos_ReadSecondChurchCopy()
        {
            var Path = Sample("sample1.dmk");
            if (Path == null)
                return;

            var Reader = new TrsDosReader(File.ReadAllBytes(Path));
            Assert.Equal(17, Reader.ReadDir("/").Length);
            Assert.Equal(1290, Reader.Read("/CHURCH00.PM1").Length);
            Assert.Equal(1290, Reader.Read("/MONEY000.PM1").Length);
        }
        [Fact]
        public void TrsDos_DoesNotClaimCpm()
        {
            var Path = Sample("sample2.dmk");
            if (Path == null)
                return;

            Assert.False(TrsDosReader.TryDetect(File.ReadAllBytes(Path), Path));
        }
        [Fact]
        public void TrsDos_Loads_Via_Pipeline()
        {
            var Source = Sample("sample0.dmk");
            if (Source == null)
                return;

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_trsdos.dmk");
            File.Copy(Source, Path, true);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new TrsDosFilesystemPlugin());

                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Assert.True(Fs.Loaded);
                Assert.Equal("trsdos", Fs.FilesystemFormat);
                Assert.NotNull(Fs.GetElement("CHURCH00.PM1"));
                Assert.Equal(1290, Fs.GetElement("CHURCH00.PM1").Size);
            }
            finally
            {
                File.Delete(Path);
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }
    }
}
