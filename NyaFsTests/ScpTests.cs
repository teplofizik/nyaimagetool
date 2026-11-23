using NyaFsAncientFs;
using System;
using System.IO;
using Xunit;

namespace NyaFsTests
{
    public class ScpTests
    {
        private static string Sample(string Name)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_WOZ_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\woz_samples";
            var Path = System.IO.Path.Combine(Root, Name);
            return File.Exists(Path) ? Path : null;
        }

        [Fact]
        public void Scp_Loads_Via_Pipeline()
        {
            var Source = Sample("clean_prodos.scp");
            if (Source == null)
                return;

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_clean.scp");
            File.Copy(Source, Path, true);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new NyaFs.Processor.Scripting.ScriptParser(
                    new NyaFs.Processor.Scripting.ScriptBaseAll());
                var Proc = new NyaFs.Processor.ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new ScpImagePlugin());
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

        [Fact]
        public void Scp_DecodesProDosFlux()
        {
            var Path = Sample("clean_prodos.scp");
            if (Path == null)
                return;

            Assert.True(ScpImage.TryRead(File.ReadAllBytes(Path), out var Scp));
            Assert.Equal(143360, Scp.ProDosImage.Length);
            Assert.True(ProDosReader.IsProDos(Scp.ProDosImage, "disk.po"));
            Assert.NotEmpty(new ProDosReader(Scp.ProDosImage).ReadDir("/"));
        }
    }
}
