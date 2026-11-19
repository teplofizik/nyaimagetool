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

        [Fact]
        public void Plan_Build_Command_With_File_And_Dir_Content()
        {
            var Root = Path.Combine(Path.GetTempPath(), "nand-src-" + Guid.NewGuid().ToString("N"));
            var Layout = Path.Combine(Path.GetTempPath(), "nand-layout-" + Guid.NewGuid().ToString("N") + ".yaml");
            var Blob = Path.Combine(Root, "boot.bin");
            var Out = Path.Combine(Path.GetTempPath(), "nand-plan-" + Guid.NewGuid().ToString("N") + ".bin");
            var Payload = Encoding.ASCII.GetBytes("declarative bootloader");

            try
            {
                Directory.CreateDirectory(Path.Combine(Root, "config", "etc"));
                File.WriteAllBytes(Blob, Payload);
                File.WriteAllText(Path.Combine(Root, "config", "etc", "serial"), "SN12345");

                File.WriteAllText(Layout,
                    "schema: 1\nformat: nand\n" +
                    "options: { page: 2048, oob: 0, block: 131072, blocks: 32 }\n" +
                    "children:\n" +
                    "  - { name: boot, offset: 0x0, size: 0x20000, kind: raw, content: { file: '" + Blob + "' } }\n" +
                    "  - { name: config, offset: 0x20000, size: auto, kind: filesystem, format: ubifs," +
                    " content: { dir: '" + Path.Combine(Root, "config") + "' } }\n");

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                Assert.True(File.Exists(Out));
                var Data = File.ReadAllBytes(Out);
                Assert.Equal(32 * Block, Data.Length);
                Assert.Equal(Payload, Data.Take(Payload.Length).ToArray());
                Assert.Equal(Encoding.ASCII.GetBytes("UBI#"), Data.Skip(Block).Take(4).ToArray());
            }
            finally
            {
                if (Directory.Exists(Root)) Directory.Delete(Root, true);
                if (File.Exists(Layout)) File.Delete(Layout);
                if (File.Exists(Out)) File.Delete(Out);
            }
        }

        [Fact]
        public void Plan_Build_Streams_Ext4_Filesystem_Slot()
        {
            var Root = Path.Combine(Path.GetTempPath(), "nand-ext4-" + Guid.NewGuid().ToString("N"));
            var Layout = Path.Combine(Path.GetTempPath(), "nand-layout-" + Guid.NewGuid().ToString("N") + ".yaml");
            var Out = Path.Combine(Path.GetTempPath(), "nand-plan-" + Guid.NewGuid().ToString("N") + ".bin");
            var Content = Encoding.UTF8.GetBytes("hello streamed ext4");

            try
            {
                Directory.CreateDirectory(Path.Combine(Root, "etc"));
                File.WriteAllBytes(Path.Combine(Root, "etc", "hello"), Content);

                File.WriteAllText(Layout,
                    "schema: 1\nformat: nand\n" +
                    "options: { page: 2048, oob: 0, block: 131072, blocks: 128 }\n" +
                    "children:\n" +
                    "  - { name: rootfs, offset: 0, size: auto, kind: filesystem, format: ext4," +
                    " content: { dir: '" + Root + "' } }\n");

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                var Data = File.ReadAllBytes(Out);
                Assert.Equal(0x53, Data[0x438]);
                Assert.Equal(0xEF, Data[0x439]);

                var Reader = new NyaFs.Filesystem.Ext2.Ext4FsReader(Data);
                Assert.Equal(Content, Reader.Read("/etc/hello"));
            }
            finally
            {
                if (Directory.Exists(Root)) Directory.Delete(Root, true);
                if (File.Exists(Layout)) File.Delete(Layout);
                if (File.Exists(Out)) File.Delete(Out);
            }
        }

        [Fact]
        public void Plan_Build_Streams_Ubi_Container_Slot()
        {
            var Root = Path.Combine(Path.GetTempPath(), "nand-ubi-" + Guid.NewGuid().ToString("N"));
            var Layout = Path.Combine(Path.GetTempPath(), "nand-layout-" + Guid.NewGuid().ToString("N") + ".yaml");
            var Out = Path.Combine(Path.GetTempPath(), "nand-plan-" + Guid.NewGuid().ToString("N") + ".bin");
            var Kernel = Path.Combine(Root, "kernel.bin");
            var KernelData = Encoding.ASCII.GetBytes("static kernel volume");

            try
            {
                Directory.CreateDirectory(Path.Combine(Root, "rootfs", "etc"));
                File.WriteAllText(Path.Combine(Root, "rootfs", "etc", "hello"), "hello nested ubi");
                File.WriteAllBytes(Kernel, KernelData);

                File.WriteAllText(Layout,
                    "schema: 1\nformat: nand\n" +
                    "options: { page: 2048, oob: 0, block: 131072, blocks: 64 }\n" +
                    "children:\n" +
                    "  - name: os\n" +
                    "    offset: 0\n" +
                    "    size: auto\n" +
                    "    kind: container\n" +
                    "    format: ubi\n" +
                    "    options: { peb: 131072, page: 2048 }\n" +
                    "    children:\n" +
                    "      - { name: rootfs, kind: filesystem, format: ubifs, content: { dir: '" + Path.Combine(Root, "rootfs") + "' } }\n" +
                    "      - { name: kernel, kind: raw, content: { file: '" + Kernel + "' } }\n");

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                var Data = File.ReadAllBytes(Out);
                Assert.Equal(Encoding.ASCII.GetBytes("UBI#"), Data.Take(4).ToArray());

                var Ubi = new NyaFs.Filesystem.UbiFs.UbiFsReader(Data);
                Assert.Equal(2, Ubi.Volumes.Count);

                var Rootfs = Ubi.FindVolume("rootfs");
                Assert.NotNull(Rootfs);
                var Reader = new NyaFs.Filesystem.UbiFs.Ubifs.UbifsReader(Ubi, Rootfs);
                Assert.Equal(Encoding.UTF8.GetBytes("hello nested ubi"), Reader.Read("/etc/hello"));

                var KernelVol = Ubi.Volumes.Find(V => V.Name == "kernel");
                Assert.NotNull(KernelVol);
                var Leb = Ubi.ReadLeb(KernelVol.Id, 0);
                Assert.NotNull(Leb);
                Assert.Equal(KernelData, Leb.Take(KernelData.Length).ToArray());
            }
            finally
            {
                if (Directory.Exists(Root)) Directory.Delete(Root, true);
                if (File.Exists(Layout)) File.Delete(Layout);
                if (File.Exists(Out)) File.Delete(Out);
            }
        }

        [Fact]
        public void Plan_Build_Command_Resolves_Blob_Content()
        {
            var Layout = Path.Combine(Path.GetTempPath(), "nand-layout-" + Guid.NewGuid().ToString("N") + ".yaml");
            var Out = Path.Combine(Path.GetTempPath(), "nand-plan-" + Guid.NewGuid().ToString("N") + ".bin");
            var Payload = Encoding.ASCII.GetBytes("cached immutable payload");
            var Sha = Sha256Hex(Payload);

            try
            {
                var Blobs = Layout + ".blobs";
                Directory.CreateDirectory(Blobs);
                File.WriteAllBytes(Path.Combine(Blobs, Sha), Payload);

                File.WriteAllText(Layout,
                    "schema: 1\nformat: nand\n" +
                    "options: { page: 2048, oob: 128, block: 131072, blocks: 4 }\n" +
                    "children:\n" +
                    "  - { name: boot, offset: 0x0, size: 0x20000, kind: raw," +
                    " content: { blob: 'sha256:" + Sha + "' } }\n");

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                Assert.True(File.Exists(Out));
                var Data = File.ReadAllBytes(Out);
                Assert.Equal(4 * Block, Data.Length);
                Assert.Equal(Payload, Data.Take(Payload.Length).ToArray());
            }
            finally
            {
                if (File.Exists(Layout)) File.Delete(Layout);
                if (File.Exists(Out)) File.Delete(Out);
                var Blobs = Layout + ".blobs";
                if (Directory.Exists(Blobs)) Directory.Delete(Blobs, true);
            }
        }

        private static string Sha256Hex(byte[] Data)
        {
            using (var Sha = System.Security.Cryptography.SHA256.Create())
                return String.Concat(Sha.ComputeHash(Data).Select(B => B.ToString("x2")));
        }

        [Fact]
        public void Plan_Build_Fit_Container_From_Files()
        {
            var Root = Path.Combine(Path.GetTempPath(), "nand-fit-" + Guid.NewGuid().ToString("N"));
            var Layout = Path.Combine(Path.GetTempPath(), "nand-layout-" + Guid.NewGuid().ToString("N") + ".yaml");
            var Out = Path.Combine(Path.GetTempPath(), "nand-plan-" + Guid.NewGuid().ToString("N") + ".bin");
            var Kernel = Path.Combine(Root, "kernel.bin");

            try
            {
                Directory.CreateDirectory(Path.Combine(Root, "ramdisk", "etc"));
                File.WriteAllBytes(Kernel, Enumerable.Range(0, 2048).Select(I => (byte)(I * 7)).ToArray());
                File.WriteAllText(Path.Combine(Root, "ramdisk", "etc", "hello"), "hi");

                File.WriteAllText(Layout,
                    "schema: 1\nformat: nand\n" +
                    "options: { page: 2048, oob: 0, block: 131072, blocks: 16 }\n" +
                    "children:\n" +
                    "  - name: os\n" +
                    "    offset: 0x0\n" +
                    "    size: auto\n" +
                    "    kind: container\n" +
                    "    format: fit\n" +
                    "    children:\n" +
                    "      - { name: kernel, kind: kernel, content: { file: '" + Kernel + "' } }\n" +
                    "      - { name: ramdisk, kind: filesystem, format: cpio," +
                    " content: { dir: '" + Path.Combine(Root, "ramdisk") + "' } }\n");

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                Assert.True(File.Exists(Out));
                var Data = File.ReadAllBytes(Out);
                Assert.Equal(new byte[] { 0xD0, 0x0D, 0xFE, 0xED }, Data.Take(4).ToArray());
            }
            finally
            {
                if (Directory.Exists(Root)) Directory.Delete(Root, true);
                if (File.Exists(Layout)) File.Delete(Layout);
                if (File.Exists(Out)) File.Delete(Out);
            }
        }

        [Fact]
        public void Plan_Build_Android_Container_From_Files()
        {
            var Root = Path.Combine(Path.GetTempPath(), "nand-android-" + Guid.NewGuid().ToString("N"));
            var Layout = Path.Combine(Path.GetTempPath(), "nand-layout-" + Guid.NewGuid().ToString("N") + ".yaml");
            var Out = Path.Combine(Path.GetTempPath(), "nand-plan-" + Guid.NewGuid().ToString("N") + ".bin");
            var Kernel = Path.Combine(Root, "kernel.bin");

            try
            {
                Directory.CreateDirectory(Path.Combine(Root, "ramdisk", "etc"));
                File.WriteAllBytes(Kernel, Enumerable.Range(0, 1024).Select(I => (byte)(I * 3)).ToArray());
                File.WriteAllText(Path.Combine(Root, "ramdisk", "etc", "hello"), "hi");

                File.WriteAllText(Layout,
                    "schema: 1\nformat: nand\n" +
                    "options: { page: 2048, oob: 0, block: 131072, blocks: 16 }\n" +
                    "children:\n" +
                    "  - name: boot\n" +
                    "    offset: 0x0\n" +
                    "    size: auto\n" +
                    "    kind: container\n" +
                    "    format: android\n" +
                    "    children:\n" +
                    "      - { name: kernel, kind: kernel, content: { file: '" + Kernel + "' } }\n" +
                    "      - { name: ramdisk, kind: filesystem, format: cpio," +
                    " content: { dir: '" + Path.Combine(Root, "ramdisk") + "' } }\n");

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                Assert.True(File.Exists(Out));
                var Data = File.ReadAllBytes(Out);
                Assert.Equal(Encoding.ASCII.GetBytes("ANDROID!"), Data.Take(8).ToArray());
                Assert.Equal(1024u, BitConverter.ToUInt32(Data, 8));
            }
            finally
            {
                if (Directory.Exists(Root)) Directory.Delete(Root, true);
                if (File.Exists(Layout)) File.Delete(Layout);
                if (File.Exists(Out)) File.Delete(Out);
            }
        }
    }
}
