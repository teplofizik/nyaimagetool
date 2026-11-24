using FsFile = NyaFs.Filesystem.Universal.Items.File;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAncientFs;
using System;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace NyaFsTests
{
    public class AtariDosTests
    {
        private static string Sample(string Name)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_ATR_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\atr_samples";
            var Path = System.IO.Path.Combine(Root, Name);
            return File.Exists(Path) ? Path : null;
        }

        [Fact]
        public void AtariDos_ReadDOS25()
        {
            var Path = Sample("dos25.atr");
            if (Path == null) return;
            var Data = File.ReadAllBytes(Path);
            Assert.True(AtariDosReader.IsAtariDos(Data, "dos25.atr"));
            var Reader = new AtariDosReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Equal(2, Root.Length);
            Assert.Contains(Root, E => E.ShortName == "BLONDELL");
            Assert.Contains(Root, E => E.ShortName == "WARRIOR.2");
            Assert.Equal(2048, Reader.Read("/BLONDELL").Length);
            Assert.Equal("6c528d0ede91845af44ec7cfb437d3f47039aa4d740014116a2fe6c4e8d8b480",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/BLONDELL"))).ToLowerInvariant());
        }

        [Fact]
        public void AtariDos_ReadWorkingDisk()
        {
            var Path = Sample("working.atr");
            if (Path == null) return;
            var Reader = new AtariDosReader(File.ReadAllBytes(Path));
            Assert.Contains(Reader.ReadDir("/"), E => E.ShortName == "DOS.SYS");
            Assert.Equal(4736, Reader.Read("/DOS.SYS").Length);
        }

        [Fact]
        public void AtariDos_RebuildsImage()
        {
            var Source = Sample("dos25.atr");
            if (Source == null) return;

            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new AtariDosFilesystemPlugin());
                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Source + "\"" }));

                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Fs.Fs.Root.Items.Add(new FsFile("NEW.TXT", 0, 0, 0x1A4,
                    System.Text.Encoding.ASCII.GetBytes("hello atari")));

                var Writer = new AtariDosWriter();
                Assert.True(Writer.CheckFilesystem(Fs));
                Writer.WriteFs(Fs);
                Assert.NotNull(Writer.RawStream);

                var Reader = new AtariDosReader(Writer.RawStream);
                Assert.Contains(Reader.ReadDir("/"), E => E.ShortName == "NEW.TXT");
                Assert.Equal("hello atari",
                    System.Text.Encoding.ASCII.GetString(Reader.Read("/NEW.TXT")).TrimEnd('\0'));
            }
            finally
            {
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }

        [Fact]
        public void AtariDos_Loads_Via_Pipeline()
        {
            var Source = Sample("dos25.atr");
            if (Source == null) return;
            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_atr_sample.atr");
            File.Copy(Source, Path, true);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new AtariDosFilesystemPlugin());
                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));
                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Assert.True(Fs.Loaded);
                Assert.Equal("atari", Fs.FilesystemFormat);
                Assert.NotNull(Fs.GetElement("BLONDELL"));
            }
            finally
            {
                File.Delete(Path);
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }
    }
}
