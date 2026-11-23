using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAncientFs;
using System;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace NyaFsTests
{
    public class CocoDosTests
    {
        private static string Sample(string Name)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_COCO_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\coco";
            var Path = System.IO.Path.Combine(Root, Name);
            return File.Exists(Path) ? Path : null;
        }

        [Fact]
        public void CocoDos_ReadSample()
        {
            var Path = Sample("sample0.dsk");
            if (Path == null)
                return;

            var Data = File.ReadAllBytes(Path);
            Assert.True(CocoDosReader.TryDetect(Data, Path));

            var Reader = new CocoDosReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Equal(12, Root.Length);
            Assert.Contains(Root, E => E.ShortName == "MENU.BAS");
            Assert.Contains(Root, E => E.ShortName == "NEUTROID.BIN");
            Assert.Equal(849, Reader.Read("/MENU.BAS").Length);
            Assert.Equal(99, Reader.Read("/NEUTROID.BAS").Length);
            Assert.Equal(10245, Reader.Read("/NEUTROID.BIN").Length);
            Assert.Equal(12298, Reader.Read("/GFX.BIN").Length);
            Assert.Equal("17336a70571372a42e367ca8b12f2ebba4cc7af0bae6856ca55884d438002766",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/MENU.BAS"))).ToLowerInvariant());
            Assert.Equal("67fe6b0a79abba4f8290dac8a9a2340e6d27c424c9cc5f0eeadd3513441dd104",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/GFX.BIN"))).ToLowerInvariant());
        }

        [Fact]
        public void CocoDos_Loads_Via_Pipeline()
        {
            var Source = Sample("sample0.dsk");
            if (Source == null)
                return;

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_coco.dsk");
            File.Copy(Source, Path, true);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new CocoDosFilesystemPlugin());

                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Assert.True(Fs.Loaded);
                Assert.Equal("cocodos", Fs.FilesystemFormat);
                Assert.NotNull(Fs.GetElement("NEUTROID.BIN"));
                Assert.Equal(10245, Fs.GetElement("NEUTROID.BIN").Size);
            }
            finally
            {
                File.Delete(Path);
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }
    }
}
