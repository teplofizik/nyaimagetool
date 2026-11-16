using System;
using System.IO;
using System.Security.Cryptography;
using NyaFs.ImageFormat;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsTvip;
using Xunit;

namespace NyaFsTests
{
    public class TvipTests
    {
        private static string CorpusFile()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                Root = @"F:\TEMP\Images";
            var Path = System.IO.Path.Combine(Root, "vendor-raw", "tvip_firmware.bin");
            return File.Exists(Path) ? Path : null;
        }

        private static ScriptPlugins Plugins()
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Plugins.Load(new NyaFsTvipCompositeImagePlugin());
            return Proc.Plugins;
        }

        [Fact]
        public void Tvip_Cramfs_Wide_Inodes_Are_Detected()
        {
            var Source = CorpusFile();
            if (Source == null)
                return;

            var Tvip = new TvipImage(File.ReadAllBytes(Source));
            var Rootfs = Tvip.Find("rootfs");
            var Wide = new NyaFs.Filesystem.CramFs.CramFsReader(Tvip.GetData(Rootfs));
            Assert.True(Wide.WideInodes);
            Assert.Contains(Wide.ReadDir("/"), E => E.Path == "bin");

            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                Root = @"F:\TEMP\Images";
            var StandardPath = Path.Combine(Root, "ramfs", "cramfs", "test.cramfs");
            if (File.Exists(StandardPath))
            {
                var Standard = new NyaFs.Filesystem.CramFs.CramFsReader(File.ReadAllBytes(StandardPath));
                Assert.False(Standard.WideInodes);
            }
        }

        [Fact]
        public void Tvip_Rootfs_Edit_RoundTrips()
        {
            var Source = CorpusFile();
            if (Source == null)
                return;

            var Added = Path.Combine(Path.GetTempPath(), "tvip-added-" + Guid.NewGuid().ToString("N") + ".txt");
            var Target = Path.Combine(Path.GetTempPath(), "tvip-edit-" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllText(Added, "hello tvip");

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Plugins.Load(new NyaFsTvipCompositeImagePlugin());
            Proc.Process(Parser.ParseScript("t", new[]
            {
                $"load \"{Source}\"",
                $"file etc/tvip_test.txt \"{Added}\" rw-r--r-- 0 0",
                $"store \"{Target}\" all tvip"
            }));

            Assert.True(File.Exists(Target));
            var Original = new TvipImage(File.ReadAllBytes(Source));
            var Rebuilt = new TvipImage(File.ReadAllBytes(Target));
            Assert.True(Rebuilt.Correct);

            Assert.Equal(Original.Find("kernel").Md5, Rebuilt.Find("kernel").Md5);
            Assert.Equal(Original.Find("splash").Md5, Rebuilt.Find("splash").Md5);
            Assert.Equal(Original.Find("uboot").Size, Rebuilt.Find("uboot").Size);

            var Rootfs = Rebuilt.Find("rootfs");
            var RootfsData = Rebuilt.GetData(Rootfs);
            Assert.Equal(MD5.HashData(RootfsData), Rootfs.Md5);

            var ReadParser = new ScriptParser(new ScriptBaseAll());
            var ReadProc = new ImageProcessor(ReadParser, new string[0]);
            ReadProc.Plugins.Load(new NyaFsTvipCompositeImagePlugin());
            ReadProc.Process(ReadParser.ParseScript("t", new[] { $"load \"{Target}\"" }));
            Assert.NotNull(ReadProc.GetFs().GetElement("etc/tvip_test.txt"));
        }
        [Fact]
        public void Tvip_Sections_Are_Parsed()
        {
            var Source = CorpusFile();
            if (Source == null)
                return;

            var Image = new TvipImage(File.ReadAllBytes(Source));

            Assert.True(Image.Correct);
            Assert.Equal("s300", Image.Model);
            Assert.Equal("3.4.16", Image.Version);

            var Kernel = Image.Find("kernel");
            var Rootfs = Image.Find("rootfs");
            var Splash = Image.Find("splash");
            var Uboot = Image.Find("uboot");

            Assert.NotNull(Kernel);
            Assert.NotNull(Rootfs);
            Assert.NotNull(Splash);
            Assert.NotNull(Uboot);
            Assert.Equal(0x32a045u, Kernel.Size);
            Assert.Equal(0x2356000u, Rootfs.Size);
            Assert.Equal(0x2a3036u, Splash.Size);
            Assert.True(Uboot.Truncated);
            Assert.Equal(0x05000441u, Uboot.Size);
        }

        [Fact]
        public void Tvip_Is_Detected_And_Loaded()
        {
            var Source = CorpusFile();
            if (Source == null)
                return;

            var Data = File.ReadAllBytes(Source);
            var Reg = Plugins();

            var Composite = Reg.DetectComposite(new ImageData(Data));
            Assert.NotNull(Composite);
            Assert.Equal("tvip", Composite.Format);

            var Blob = new BaseImageBlob();
            Assert.True(new ImagePipeline(Reg, Blob).Load(Data));
            Assert.NotNull(Blob.GetKernel(0));
            Assert.True(Blob.GetKernel(0).Loaded);
            Assert.NotNull(Blob.GetFilesystem(0));
            Assert.True(Blob.GetFilesystem(0).Loaded);
            Assert.Contains(Blob.Unknown, U => U.Container.Contains("tvip/splash"));
            Assert.Contains(Blob.Unknown, U => U.Container.Contains("tvip/uboot"));
        }

        [Fact]
        public void Tvip_Inspector_Shows_Sections()
        {
            var Source = CorpusFile();
            if (Source == null)
                return;

            var Inspector = new ImageInspector(Plugins());
            var Root = Inspector.Inspect("tvip_firmware.bin", File.ReadAllBytes(Source));

            Assert.Equal("tvip", Root.Format);
            Assert.Contains(Root.Children, C => C.Name == "kernel" && C.Format == "legacy");
            Assert.Contains(Root.Children, C => C.Name == "kernel" && C.Children.Exists(K => K.Kind == ImageKind.Kernel));
            Assert.Contains(Root.Children, C => C.Name == "rootfs" && C.Kind == ImageKind.Filesystem);
            Assert.Contains(Root.Children, C => C.Name == "splash");
            Assert.Contains(Root.Children, C => C.Name == "uboot");
        }
    }
}
