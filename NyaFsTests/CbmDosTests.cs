using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAncientFs;
using System;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace NyaFsTests
{
    public class CbmDosTests
    {
        private static string Sample(string Name)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_D64_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\d64_samples";
            var Path = System.IO.Path.Combine(Root, Name);
            return File.Exists(Path) ? Path : null;
        }

        [Fact]
        public void CbmDos_ReadD64()
        {
            var Path = Sample("cool.d64");
            if (Path == null)
                return;

            var Data = File.ReadAllBytes(Path);
            Assert.True(CbmDosReader.IsCbmDos(Data, "cool.d64"));

            var Reader = new CbmDosReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Contains(Root, E => E.ShortName == "WORM");
            Assert.Contains(Root, E => E.ShortName == "MENU");
            Assert.Equal(6096, Reader.Read("/WORM").Length);
            Assert.Equal("6a6232f31c75f781d25fdf9349f65eddff3460e9fe7e034c869b73d8c32bdf8f",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/WORM"))).ToLowerInvariant());
            Assert.Equal(new byte[] { 0x01, 0x08 }, Reader.Read("/WORM")[0..2]);
        }

        [Fact]
        public void CbmDos_C128Disk()
        {
            var Path = Sample("c128.d64");
            if (Path == null)
                return;

            var Data = File.ReadAllBytes(Path);
            var Reader = new CbmDosReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.NotEmpty(Root);
            Assert.Contains(Root, E => E.ShortName.EndsWith(".BBS"));
        }

        [Fact]
        public void CbmDos_Loads_Via_Pipeline()
        {
            var Source = Sample("cool.d64");
            if (Source == null)
                return;

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_cbm_sample.d64");
            File.Copy(Source, Path, true);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new CbmDosFilesystemPlugin());

                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Assert.True(Fs.Loaded);
                Assert.Equal("cbm", Fs.FilesystemFormat);
                Assert.NotNull(Fs.GetElement("WORM"));
                Assert.Equal(6096, Fs.GetElement("WORM").Size);
            }
            finally
            {
                File.Delete(Path);
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }
    }
}
