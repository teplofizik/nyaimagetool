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
    public class ProDosTests
    {
        private static string Sample(string Name)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_PRODOS_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\prodos_samples";
            var Path = System.IO.Path.Combine(Root, Name);
            return File.Exists(Path) ? Path : null;
        }

        [Fact]
        public void ProDos_ReadSamples()
        {
            var Path = Sample("samples.po");
            if (Path == null) return;
            var Data = File.ReadAllBytes(Path);
            Assert.True(ProDosReader.IsProDos(Data, "samples.po"));
            var Reader = new ProDosReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Equal(5, Root.Length);
            Assert.Contains(Root, E => E.ShortName == "MOMS.BULLETIN");
            Assert.Equal(3605, Reader.Read("/MOMS.BULLETIN").Length);
            Assert.Equal("b15265d0d4c1a30059a2433b1f8875de8ede991b7462a7e3be437e2d02a63e12",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/MOMS.BULLETIN"))).ToLowerInvariant());
        }

        [Fact]
        public void ProDos_ReadLargeFile()
        {
            var Path = Sample("ledger.po");
            if (Path == null) return;
            var Reader = new ProDosReader(File.ReadAllBytes(Path));
            Assert.Contains(Reader.ReadDir("/"), E => E.ShortName == "LEDGER");
            Assert.Equal(129855, Reader.Read("/LEDGER").Length);
            Assert.Equal("2b16196fc22477b1f812c8441c5cd894b67046ecd71006747a8f7df5a3b90a10",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/LEDGER"))).ToLowerInvariant());
        }

        [Fact]
        public void ProDos_RebuildsImage()
        {
            var Source = Sample("samples.po");
            if (Source == null) return;

            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new ProDosFilesystemPlugin());
                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Source + "\"" }));
                var Fs = Proc.GetFs();
                Fs.Fs.Root.Items.Add(new FsFile("NEW.TXT", 0, 0, 0x1A4,
                    System.Text.Encoding.ASCII.GetBytes("hello prodos")));

                var Writer = new ProDosWriter();
                Assert.True(Writer.CheckFilesystem(Fs));
                Writer.WriteFs(Fs);
                Assert.NotNull(Writer.RawStream);
                Assert.True(ProDosReader.IsProDos(Writer.RawStream, "rebuilt.po"));

                var Reader = new ProDosReader(Writer.RawStream);
                Assert.Contains(Reader.ReadDir("/"), E => E.ShortName == "NEW.TXT");
                Assert.Equal("hello prodos",
                    System.Text.Encoding.ASCII.GetString(Reader.Read("/NEW.TXT")));
            }
            finally
            {
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }

        [Fact]
        public void ProDos_Loads_Via_Pipeline()
        {
            var Source = Sample("samples.po");
            if (Source == null) return;
            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_prodos_sample.po");
            File.Copy(Source, Path, true);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new ProDosFilesystemPlugin());
                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));
                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Assert.True(Fs.Loaded);
                Assert.Equal("prodos", Fs.FilesystemFormat);
                Assert.NotNull(Fs.GetElement("MOMS.BULLETIN"));
            }
            finally
            {
                File.Delete(Path);
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }
    }
}
