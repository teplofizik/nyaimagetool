using System;
using System.IO;
using System.Text;
using NyaFs.Filesystem.Fat;
using NyaFs.Filesystem.SquashFs;
using NyaFs.Filesystem.SquashFs.Types;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using Xunit;

namespace NyaFsTests
{
    public class FilesystemPlanTests
    {
        private static ImageProcessor Processor()
        {
            var Proc = new ImageProcessor(new ScriptParser(new ScriptBaseAll()), new string[0]);
            Proc.Plugins.Load(new NyaFsNand.NyaFsNandCompositeImagePlugin());
            return Proc;
        }

        private static void Run(ImageProcessor Proc, params string[] Lines) =>
            Proc.Process(new ScriptParser(new ScriptBaseAll()).ParseScript("t", Lines));

        [Fact]
        public void Plan_Standalone_Squashfs_Applies_Compression()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "fs-plan-" + Guid.NewGuid().ToString("N"));
            var Root = Path.Combine(Dir, "root");
            Directory.CreateDirectory(Path.Combine(Root, "etc"));

            var Layout = Path.Combine(Dir, "squashfs.yaml");
            var Out = Path.Combine(Dir, "root.squashfs");

            try
            {
                File.WriteAllText(Path.Combine(Root, "etc", "hello"), "hello squashfs");

                File.WriteAllText(Layout,
                    "schema: 1\nformat: squashfs\n" +
                    "options: { compression: lz4 }\n" +
                    "content: { dir: root }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                var Data = File.ReadAllBytes(Out);
                Assert.True(Data.Length > 96);
                Assert.Equal((byte)'h', Data[0]);
                Assert.Equal((byte)'s', Data[1]);
                Assert.Equal((byte)'q', Data[2]);
                Assert.Equal((byte)'s', Data[3]);

                var Reader = new SquashFsReader(Data);
                Assert.Equal(SqCompressionType.Lz4, Reader.Compression);
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }

        [Fact]
        public void Plan_Standalone_Fat_Uses_Type_And_Size()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "fs-plan-" + Guid.NewGuid().ToString("N"));
            var Root = Path.Combine(Dir, "root");
            Directory.CreateDirectory(Root);

            var Layout = Path.Combine(Dir, "fat.yaml");
            var Out = Path.Combine(Dir, "root.fat");

            try
            {
                File.WriteAllText(Path.Combine(Root, "hello.txt"), "hello fat");

                File.WriteAllText(Layout,
                    "schema: 1\nformat: fat\n" +
                    "options: { type: fat16, fs-size: 4M, size: 8M }\n" +
                    "content: { dir: root }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                var Data = File.ReadAllBytes(Out);
                Assert.Equal(8 * 1024 * 1024, Data.Length);

                var Reader = new FatFsReader(Data);
                Assert.Equal(16, Reader.FatType);
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }

        [Fact]
        public void Plan_Nand_Filesystem_Node_Applies_Options()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "fs-plan-" + Guid.NewGuid().ToString("N"));
            var Root = Path.Combine(Dir, "root");
            Directory.CreateDirectory(Path.Combine(Root, "etc"));

            var Layout = Path.Combine(Dir, "nand.yaml");
            var Out = Path.Combine(Dir, "nand.bin");

            try
            {
                File.WriteAllText(Path.Combine(Root, "etc", "hello"), "hello from nand");

                File.WriteAllText(Layout,
                    "schema: 1\nformat: nand\n" +
                    "options: { page: 2048, oob: 0, block: 131072, blocks: 32 }\n" +
                    "children:\n" +
                    "  - name: rootfs\n" +
                    "    offset: 0x0\n" +
                    "    size: 0x200000\n" +
                    "    kind: filesystem\n" +
                    "    format: squashfs\n" +
                    "    options: { compression: zstd }\n" +
                    "    content: { dir: root }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                var Data = File.ReadAllBytes(Out);
                Assert.Equal(32 * 131072, Data.Length);

                var Range = new byte[0x200000];
                Array.Copy(Data, 0, Range, 0, Range.Length);

                var Reader = new SquashFsReader(Range);
                Assert.Equal(SqCompressionType.Zstd, Reader.Compression);
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }

        [Fact]
        public void Plan_Standalone_Ext4_Honours_FsSize()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "fs-plan-" + Guid.NewGuid().ToString("N"));
            var Root = Path.Combine(Dir, "root");
            Directory.CreateDirectory(Root);

            var Layout = Path.Combine(Dir, "ext4.yaml");
            var Out = Path.Combine(Dir, "root.ext4");

            try
            {
                File.WriteAllText(Path.Combine(Root, "hello.txt"), "hello ext4");

                File.WriteAllText(Layout,
                    "schema: 1\nformat: ext4\n" +
                    "options: { fs-size: 16M }\n" +
                    "content: { dir: root }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                var Data = File.ReadAllBytes(Out);
                Assert.Equal(16 * 1024 * 1024, Data.Length);

                var BlockCount = BitConverter.ToUInt32(Data, 1024 + 4);
                var BlockLog = Data[1024 + 24];
                var Total = (long)BlockCount << (10 + BlockLog);
                Assert.InRange(Total, 15L * 1024 * 1024, 16L * 1024 * 1024);
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }

        [Fact]
        public void Plan_Region_Too_Small_Is_An_Error()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "fs-plan-" + Guid.NewGuid().ToString("N"));
            var Root = Path.Combine(Dir, "root");
            Directory.CreateDirectory(Root);

            var Layout = Path.Combine(Dir, "squashfs.yaml");
            var Out = Path.Combine(Dir, "root.squashfs");

            try
            {
                File.WriteAllText(Path.Combine(Root, "hello.txt"), "hello");

                File.WriteAllText(Layout,
                    "schema: 1\nformat: squashfs\n" +
                    "size: 1K\n" +
                    "content: { dir: root }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                Assert.False(File.Exists(Out));
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }
    }
}
