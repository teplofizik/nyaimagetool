using System;
using System.IO;
using System.Text;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using Xunit;

namespace NyaFsTests
{
    public class SpiffsTests
    {
        private static string Corpus(string Rel)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\Images";
            var Path = System.IO.Path.Combine(Root, Rel.Replace('/', System.IO.Path.DirectorySeparatorChar));
            return File.Exists(Path) ? Path : null;
        }

        [Fact]
        public void Spiffs_Reader_Reads_Files()
        {
            var Path = Corpus("mcu/test.spiffs");
            if (Path == null) return; // no corpus, skip

            var R = new NyaFsMcuFs.SpiffsReader(File.ReadAllBytes(Path));

            var Sys = R.Read("config/system.yaml");
            Assert.NotNull(Sys);
            Assert.Equal(40, Sys.Length);
            Assert.StartsWith("device:", Encoding.UTF8.GetString(Sys));

            Assert.Equal("spiffs test fixture", Encoding.UTF8.GetString(R.Read("readme.txt")));
            Assert.Equal(53, R.Read("web/index.html").Length);
        }

        [Fact]
        public void Spiffs_Write_RoundTrip()
        {
            var Path = Corpus("mcu/test.spiffs");
            if (Path == null) return; // no corpus, skip

            var R = new NyaFsMcuFs.SpiffsReader(File.ReadAllBytes(Path));
            var Sys = R.Read("config/system.yaml");
            var Readme = R.Read("readme.txt");
            var Index = R.Read("web/index.html");
            Assert.NotNull(Sys);
            Assert.NotNull(Readme);
            Assert.NotNull(Index);

            var B = new NyaFsMcuFs.SpiffsBuilder(1048576);
            B.File("config/system.yaml", Sys, 0, 0, 0);
            B.File("readme.txt", Readme, 0, 0, 0);
            B.File("web/index.html", Index, 0, 0, 0);

            var Image = B.GetFilesystemImage();

            var R2 = new NyaFsMcuFs.SpiffsReader(Image);
            Assert.Equal(Sys, R2.Read("config/system.yaml"));
            Assert.Equal(Readme, R2.Read("readme.txt"));
            Assert.Equal(Index, R2.Read("web/index.html"));
        }

        [Fact]
        public void Spiffs_Loads_Via_Pipeline()
        {
            var Path = Corpus("mcu/test.spiffs");
            if (Path == null) return; // no corpus, skip

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Plugins.Load(new NyaFsMcuFs.SpiffsFilesystemPlugin());

            Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

            var Fs = Proc.GetFs();
            Assert.NotNull(Fs);
            Assert.True(Fs.Loaded);
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Spiffs, Fs.FilesystemType);
            Assert.NotNull(Fs.GetElement("config/system.yaml"));
        }
    }
}
