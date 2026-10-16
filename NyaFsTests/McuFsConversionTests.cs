using System;
using System.IO;
using NyaFs.Filesystem.Universal;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Elements.Fs.Writer;
using NyaFs.ImageFormat.Types;
using NyaFsMcuFs;
using Xunit;

namespace NyaFsTests
{
    /// <summary>
    /// End-to-end conversion between the MCU filesystems, relying on LinuxFilesystem.FsSize
    /// (the geometry carried over from the source image).
    /// </summary>
    public class McuFsConversionTests
    {
        private static string Corpus(string Rel)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\Images";
            var Path = System.IO.Path.Combine(Root, Rel.Replace('/', System.IO.Path.DirectorySeparatorChar));
            return File.Exists(Path) ? Path : null;
        }

        private static LinuxFilesystem Load(FsType Type, IFilesystemReader Reader, long Size)
        {
            var Fs = new LinuxFilesystem();
            new BaseFsReader(Type, Reader, Size).ReadToFs(Fs);
            return Fs;
        }

        private static byte[] Store(IFilesystemBuilder Builder, LinuxFilesystem Fs)
        {
            var W = new BaseFsWriter(Builder);
            W.WriteFs(Fs);
            return W.RawStream;
        }

        private static void AssertFile(LinuxFilesystem Fs, string Path, byte[] Expected)
        {
            var Item = Fs.GetElement(Path) as NyaFs.Filesystem.Universal.Items.File;
            Assert.NotNull(Item);
            Assert.Equal(Expected, Item.Content);
        }

        [Fact]
        public void Spiffs_To_LittleFs()
        {
            var Path = Corpus("mcu/test.spiffs");
            if (Path == null) return; // no corpus, skip

            var Raw = File.ReadAllBytes(Path);
            var Src = Load(FsType.Spiffs, new SpiffsReader(Raw), Raw.Length);
            Assert.Equal(Raw.Length, Src.FsSize);

            var Sys = ((NyaFs.Filesystem.Universal.Items.File)Src.GetElement("config/system.yaml")).Content;
            var Readme = ((NyaFs.Filesystem.Universal.Items.File)Src.GetElement("readme.txt")).Content;
            var Index = ((NyaFs.Filesystem.Universal.Items.File)Src.GetElement("web/index.html")).Content;

            const int BlockSize = 4096;
            var Image = Store(new LittleFsBuilder(BlockSize, Src.FsSize / BlockSize), Src);

            var Dst = Load(FsType.LittleFs, new LittleFsReader(Image), Image.Length);
            AssertFile(Dst, "config/system.yaml", Sys);
            AssertFile(Dst, "readme.txt", Readme);
            AssertFile(Dst, "web/index.html", Index);
        }

        [Fact]
        public void LittleFs_To_Spiffs()
        {
            var Path = Corpus("mcu/littlefs.bin");
            if (Path == null) return; // no corpus, skip

            var Raw = File.ReadAllBytes(Path);
            var Src = Load(FsType.LittleFs, new LittleFsReader(Raw), Raw.Length);
            Assert.True(Src.FsSize > 0);

            var Sys = ((NyaFs.Filesystem.Universal.Items.File)Src.GetElement("config/system.yaml")).Content;
            var Index = ((NyaFs.Filesystem.Universal.Items.File)Src.GetElement("web/index.html")).Content;

            var Image = Store(new SpiffsBuilder(Src.FsSize), Src);

            var Dst = Load(FsType.Spiffs, new SpiffsReader(Image), Image.Length);
            AssertFile(Dst, "config/system.yaml", Sys);
            AssertFile(Dst, "web/index.html", Index);
        }
    }
}
