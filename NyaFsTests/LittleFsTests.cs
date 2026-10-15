using System;
using System.IO;
using System.Text;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using Xunit;

namespace NyaFsTests
{
    public class LittleFsTests
    {
        private static string Corpus(string Rel)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\Images";
            var Path = System.IO.Path.Combine(Root, Rel.Replace('/', System.IO.Path.DirectorySeparatorChar));
            return File.Exists(Path) ? Path : null;
        }

        [Fact]
        public void LittleFs_Reader_Reads_Files()
        {
            var Path = Corpus("mcu/littlefs.bin");
            if (Path == null) return; // no corpus, skip

            var R = new NyaFsMcuFs.LittleFsReader(File.ReadAllBytes(Path));

            Assert.Equal(4063232, R.TotalSize);

            var System = R.Read("config/system.yaml");
            Assert.NotNull(System);
            Assert.Equal(528, System.Length);
            Assert.StartsWith("device:", Encoding.UTF8.GetString(System));

            var Index = R.Read("web/index.html");
            Assert.NotNull(Index);
            Assert.Equal(29811, Index.Length);
            Assert.StartsWith("<!DOCTYPE html>", Encoding.UTF8.GetString(Index));
        }

        [Fact]
        public void LittleFs_Write_RoundTrip()
        {
            var Path = Corpus("mcu/littlefs.bin");
            if (Path == null) return; // no corpus, skip

            var R = new NyaFsMcuFs.LittleFsReader(File.ReadAllBytes(Path));
            var System = R.Read("config/system.yaml");
            var Index = R.Read("web/index.html");
            Assert.NotNull(System);
            Assert.NotNull(Index);

            var B = new NyaFsMcuFs.LittleFsBuilder(4096, 992);
            B.Directory("config", 0, 0, 0);
            B.File("config/system.yaml", System, 0, 0, 0);
            B.Directory("web", 0, 0, 0);
            B.File("web/index.html", Index, 0, 0, 0);

            var Image = B.GetFilesystemImage();

            var R2 = new NyaFsMcuFs.LittleFsReader(Image);
            Assert.Equal(System, R2.Read("config/system.yaml"));
            Assert.Equal(Index, R2.Read("web/index.html"));
        }

        [Fact]
        public void LittleFs_Loads_Via_Pipeline()
        {
            var Path = Corpus("mcu/littlefs.bin");
            if (Path == null) return; // no corpus, skip

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Plugins.Load(new NyaFsMcuFs.LittleFsFilesystemPlugin());

            Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

            var Fs = Proc.GetFs();
            Assert.NotNull(Fs);
            Assert.True(Fs.Loaded);
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.LittleFs, Fs.FilesystemType);
            Assert.Equal(4063232, Fs.FsSize);
            Assert.NotNull(Fs.GetElement("web/index.html"));
        }
    }
}
