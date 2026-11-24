using System;
using System.IO;
using System.Text;
using NyaFs.FlattenedDeviceTree;
using NyaFs.FlattenedDeviceTree.Types;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Composite;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsBmu;
using Xunit;

namespace NyaFsTests
{
    public class DropTests
    {
        private static byte[] BuildFit(byte[] Kernel, byte[] Dtb)
        {
            var Tree = new NyaFs.FlattenedDeviceTree.FlattenedDeviceTree();
            Tree.Root.AddStringValue("description", "drop test");

            var Images = new Node("images");
            Images.AddUInt32Value("#address-cells", 1);

            var KernelNode = new Node("kernel@1");
            KernelNode.AddStringValue("description", "kernel");
            KernelNode.AddRawValue("data", Kernel);
            KernelNode.AddStringValue("type", "kernel");
            KernelNode.AddStringValue("compression", "none");
            KernelNode.AddStringValue("arch", "arm");
            KernelNode.AddStringValue("os", "linux");
            Images.Nodes.Add(KernelNode);

            var FdtNode = new Node("fdt@1");
            FdtNode.AddStringValue("description", "devicetree");
            FdtNode.AddRawValue("data", Dtb);
            FdtNode.AddStringValue("type", "flat_dt");
            FdtNode.AddStringValue("compression", "none");
            Images.Nodes.Add(FdtNode);

            var Configurations = new Node("configurations");
            Configurations.AddStringValue("default", "config@1");
            var Config = new Node("config@1");
            Config.AddStringValue("kernel", "kernel@1");
            Config.AddStringValue("fdt", "fdt@1");
            Configurations.Nodes.Add(Config);

            Tree.Root.Nodes.Add(Images);
            Tree.Root.Nodes.Add(Configurations);
            return new NyaFs.FlattenedDeviceTree.Writer.FDTWriter(Tree).GetBinary();
        }

        private static byte[] BuildDtb()
        {
            var Tree = new NyaFs.FlattenedDeviceTree.FlattenedDeviceTree();
            Tree.Root.AddStringValue("compatible", "test");
            return new NyaFs.FlattenedDeviceTree.Writer.FDTWriter(Tree).GetBinary();
        }

        private static ImageProcessor Processor()
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            return new ImageProcessor(Parser, new string[0]);
        }

        private static string Temp(string Suffix) =>
            Path.Combine(Path.GetTempPath(), "nyafs-drop-" + Guid.NewGuid().ToString("N") + Suffix);

        [Fact]
        public void Drop_Fit_Ramdisk_Rebuilds_Without_It()
        {
            var Data = BuildFit(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, BuildDtb());
            var Source = Temp(".itb");
            var Target = Temp(".itb");
            File.WriteAllBytes(Source, Data);

            try
            {
                var Proc = Processor();
                Proc.Process(Proc.Parser.ParseScript("t", new[] { "load \"" + Source + "\"" }));

                Assert.NotNull(Proc.GetBlob().BuildPlan);
                Assert.True(Proc.GetBlob().BuildPlan.Root.DropChildren);
                Assert.NotNull(Proc.GetBlob().BuildPlan.Find("fdt@1"));

                Proc.Process(Proc.Parser.ParseScript("t2", new[]
                {
                    "drop fdt@1",
                    "store \"" + Target + "\""
                }));

                Assert.True(File.Exists(Target));

                var Read = Processor();
                Read.Process(Read.Parser.ParseScript("t3", new[] { "load \"" + Target + "\"" }));
                var Plan = Read.GetBlob().BuildPlan;
                Assert.NotNull(Plan);
                Assert.NotNull(Plan.Find("kernel@1"));
                Assert.Null(Plan.Find("fdt@1"));
            }
            finally
            {
                if (File.Exists(Source)) File.Delete(Source);
                if (File.Exists(Target)) File.Delete(Target);
            }
        }

        [Fact]
        public void Drop_Legacy_Multi_Part_Rebuilds_Without_It()
        {
            var Source = LegacyMultiImageWriter.BuildMulti(new[]
            {
                new byte[] { 1, 2, 3, 4, 5 },
                new byte[] { 6, 7, 8, 9 },
                new byte[] { 10, 11, 12 }
            });

            var SourcePath = Temp(".img");
            var Target = Temp(".img");
            File.WriteAllBytes(SourcePath, Source);

            try
            {
                var Proc = Processor();
                Proc.Process(Proc.Parser.ParseScript("t", new[] { "load \"" + SourcePath + "\"" }));

                var Plan = Proc.GetBlob().BuildPlan;
                Assert.NotNull(Plan);
                Assert.True(Plan.Root.DropChildren);
                Assert.Equal(3, Plan.Root.Children.Count);

                Proc.Process(Proc.Parser.ParseScript("t2", new[]
                {
                    "drop image2",
                    "store \"" + Target + "\""
                }));

                Assert.True(File.Exists(Target));

                var Read = Processor();
                Read.Process(Read.Parser.ParseScript("t3", new[] { "load \"" + Target + "\"" }));
                Assert.NotNull(Read.GetBlob().BuildPlan);
                Assert.Equal(2, Read.GetBlob().BuildPlan.Root.Children.Count);
            }
            finally
            {
                if (File.Exists(SourcePath)) File.Delete(SourcePath);
                if (File.Exists(Target)) File.Delete(Target);
            }
        }

        [Fact]
        public void Drop_Single_Legacy_Is_Not_Supported()
        {
            var Source = Temp(".img");
            File.WriteAllBytes(Source, LegacyMultiImageWriter.BuildScript(Encoding.ASCII.GetBytes("echo hi\n")));

            try
            {
                var Proc = Processor();
                Proc.Process(Proc.Parser.ParseScript("t", new[] { "load \"" + Source + "\"" }));

                var Plan = Proc.GetBlob().BuildPlan;
                Assert.NotNull(Plan);
                Assert.False(Plan.Root.DropChildren);
            }
            finally
            {
                if (File.Exists(Source)) File.Delete(Source);
            }
        }

        [Fact]
        public void Bmu_DropCapability_Is_Merge_Only()
        {
            var Plugin = new NyaFsBmuCompositeImagePlugin();

            var Merge = new byte[36];
            Merge[0] = 0xAB;
            Merge[1] = 0xAB;
            Merge[2] = 0xAB;
            Merge[3] = 0xAB;
            Merge[0x10] = 172;
            Assert.True(Plugin.CanDropChildren(new ImageData(Merge)));

            var Single = new byte[0x800];
            Single[0] = 0x26;
            Assert.False(Plugin.CanDropChildren(new ImageData(Single)));
        }

        private static byte[] BuildMergeBmu(out string FirstName, out string SecondName)
        {
            const int HeaderSize = 36;
            const int ItemSize = 172;
            const int Count = 2;
            var DataOffset = HeaderSize + Count * ItemSize;

            var First = Encoding.ASCII.GetBytes("A.bmu");
            var Second = Encoding.ASCII.GetBytes("B.bmu");
            var FirstData = new byte[] { 1, 2, 3, 4 };
            var SecondData = new byte[] { 5, 6, 7, 8, 9 };

            var Result = new byte[DataOffset + FirstData.Length + SecondData.Length];
            Result[0] = 0xAB; Result[1] = 0xAB; Result[2] = 0xAB; Result[3] = 0xAB;
            WriteU32(Result, 0x08, (uint)HeaderSize);
            WriteU32(Result, 0x0C, (uint)Count);
            WriteU32(Result, 0x10, (uint)ItemSize);
            WriteU32(Result, 0x14, (uint)DataOffset);

            var FirstBase = HeaderSize;
            Result[FirstBase + 0] = (byte)First.Length;
            Buffer.BlockCopy(First, 0, Result, FirstBase + 4, First.Length);
            WriteU32(Result, FirstBase + 0xA4, (uint)DataOffset);
            WriteU32(Result, FirstBase + 0xA8, (uint)FirstData.Length);

            var SecondBase = HeaderSize + ItemSize;
            Result[SecondBase + 0] = (byte)Second.Length;
            Buffer.BlockCopy(Second, 0, Result, SecondBase + 4, Second.Length);
            WriteU32(Result, SecondBase + 0xA4, (uint)(DataOffset + FirstData.Length));
            WriteU32(Result, SecondBase + 0xA8, (uint)SecondData.Length);

            Buffer.BlockCopy(FirstData, 0, Result, DataOffset, FirstData.Length);
            Buffer.BlockCopy(SecondData, 0, Result, DataOffset + FirstData.Length, SecondData.Length);

            FirstName = "A.bmu";
            SecondName = "B.bmu";
            return Result;
        }

        private static void WriteU32(byte[] Data, int Offset, uint Value)
        {
            Data[Offset] = (byte)Value;
            Data[Offset + 1] = (byte)(Value >> 8);
            Data[Offset + 2] = (byte)(Value >> 16);
            Data[Offset + 3] = (byte)(Value >> 24);
        }

        [Fact]
        public void Drop_Merge_Bmu_Item_Compacts_Table()
        {
            var Data = BuildMergeBmu(out var FirstName, out var SecondName);
            var Source = Temp(".bmu");
            var Target = Temp(".bmu");
            File.WriteAllBytes(Source, Data);

            try
            {
                var Proc = Processor();
                Proc.Plugins.Load(new NyaFsBmuCompositeImagePlugin());
                Proc.Process(Proc.Parser.ParseScript("t", new[] { "load \"" + Source + "\"" }));

                var Plan = Proc.GetBlob().BuildPlan;
                Assert.NotNull(Plan);
                Assert.True(Plan.Root.DropChildren);
                Assert.NotNull(Plan.Find(FirstName));

                Proc.Process(Proc.Parser.ParseScript("t2", new[]
                {
                    "drop \"" + FirstName + "\"",
                    "store \"" + Target + "\" all bmu"
                }));

                Assert.True(File.Exists(Target));
                var Rebuilt = File.ReadAllBytes(Target);
                Assert.Equal(1u, ReadU32(Rebuilt, 0x0C));
                Assert.Equal((uint)(36 + 172), ReadU32(Rebuilt, 0x14));

                var Read = Processor();
                Read.Plugins.Load(new NyaFsBmuCompositeImagePlugin());
                Read.Process(Read.Parser.ParseScript("t3", new[] { "load \"" + Target + "\"" }));
                Assert.NotNull(Read.GetBlob().BuildPlan);
                Assert.Null(Read.GetBlob().BuildPlan.Find(FirstName));
                Assert.NotNull(Read.GetBlob().BuildPlan.Find(SecondName));
            }
            finally
            {
                if (File.Exists(Source)) File.Delete(Source);
                if (File.Exists(Target)) File.Delete(Target);
            }
        }

        private static uint ReadU32(byte[] Data, int Offset) =>
            (uint)(Data[Offset] | (Data[Offset + 1] << 8) | (Data[Offset + 2] << 16) | (Data[Offset + 3] << 24));
    }
}
