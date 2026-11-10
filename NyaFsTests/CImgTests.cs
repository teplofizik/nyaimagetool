using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Composite;
using NyaFs.ImageFormat.Plugins.ImagePlugins.CompositeImagePlugins;
using NyaFs.ImageFormat.Types;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Commands;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class CImgTests
    {
        [Fact]
        public void CImg_Regenerate_Existing()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                return;

            var Files = new[]
            {
                Path.Combine(Root, "ramfs", "cimg", "minerfs.gz"),
                Path.Combine(Root, "vendor-raw", "boot.emmc")
            };

            foreach (var FileName in Files)
            {
                if (!File.Exists(FileName))
                    continue;

                var Source = new CvImage(File.ReadAllBytes(FileName));
                Assert.True(Source.Correct);

                var Rebuilt = new CImgBuilder
                {
                    Label = Source.ImageName,
                    ChunkSize = 16 * 1024 * 1024
                }.Build(Source.Content);

                var Check = new CvImage(Rebuilt);
                Assert.True(Check.Correct);
                Assert.Equal(Source.Content, Check.Content);
                Assert.Equal(Source.ImageName, Check.ImageName);
            }
        }

        [Fact]
        public void CImg_AllFit_RoundTrip()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                return;

            var Source = Path.Combine(Root, "vendor-raw", "boot.emmc");
            if (!File.Exists(Source))
                return;

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Source + "\"" }));

            Assert.Equal("kernel", Proc.GetBlob().GetContainerOption("cimg", "label"));

            var Target = Path.Combine(Path.GetTempPath(), "nyafs-cimg-all-" + Guid.NewGuid().ToString("N") + ".emmc");
            var Res = new Store().Get(new ScriptArgs(4, new[] { Target, "all", "cimg" })).Exec(Proc);
            Assert.Equal(ScriptStepStatus.Ok, Res.Status);

            var ReadParser = new ScriptParser(new ScriptBaseAll());
            var ReadProc = new ImageProcessor(ReadParser, new string[0]);
            ReadProc.Process(ReadParser.ParseScript("t", new[] { "load \"" + Target + "\"" }));

            Assert.Equal(Proc.GetKernel().Image, ReadProc.GetKernel().Image);
            Assert.Equal(
                new NyaFs.FlattenedDeviceTree.Writer.FDTWriter(Proc.GetDevTree().DevTree).GetBinary(),
                new NyaFs.FlattenedDeviceTree.Writer.FDTWriter(ReadProc.GetDevTree().DevTree).GetBinary());
            Assert.Equal("kernel", ReadProc.GetBlob().GetContainerOption("cimg", "label"));
        }

        [Fact]
        public void CImg_Build_RoundTrip()
        {
            var Payload = Encoding.ASCII.GetBytes(String.Concat(System.Linq.Enumerable.Repeat("abcde", 50)));
            var Builder = new CImgBuilder { Label = "TEST", ChunkSize = 64, Offset = 0x2000, PartSize = 0x40000 };
            var Image = Builder.Build(Payload);

            var Cimg = new CvImage(Image);
            Assert.True(Cimg.Correct);
            Assert.Equal(Payload, Cimg.Content);
            Assert.Equal(4u, Cimg.TotalChunk);

            var Plugin = new CImgImagePlugin();
            Assert.True(Plugin.DetectImage(new ImageData(Image)));
        }
    }
}
