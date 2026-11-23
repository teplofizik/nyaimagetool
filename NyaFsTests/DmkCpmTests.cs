using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAncientFs;
using System;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace NyaFsTests
{
    public class DmkCpmTests
    {
        private static string Sample(string Name)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_DMK_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\dmk";
            var Path = System.IO.Path.Combine(Root, Name);
            return File.Exists(Path) ? Path : null;
        }

        [Fact]
        public void Dmk_ReadCpmQc()
        {
            var Path = Sample("sample2.dmk");
            if (Path == null)
                return;

            var Data = File.ReadAllBytes(Path);
            Assert.True(DmkReader.TryDetect(Data, Path, out var Geometry));
            Assert.Equal("trs80-dmk", Geometry.Name);
            Assert.Equal(40, Geometry.Tracks);
            Assert.Equal(18, Geometry.SectorsPerTrack);
            Assert.Equal(256, Geometry.SectorSize);

            var Reader = new DmkReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Equal(20, Root.Length);
            Assert.Contains(Root, E => E.ShortName == "GETMOR.OVL");
            Assert.Contains(Root, E => E.ShortName == "MENU.OVL");
            Assert.Contains(Root, E => E.ShortName == "XCMD.OVL");
            Assert.Equal(7424, Reader.Read("/GETMOR.OVL").Length);
            Assert.Equal(7808, Reader.Read("/MENU.OVL").Length);
            Assert.Equal(8192, Reader.Read("/XCMD.OVL").Length);
            Assert.Equal(32896, Reader.Read("/QC.COM").Length);
            Assert.Equal("9ab75916e8edae46342f1406a5027c99b05416b68357d2c9a7d31e73f74e7c36",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/GETMOR.OVL"))).ToLowerInvariant());
            Assert.Equal("3403f3b842937e3649bcac900f49446a87ded7c38c0931c594493e1682963695",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/MENU.OVL"))).ToLowerInvariant());
            Assert.Equal("34c312d020777fb695c8798148ef0c28737b421b263e276c7b7c109c8dfee77b",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/QC.COM"))).ToLowerInvariant());
        }

        [Fact]
        public void Dmk_DoesNotClaimTrsDos()
        {
            var Path = Sample("sample0.dmk");
            if (Path == null)
                return;

            Assert.False(DmkReader.TryDetect(File.ReadAllBytes(Path), Path, out _));
        }
        [Fact]
        public void Dmk_Loads_Via_Pipeline()
        {
            var Source = Sample("sample2.dmk");
            if (Source == null)
                return;

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_dmk_cpm.dmk");
            File.Copy(Source, Path, true);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new DmkCpmFilesystemPlugin());

                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Assert.True(Fs.Loaded);
                Assert.Equal("dmk-cpm", Fs.FilesystemFormat);
                Assert.NotNull(Fs.GetElement("GETMOR.OVL"));
                Assert.Equal(7424, Fs.GetElement("GETMOR.OVL").Size);
            }
            finally
            {
                File.Delete(Path);
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }
    }
}
