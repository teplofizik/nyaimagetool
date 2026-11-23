using NyaFsAncientFs;
using System;
using System.IO;
using Xunit;

namespace NyaFsTests
{
    public class WozTests
    {
        private static string Sample(string Name)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_WOZ_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\woz_samples";
            var Path = System.IO.Path.Combine(Root, Name);
            return File.Exists(Path) ? Path : null;
        }

        [Fact]
        public void Woz_DecodesProDos()
        {
            var Path = Sample("M&M Software - RT1225 Convert It.woz");
            if (Path == null)
                return;

            Assert.True(WozImage.TryRead(File.ReadAllBytes(Path), out var Woz));
            Assert.Equal(143360, Woz.ProDosImage.Length);
            Assert.True(ProDosReader.IsProDos(Woz.ProDosImage, "disk.po"));
            Assert.NotEmpty(new ProDosReader(Woz.ProDosImage).ReadDir("/"));
        }

        [Fact]
        public void Woz_Loads_Via_Pipeline()
        {
            var Source = Sample("M&M Software - RT1225 Convert It.woz");
            if (Source == null)
                return;

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_test.woz");
            File.Copy(Source, Path, true);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new NyaFs.Processor.Scripting.ScriptParser(
                    new NyaFs.Processor.Scripting.ScriptBaseAll());
                var Proc = new NyaFs.Processor.ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new WozImagePlugin());
                Proc.Plugins.Load(new ProDosFilesystemPlugin());

                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Assert.True(Fs.Loaded);
                Assert.Equal("prodos", Fs.FilesystemFormat);
            }
            finally
            {
                File.Delete(Path);
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }
    }
}
