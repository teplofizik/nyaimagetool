using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Composite;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.ImageFormat.Elements.Kernel;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class FitMultiConfigTests
    {
        private static ImageProcessor Load(string Path)
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));
            return Proc;
        }

        private static ImageBuildNode Node(ImageBuildNode Root, string Name, ImageKind Kind, object Element, string Format)
        {
            var Node = new ImageBuildNode
            {
                Path = Name,
                Name = Name,
                Kind = Kind,
                Format = Format,
                Dirty = true
            };

            switch (Kind)
            {
                case ImageKind.Kernel:
                    Node.Kernel = (LinuxKernel)Element;
                    Node.Info = Node.Kernel.Info;
                    Node.Compression = Node.Kernel.Info.Compression;
                    break;
                case ImageKind.Filesystem:
                    Node.Fs = (LinuxFilesystem)Element;
                    Node.Info = Node.Fs.Info;
                    Node.Compression = Node.Fs.Info.Compression;
                    break;
                case ImageKind.Devtree:
                    Node.Dtb = (NyaFs.ImageFormat.Elements.Dtb.DeviceTree)Element;
                    Node.Info = Node.Dtb.Info;
                    Node.Compression = Node.Dtb.Info.Compression;
                    break;
            }

            Root.Children.Add(Node);
            return Node;
        }

        [Fact]
        public void Fit_Signature_Nodes_Are_Detected()
        {
            var RootDir = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(RootDir))
                return;

            var Source = System.IO.Path.Combine(RootDir, "fit", "ramboot.itb");
            if (!File.Exists(Source))
                return;

            var Fit = new NyaFs.FlattenedDeviceTree.Reader.FDTReader(File.ReadAllBytes(Source)).Read();
            var Kernel = Fit.Get("images/kernel@1");
            var Signature = new NyaFs.FlattenedDeviceTree.Types.Node("signature-1");
            Signature.AddStringValue("algo", "sha256,rsa2048");
            Kernel.Nodes.Add(Signature);

            var Target = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs-fit-signed-" + Guid.NewGuid().ToString("N") + ".itb");
            File.WriteAllBytes(Target, new NyaFs.FlattenedDeviceTree.Writer.FDTWriter(Fit).GetBinary());

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Target + "\"" }));

            Assert.Contains("signatures", Proc.GetBlob().BuildPlan.Root.Options.Keys);
        }

        [Fact]
        public void Fit_External_Data_Is_Read()
        {
            var RootDir = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(RootDir))
                return;

            var Source = System.IO.Path.Combine(RootDir, "fit", "ramboot.itb");
            if (!File.Exists(Source))
                return;

            var Original = File.ReadAllBytes(Source);
            var Fit = new NyaFs.FlattenedDeviceTree.Reader.FDTReader(Original).Read();
            var Kernel = Fit.Get("images/kernel@1");
            var Data = Kernel.GetValue("data");
            if (Data == null)
                return;

            Kernel.Properties.RemoveAll(P => P.Name == "data");
            Kernel.AddUInt32Value("data-offset", 0);
            Kernel.AddUInt32Value("data-size", (uint)Data.Length);

            var Fdt = new NyaFs.FlattenedDeviceTree.Writer.FDTWriter(Fit).GetBinary();
            var Base = (Fdt.Length + 3) & ~3;
            var External = new byte[Base + Data.Length];
            Array.Copy(Fdt, External, Fdt.Length);
            Array.Copy(Data, 0, External, Base, Data.Length);

            var Target = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs-fit-external-" + Guid.NewGuid().ToString("N") + ".itb");
            File.WriteAllBytes(Target, External);

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Target + "\"" }));

            Assert.NotNull(Proc.GetKernel());
            Assert.True(Proc.GetKernel().Loaded);
        }

        [Fact]
        public void Fit_Hash_Algorithm_Option()
        {
            var RootDir = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(RootDir))
                return;

            var Source = System.IO.Path.Combine(RootDir, "fit", "ramboot.itb");
            if (!File.Exists(Source))
                return;

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[]
            {
                "load \"" + Source + "\"",
                "set container fit.hash sha512",
                "use fdt@cv1835_miner_emmc",
                "import \"" + Source + "\" all/fit/fdt@cv1835_miner_emmc"
            }));

            var Target = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs-fit-hash-" + Guid.NewGuid().ToString("N") + ".itb");
            var Res = new NyaFs.Processor.Scripting.Commands.Store().Get(new NyaFs.Processor.Scripting.ScriptArgs(3, new[] { Target })).Exec(Proc);
            Assert.Equal(ScriptStepStatus.Ok, Res.Status);

            var Fit = new NyaFs.FlattenedDeviceTree.Reader.FDTReader(File.ReadAllBytes(Target)).Read();
            var Kernel = Fit.Get("images/kernel@1");
            Assert.Contains(Kernel.Nodes, H => H.GetStringValue("algo") == "sha512");
        }

        [Fact]
        public void Fit_Source_Export_Uses_Incbin_Blobs()
        {
            var RootDir = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(RootDir))
                return;

            var Source = System.IO.Path.Combine(RootDir, "fit", "ramboot.itb");
            if (!File.Exists(Source))
                return;

            var OutDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs-fit-its-" + Guid.NewGuid().ToString("N"));
            string Error;
            Assert.True(new NyaFsFit.FitSourceExporter().Export(Source, OutDir, out Error), Error);

            var Its = System.IO.Path.Combine(OutDir, "ramboot.its");
            Assert.True(File.Exists(Its));
            var Text = File.ReadAllText(Its);
            Assert.Contains("/incbin/", Text);
            Assert.Contains("configurations", Text);
            Assert.True(File.Exists(System.IO.Path.Combine(OutDir, "blobs", "kernel_1.bin")));
            Assert.True(new FileInfo(System.IO.Path.Combine(OutDir, "blobs", "kernel_1.bin")).Length > 0);

            var Rebuilt = System.IO.Path.Combine(OutDir, "rebuilt.fit");
            Assert.True(new NyaFsFit.FitSourceImporter().Import(Its, Rebuilt, out Error), Error);
            var Read = Load(Rebuilt);
            Assert.Equal(4, Read.GetBlob().BuildPlan.Root.Configurations.Count);
        }

        [Fact]
        public void Fit_MultiConfig_Builds_From_Different_Images()
        {
            var RootDir = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(RootDir))
                return;

            var BootPath = System.IO.Path.Combine(RootDir, "vendor-raw", "boot.emmc");
            var MinerPath = System.IO.Path.Combine(RootDir, "ramfs", "cimg", "minerfs.gz");
            var OverlayPath = System.IO.Path.Combine(RootDir, "ramfs", "cpio", "overlay.gz");
            var RamBootPath = System.IO.Path.Combine(RootDir, "fit", "ramboot.itb");
            if (!File.Exists(BootPath) || !File.Exists(MinerPath) || !File.Exists(OverlayPath) || !File.Exists(RamBootPath))
                return;

            var Boot = Load(BootPath);
            var Miner = Load(MinerPath);
            var Overlay = Load(OverlayPath);
            var RamBoot = Load(RamBootPath);

            var KernelA = Boot.GetKernel();
            var KernelB = RamBoot.GetKernel();
            var FsA = Miner.GetFs();
            var FsB = Overlay.GetFs();
            var DtbA = Boot.GetDevTree();
            if ((KernelA == null) || (KernelB == null) || (FsA == null) || (FsB == null) || (DtbA == null))
                return;

            foreach (var Fs in new[] { FsA, FsB })
            {
                Fs.Info.Architecture = NyaFs.ImageFormat.Types.CPU.IH_ARCH_ARM64;
                Fs.Info.OperatingSystem = NyaFs.ImageFormat.Types.OS.IH_OS_LINUX;
                Fs.Info.Type = NyaFs.ImageFormat.Types.ImageType.IH_TYPE_RAMDISK;
            }

            var Root = new ImageBuildNode
            {
                Name = "fit",
                Format = "fit",
                Kind = ImageKind.Unknown,
                DefaultConfiguration = "config@a"
            };

            Node(Root, "kernel@a", ImageKind.Kernel, KernelA, KernelA.KernelFormat ?? "raw");
            Node(Root, "kernel@b", ImageKind.Kernel, KernelB, KernelB.KernelFormat ?? "raw");
            Node(Root, "ramdisk@a", ImageKind.Filesystem, FsA, FsA.FilesystemFormat ?? "cpio");
            Node(Root, "ramdisk@b", ImageKind.Filesystem, FsB, FsB.FilesystemFormat ?? "cpio");
            Node(Root, "fdt@a", ImageKind.Devtree, DtbA, DtbA.DevtreeFormat ?? "dtb");
            Node(Root, "fdt@b", ImageKind.Devtree, DtbA, DtbA.DevtreeFormat ?? "dtb");

            var Firmware = new ImageBuildNode
            {
                Path = "firmware@1",
                Name = "firmware@1",
                Kind = ImageKind.Unknown,
                Format = "raw",
                Compression = NyaFs.ImageFormat.Types.CompressionType.IH_COMP_NONE,
                Raw = Encoding.ASCII.GetBytes("firmware"),
                Info = new NyaFs.ImageFormat.Types.ImageInfo
                {
                    Type = NyaFs.ImageFormat.Types.ImageType.IH_TYPE_FIRMWARE
                }
            };
            Root.Children.Add(Firmware);

            Root.Configurations.Add(new ImageContainerConfiguration
            {
                Name = "config@a", Description = "A", Kernel = "kernel@a", Ramdisk = "ramdisk@a", Fdt = "fdt@a"
            });
            Root.Configurations.Add(new ImageContainerConfiguration
            {
                Name = "config@b", Description = "B", Kernel = "kernel@b", Ramdisk = "ramdisk@b", Fdt = "fdt@b", Setup = "firmware@1"
            });

            var Writer = new ImageBuildPlanWriter(new ImageBuildPlan(Root));
            string Error;
            var Data = Writer.Build(out Error);
            Assert.NotNull(Data);

            var Target = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs-fit-multi-build-" + Guid.NewGuid().ToString("N") + ".itb");
            File.WriteAllBytes(Target, Data);

            var Read = Load(Target);
            Assert.NotNull(Read.GetBlob().BuildPlan);
            Assert.Equal(2, Read.GetBlob().BuildPlan.Root.Configurations.Count);
            Assert.Contains(Read.GetBlob().BuildPlan.Root.Children, C => C.Name == "firmware@1");
            Assert.Equal("firmware@1", Read.GetBlob().BuildPlan.Root.Configurations.Find(C => C.Name == "config@b").Setup);
        }
    }
}
