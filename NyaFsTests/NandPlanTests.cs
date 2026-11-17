using System;
using System.IO;
using System.Linq;
using System.Text;
using NyaFs.ImageFormat;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsNand;
using Xunit;

namespace NyaFsTests
{
    public class NandPlanTests
    {
        private const int Page = 2048;
        private const int Block = 0x20000;

        private static string Fixture(string Name) =>
            Path.Combine(AppContext.BaseDirectory, "Fixtures", Name);

        private static byte[] Wrap(byte[] Logical, int PageSize, int OobSize)
        {
            var Pages = Logical.Length / PageSize;
            var Result = new byte[Pages * (PageSize + OobSize)];
            for (int I = 0; I < Pages; I++)
            {
                Array.Copy(Logical, I * PageSize, Result, I * (PageSize + OobSize), PageSize);
                for (int K = 0; K < OobSize; K++)
                    Result[I * (PageSize + OobSize) + PageSize + K] = 0xFF;
            }
            return Result;
        }

        private static ImageProcessor Processor()
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Plugins.Load(new NyaFsNandCompositeImagePlugin());
            return Proc;
        }

        private static void Run(ImageProcessor Proc, params string[] Lines) =>
            Proc.Process(new ScriptParser(new ScriptBaseAll()).ParseScript("t", Lines));

        [Fact]
        public void Plan_Empty_Layout_Produces_Erased_Image()
        {
            var Layout = Path.Combine(Path.GetTempPath(), "nand-layout-" + Guid.NewGuid().ToString("N") + ".yaml");
            var Out = Path.Combine(Path.GetTempPath(), "nand-plan-" + Guid.NewGuid().ToString("N") + ".bin");

            try
            {
                File.WriteAllText(Layout, "schema: 1\nformat: nand\n" +
                    "options: { page: 2048, oob: 128, block: 131072, blocks: 16 }\n");

                var Proc = Processor();
                Run(Proc,
                    "mkimage nand \"" + Layout + "\"",
                    "store \"" + Out + "\" all nand");

                Assert.True(File.Exists(Out));
                var Data = File.ReadAllBytes(Out);
                Assert.Equal(16 * Block, Data.Length);
                Assert.All(Data, B => Assert.Equal(0xFF, B));
            }
            finally
            {
                if (File.Exists(Layout)) File.Delete(Layout);
                if (File.Exists(Out)) File.Delete(Out);
            }
        }

        [Fact]
        public void Plan_Raw_Slots_Import_And_Store()
        {
            var Layout = Path.Combine(Path.GetTempPath(), "nand-layout-" + Guid.NewGuid().ToString("N") + ".yaml");
            var Blob = Path.Combine(Path.GetTempPath(), "nand-blob-" + Guid.NewGuid().ToString("N") + ".bin");
            var Out = Path.Combine(Path.GetTempPath(), "nand-plan-" + Guid.NewGuid().ToString("N") + ".bin");

            var Payload = Encoding.ASCII.GetBytes("bootloader payload");
            var Payload2 = Encoding.ASCII.GetBytes("second payload");

            try
            {
                File.WriteAllBytes(Blob + "1", Payload);
                File.WriteAllBytes(Blob + "2", Payload2);

                File.WriteAllText(Layout,
                    "schema: 1\nformat: nand\n" +
                    "options: { page: 2048, oob: 0, block: 131072, blocks: 8 }\n" +
                    "children:\n" +
                    "  - { name: boot, offset: 0x0, size: 0x20000, kind: raw }\n" +
                    "  - { name: data, offset: 0x20000, size: 0x40000, kind: raw }\n");

                var Proc = Processor();
                Run(Proc,
                    "mkimage nand \"" + Layout + "\"",
                    "use all/boot",
                    "import \"" + Blob + "1\" .",
                    "use all/data",
                    "import \"" + Blob + "2\" .",
                    "store \"" + Out + "\" all nand");

                Assert.True(File.Exists(Out));
                var Data = File.ReadAllBytes(Out);
                Assert.Equal(8 * Block, Data.Length);

                Assert.Equal(Payload, Data.Take(Payload.Length).ToArray());
                Assert.Equal(0xFF, Data[Block - 1]);
                Assert.Equal(Payload2, Data.Skip(Block).Take(Payload2.Length).ToArray());
                Assert.Equal(0xFF, Data[2 * Block - 1]);
                Assert.All(Data.Skip(3 * Block), B => Assert.Equal(0xFF, B));
            }
            finally
            {
                if (File.Exists(Layout)) File.Delete(Layout);
                if (File.Exists(Blob + "1")) File.Delete(Blob + "1");
                if (File.Exists(Blob + "2")) File.Delete(Blob + "2");
                if (File.Exists(Out)) File.Delete(Out);
            }
        }

        [Fact]
        public void Plan_Filesystem_Slot_Is_Built_From_Active_Node()
        {
            var Layout = Path.Combine(Path.GetTempPath(), "nand-layout-" + Guid.NewGuid().ToString("N") + ".yaml");
            var File1 = Path.Combine(Path.GetTempPath(), "nand-file-" + Guid.NewGuid().ToString("N") + ".txt");
            var Out = Path.Combine(Path.GetTempPath(), "nand-plan-" + Guid.NewGuid().ToString("N") + ".bin");

            try
            {
                File.WriteAllText(File1, "hello from the plan");

                File.WriteAllText(Layout,
                    "schema: 1\nformat: nand\n" +
                    "options: { page: 2048, oob: 0, block: 131072, blocks: 32 }\n" +
                    "children:\n" +
                    "  - { name: config, kind: filesystem, format: ubifs, size: auto }\n");

                var Proc = Processor();
                Run(Proc,
                    "mkimage nand \"" + Layout + "\"",
                    "use all/config",
                    "mkfs",
                    "dir /etc 755 0 0",
                    "file /etc/hello.txt \"" + File1 + "\" 644 0 0",
                    "store \"" + Out + "\" all nand");

                Assert.True(File.Exists(Out));
                var Data = File.ReadAllBytes(Out);
                Assert.Equal(32 * Block, Data.Length);
                Assert.Equal(Encoding.ASCII.GetBytes("UBI#"), Data.Take(4).ToArray());
                Assert.All(Data.Skip(32 * Block - 1), B => Assert.Equal(0xFF, B));
            }
            finally
            {
                if (File.Exists(Layout)) File.Delete(Layout);
                if (File.Exists(File1)) File.Delete(File1);
                if (File.Exists(Out)) File.Delete(Out);
            }
        }

        [Fact]
        public void Plan_Layout_Export_Then_MkImage()
        {
            var Ubi = File.ReadAllBytes(Fixture("ubifs_sample.ubi"));
            var Raw = Wrap(Ubi, Page, 64);

            var Dump = Path.Combine(Path.GetTempPath(), "nand-dump-" + Guid.NewGuid().ToString("N") + ".bin");
            var Layout = Path.Combine(Path.GetTempPath(), "nand-layout-" + Guid.NewGuid().ToString("N") + ".yaml");
            var Out = Path.Combine(Path.GetTempPath(), "nand-plan-" + Guid.NewGuid().ToString("N") + ".bin");

            try
            {
                File.WriteAllBytes(Dump, Raw);

                var Proc = Processor();
                Run(Proc,
                    "layout export \"" + Dump + "\" \"" + Layout + "\"",
                    "mkimage nand \"" + Layout + "\"",
                    "store \"" + Out + "\" all nand");

                Assert.True(File.Exists(Layout));
                var Yaml = File.ReadAllText(Layout);
                Assert.Contains("format: 'nand'", Yaml);
                Assert.Contains("name: 'ubi@00000000'", Yaml);
                Assert.Contains("block: 131072", Yaml);
                Assert.Contains("immutable: true", Yaml);

                Assert.True(File.Exists(Out));
                var Data = File.ReadAllBytes(Out);
                Assert.Equal(Ubi.Length, Data.Length);
                Assert.Equal(Ubi, Data);
            }
            finally
            {
                if (File.Exists(Dump)) File.Delete(Dump);
                if (File.Exists(Layout)) File.Delete(Layout);
                if (File.Exists(Out)) File.Delete(Out);
                var Blobs = Layout + ".blobs";
                if (Directory.Exists(Blobs)) Directory.Delete(Blobs, true);
            }
        }
    }
}
