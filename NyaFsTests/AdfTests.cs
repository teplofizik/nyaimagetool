using FsFile = NyaFs.Filesystem.Universal.Items.File;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAncientFs;
using System;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace NyaFsTests
{
    public class AdfTests
    {
        private static string Sample(string Name)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_ADF_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\adf_samples";
            var Path = System.IO.Path.Combine(Root, Name);
            return File.Exists(Path) ? Path : null;
        }

        [Fact]
        public void Adf_ReadOFS()
        {
            var Path = Sample("extras.adf");
            if (Path == null)
                return;

            var Data = File.ReadAllBytes(Path);
            Assert.True(AdfReader.IsAdf(Data));

            var Reader = new AdfReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Contains(Root, E => E.ShortName == "Trashcan");
            Assert.Contains(Root, E => E.ShortName == "AmigaBASIC");
            Assert.Contains(Root, E => E.ShortName == "Amiga Tutor");
            Assert.Equal(66, Reader.Read("/.info").Length);
            Assert.Equal("36094bc4b64320b82126c10e4fbabe8e33f3812dd19a6161ad1a8d6b7ba13259",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/.info"))).ToLowerInvariant());
            Assert.Contains(Reader.ReadDir("/Trashcan"), E => E.ShortName == ".info");
        }

        [Fact]
        public void Adf_ReadSecondDisk()
        {
            var Path = Sample("kaleidoscope.adf");
            if (Path == null)
                return;

            var Reader = new AdfReader(File.ReadAllBytes(Path));
            Assert.NotEmpty(Reader.ReadDir("/"));
        }

        [Fact]
        public void Adf_RebuildsImage()
        {
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Fs = new LinuxFilesystem();
                Fs.Fs.Root.Items.Add(new FsFile("NEW.TXT", 0, 0, 0x1A4,
                    System.Text.Encoding.ASCII.GetBytes("hello amiga")));

                var Writer = new AdfWriter();
                Assert.True(Writer.CheckFilesystem(Fs));
                Writer.WriteFs(Fs);
                Assert.NotNull(Writer.RawStream);
                Assert.True(AdfReader.IsAdf(Writer.RawStream));

                var Reader = new AdfReader(Writer.RawStream);
                Assert.Contains(Reader.ReadDir("/"), E => E.ShortName == "NEW.TXT");
                Assert.Equal("hello amiga",
                    System.Text.Encoding.ASCII.GetString(Reader.Read("/NEW.TXT")));
            }
            finally
            {
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }

        [Fact]
        public void Adf_Loads_Via_Pipeline()
        {
            var Source = Sample("extras.adf");
            if (Source == null)
                return;

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_adf_sample.adf");
            File.Copy(Source, Path, true);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new AdfFilesystemPlugin());

                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Assert.True(Fs.Loaded);
                Assert.Equal("adf", Fs.FilesystemFormat);
                Assert.NotNull(Fs.GetElement("AmigaBASIC"));
            }
            finally
            {
                File.Delete(Path);
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }
    }
}
