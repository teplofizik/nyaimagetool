using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAncientFs;
using System;
using System.IO;
using Xunit;

namespace NyaFsTests
{
    public class ImdTests
    {
        private static string Sample(string Name)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMD_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\imd_samples";
            var Path = System.IO.Path.Combine(Root, Name);
            return File.Exists(Path) ? Path : null;
        }

        [Fact]
        public void Imd_ReadCocoDisk()
        {
            var Path = Sample("DISKUTIL IMPORTANT.imd");
            if (Path == null)
                return;

            var Data = File.ReadAllBytes(Path);
            Assert.True(ImdImage.TryRead(Data, out var Imd));
            Assert.Equal(40, Imd.Tracks);
            Assert.Equal(1, Imd.Sides);
            Assert.Equal(18, Imd.SectorsPerTrack);
            Assert.Equal(256, Imd.SectorSize);
            Assert.Equal(184320, Imd.Raw.Length);

            var Reader = new CocoDosReader(Imd.Raw);
            Assert.NotEmpty(Reader.ReadDir("/"));
        }

        [Fact]
        public void Imd_Loads_Coco_Via_Pipeline()
        {
            var Source = Sample("DISKUTIL IMPORTANT.imd");
            if (Source == null)
                return;

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_coco.imd");
            File.Copy(Source, Path, true);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new ImdImagePlugin());
                Proc.Plugins.Load(new CocoDosFilesystemPlugin());

                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Assert.True(Fs.Loaded);
                Assert.Equal("cocodos", Fs.FilesystemFormat);
                Assert.NotNull(Fs.GetElement("DISKUTIL.BIN"));
            }
            finally
            {
                File.Delete(Path);
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }
    }
}
