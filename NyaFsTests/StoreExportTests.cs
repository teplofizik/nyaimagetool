using System;
using System.IO;
using System.Text;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Commands;
using Xunit;

namespace NyaFsTests
{
    /// <summary>
    /// Write-out path: the store/export commands must write an image to disk that the
    /// loader can read back to an equivalent filesystem.
    /// </summary>
    public class StoreExportTests
    {
        private static string TempDir()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "nyafs-store-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Dir);
            return Dir;
        }

        private static ImageProcessor ProcessorWithFs()
        {
            var Fs = new LinuxFilesystem();
            Fs.Info.Architecture = NyaFs.ImageFormat.Types.CPU.IH_ARCH_ARM;
            Fs.Info.OperatingSystem = NyaFs.ImageFormat.Types.OS.IH_OS_LINUX;
            Fs.Info.Type = NyaFs.ImageFormat.Types.ImageType.IH_TYPE_RAMDISK;
            Fs.Info.Compression = NyaFs.ImageFormat.Types.CompressionType.IH_COMP_NONE;
            Fs.FilesystemType = NyaFs.ImageFormat.Types.FsType.Cpio;
            Fs.Fs.Root.Items.Add(new NyaFs.Filesystem.Universal.Items.Dir("etc", 0, 0, 493));
            Fs.Fs.GetDirectory("etc").Items.Add(new NyaFs.Filesystem.Universal.Items.File("etc/hostname", 0, 0, 420, Encoding.ASCII.GetBytes("nyafs")));
            Fs.Fs.Root.Items.Add(new NyaFs.Filesystem.Universal.Items.SymLink("link", 0, 0, 511, "etc/hostname"));

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.SetFs(Fs);
            return Proc;
        }

        private static ScriptStepResult RunStore(ImageProcessor Proc, string Path, string Format) =>
            new Store().Get(new ScriptArgs(1, new[] { Path, "ramfs", Format })).Exec(Proc);

        private static LinuxFilesystem Reload(string Path)
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));
            return Proc.GetFs();
        }

        private static void AssertFs(LinuxFilesystem Fs)
        {
            Assert.NotNull(Fs);
            Assert.True(Fs.Loaded);
            Assert.Equal(Encoding.ASCII.GetBytes("nyafs"),
                ((NyaFs.Filesystem.Universal.Items.File)Fs.GetElement("etc/hostname")).Content);
            Assert.Equal("etc/hostname",
                ((NyaFs.Filesystem.Universal.Items.SymLink)Fs.GetElement("link")).Target);
        }

        [Theory]
        [InlineData("cpio")]
        [InlineData("squashfs")]
        [InlineData("gzip")]
        [InlineData("xz")]
        [InlineData("zstd")]
        [InlineData("tar")]
        [InlineData("tar.gz")]
        [InlineData("tar.xz")]
        [InlineData("tar.bz2")]
        [InlineData("tar.zstd")]
        public void Store_Ramfs_RoundTrip(string Format)
        {
            var Dir = TempDir();
            var Path = System.IO.Path.Combine(Dir, "out.bin");

            var Res = RunStore(ProcessorWithFs(), Path, Format);
            Assert.Equal(ScriptStepStatus.Ok, Res.Status);
            Assert.True(File.Exists(Path), $"store did not write {Format}");

            AssertFs(Reload(Path));
        }

        private static ImageProcessor McuProcessor()
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Plugins.Load(new NyaFsMcuFs.LittleFsFilesystemPlugin());
            Proc.Plugins.Load(new NyaFsMcuFs.SpiffsFilesystemPlugin());
            return Proc;
        }

        private static LinuxFilesystem McuFs()
        {
            var Fs = new LinuxFilesystem();
            Fs.FsSize = 1 << 20; // 1 MiB
            Fs.Fs.Root.Items.Add(new NyaFs.Filesystem.Universal.Items.File("etc/hostname", 0, 0, 420, Encoding.ASCII.GetBytes("nyafs")));
            return Fs;
        }

        [Theory]
        [InlineData("littlefs")]
        [InlineData("spiffs")]
        public void Store_Ramfs_McuFs_RoundTrip(string Format)
        {
            var Dir = TempDir();
            var Path = System.IO.Path.Combine(Dir, "out.bin");

            var Proc = McuProcessor();
            Proc.SetFs(McuFs());

            var Res = RunStore(Proc, Path, Format);
            Assert.Equal(ScriptStepStatus.Ok, Res.Status);
            Assert.True(File.Exists(Path), $"store did not write {Format}");

            // reload through the plugin
            var Proc2 = McuProcessor();
            Proc2.Process(Proc2.Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));
            var Fs = Proc2.GetFs();
            Assert.NotNull(Fs);
            Assert.True(Fs.Loaded);
            Assert.Equal(Encoding.ASCII.GetBytes("nyafs"),
                ((NyaFs.Filesystem.Universal.Items.File)Fs.GetElement("etc/hostname")).Content);
        }

        [Fact]
        public void Extract_Whole_File_Is_Byte_Identical()
        {
            var Dir = TempDir();
            var Src = System.IO.Path.Combine(Dir, "in.tar.xz");
            RunStore(ProcessorWithFs(), Src, "tar.xz");

            var Out = System.IO.Path.Combine(Dir, "out.bin");
            var Res = new Extract().Get(new ScriptArgs(1, new[] { Src, "", Out })).Exec(ProcessorWithFs());

            Assert.Equal(ScriptStepStatus.Ok, Res.Status);
            Assert.Equal(File.ReadAllBytes(Src), File.ReadAllBytes(Out));
        }

        [Fact]
        public void Extract_SubImage_From_Legacy_Matches_Data_Slice()
        {
            var Dir = TempDir();
            var LegacyPath = System.IO.Path.Combine(Dir, "legacy.img");

            Assert.Equal(ScriptStepStatus.Ok, RunStore(ProcessorWithFs(), LegacyPath, "legacy").Status);

            var Legacy = File.ReadAllBytes(LegacyPath);
            var Len = (int)(((uint)Legacy[0x0C] << 24) | ((uint)Legacy[0x0D] << 16) | ((uint)Legacy[0x0E] << 8) | Legacy[0x0F]);
            var Expected = new byte[Len];
            Array.Copy(Legacy, 0x40, Expected, 0, Len);

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            var Root = new NyaFs.ImageFormat.ImageInspector(Proc.Plugins).Inspect("legacy.img", Legacy);
            Assert.True(Root.Children.Count > 0, "legacy image has no sub-images");
            var Sub = Root.Children[0].Path;

            var Out = System.IO.Path.Combine(Dir, "sub.bin");
            var Res = new Extract().Get(new ScriptArgs(1, new[] { LegacyPath, Sub, Out })).Exec(Proc);

            Assert.Equal(ScriptStepStatus.Ok, Res.Status);
            Assert.Equal(Expected, File.ReadAllBytes(Out));
        }

        [Fact]
        public void Export_Writes_Files_To_Dir()
        {
            var Dir = TempDir();
            var Out = System.IO.Path.Combine(Dir, "exported");

            var Res = new Export().Get(new ScriptArgs(1, new[] { Out })).Exec(ProcessorWithFs());
            Assert.Equal(ScriptStepStatus.Ok, Res.Status);

            var Hostname = System.IO.Path.Combine(Out, "etc", "hostname");
            Assert.True(File.Exists(Hostname));
            Assert.Equal("nyafs", File.ReadAllText(Hostname));
        }
    }
}
