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
        public void CImg_Load_Resets_Previous_Workspace()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                return;

            var Boot = Path.Combine(Root, "vendor-raw", "boot.emmc");
            var Miner = Path.Combine(Root, "ramfs", "cimg", "minerfs.gz");
            if (!File.Exists(Boot) || !File.Exists(Miner))
                return;

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[]
            {
                "load \"" + Boot + "\"",
                "load \"" + Miner + "\""
            }));

            Assert.Null(Proc.GetKernel());
            Assert.Null(Proc.GetDevTree());
            Assert.NotNull(Proc.GetFs());
        }

        [Fact]
        public void CImg_Use_And_Import_Replace_Active_Kernel()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                return;

            var Source = Path.Combine(Root, "vendor-raw", "boot.emmc");
            if (!File.Exists(Source))
                return;

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[]
            {
                "load \"" + Source + "\"",
                "use all/cimg/fit/kernel",
                "import \"" + Source + "\" all/cimg/fit/kernel"
            }));

            Assert.NotNull(Proc.ActiveNode);
            Assert.NotNull(Proc.ActiveNode.Kernel);
            Assert.True(Proc.GetBlob().BuildPlan.Root.Dirty);
        }

        [Fact]
        public void Fit_MultiConfiguration_Repack()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                return;

            var Source = Path.Combine(Root, "fit", "ramboot.itb");
            if (!File.Exists(Source))
                return;

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[]
            {
                "load \"" + Source + "\"",
                "use fdt@cv1835_miner_emmc",
                "import \"" + Source + "\" all/fit/fdt@cv1835_miner_emmc"
            }));

            Assert.Equal(4, Proc.GetBlob().BuildPlan.Root.Configurations.Count);

            var Target = Path.Combine(Path.GetTempPath(), "nyafs-fit-multi-" + Guid.NewGuid().ToString("N") + ".itb");
            var Res = new Store().Get(new ScriptArgs(3, new[] { Target })).Exec(Proc);
            Assert.Equal(ScriptStepStatus.Ok, Res.Status);

            var ReadParser = new ScriptParser(new ScriptBaseAll());
            var ReadProc = new ImageProcessor(ReadParser, new string[0]);
            ReadProc.Process(ReadParser.ParseScript("t", new[] { "load \"" + Target + "\"" }));
            Assert.Equal(4, ReadProc.GetBlob().BuildPlan.Root.Configurations.Count);
        }

        [Fact]
        public void CImg_BuildPlan_Marks_Filesystem_Kind()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                return;

            var Source = Path.Combine(Root, "ramfs", "cimg", "minerfs.gz");
            if (!File.Exists(Source))
                return;

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            var Payload = Path.Combine(Path.GetTempPath(), "nyafs-plan-payload-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(Payload, "plan-dirty");

            Proc.Process(Parser.ParseScript("t", new[]
            {
                "load \"" + Source + "\"",
                "file etc/hostname \"" + Payload + "\""
            }));

            var Plan = Proc.GetBlob().BuildPlan;
            Assert.NotNull(Plan);
            Assert.True(Plan.Root.Dirty);
            Assert.True(Plan.FindKind(ImageKind.Filesystem).Dirty);
        }

        [Fact]
        public void CImg_Store_After_Edit_Rebuilds_Origin_Container()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                return;

            var Source = Path.Combine(Root, "vendor-raw", "boot.emmc");
            if (!File.Exists(Source))
                return;

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[]
            {
                "load \"" + Source + "\"",
                "set kernel entry 0x100090000"
            }));

            var Target = Path.Combine(Path.GetTempPath(), "nyafs-plan-edit-" + Guid.NewGuid().ToString("N") + ".emmc");
            var Res = new Store().Get(new ScriptArgs(3, new[] { Target })).Exec(Proc);
            Assert.Equal(ScriptStepStatus.Ok, Res.Status);

            var ReadParser = new ScriptParser(new ScriptBaseAll());
            var ReadProc = new ImageProcessor(ReadParser, new string[0]);
            ReadProc.Process(ReadParser.ParseScript("t", new[] { "load \"" + Target + "\"" }));
            Assert.Equal(0x100090000UL, ReadProc.GetKernel().Info.EntryPointAddress);
        }

        [Fact]
        public void CImg_Store_Without_Format_Uses_BuildPlan_Raw()
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

            Assert.NotNull(Proc.GetBlob().BuildPlan);
            Assert.NotNull(Proc.GetBlob().BuildPlan.Root.Raw);

            var Target = Path.Combine(Path.GetTempPath(), "nyafs-plan-" + Guid.NewGuid().ToString("N") + ".emmc");
            var Res = new Store().Get(new ScriptArgs(3, new[] { Target })).Exec(Proc);
            Assert.Equal(ScriptStepStatus.Ok, Res.Status);
            Assert.Equal(File.ReadAllBytes(Source), File.ReadAllBytes(Target));
        }

        [Fact]
        public void CImg_Inspector_Accepts_Logical_Paths()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                return;

            var Source = Path.Combine(Root, "vendor-raw", "boot.emmc");
            if (!File.Exists(Source))
                return;

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            var Inspector = new ImageInspector(Proc.Plugins);
            var Data = File.ReadAllBytes(Source);

            Assert.NotNull(Inspector.Locate(Data, "all/cimg/fit"));
            var Kernel = Inspector.Locate(Data, "all/cimg/fit/kernel");
            Assert.NotNull(Kernel.Info);
            Assert.Equal(NyaFs.ImageFormat.Types.CPU.IH_ARCH_ARM64, Kernel.Info.Architecture);
            Assert.NotNull(Inspector.Locate(Data, "all/cimg/fit/kernel"));
            Assert.NotNull(Inspector.Locate(Data, "all/cimg/fit/devtree"));
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
