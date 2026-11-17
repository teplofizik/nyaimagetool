using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using NyaFs.Filesystem.Fat;
using NyaFs.Filesystem.SquashFs;
using NyaFs.Filesystem.SquashFs.Types;
using NyaFs.Filesystem.UbiFs;
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
        public void Plan_Cpio_Content_Compression_Produces_Archive()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "fs-plan-" + Guid.NewGuid().ToString("N"));
            var Root = Path.Combine(Dir, "root");
            Directory.CreateDirectory(Path.Combine(Root, "etc"));

            var Layout = Path.Combine(Dir, "cpio.yaml");
            var Out = Path.Combine(Dir, "root.cpio.gz");

            try
            {
                File.WriteAllText(Path.Combine(Root, "etc", "hello"), "hello cpio");

                File.WriteAllText(Layout,
                    "schema: 1\nformat: cpio\n" +
                    "content: { dir: root, compression: gzip }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                var Data = File.ReadAllBytes(Out);
                Assert.Equal(0x1F, Data[0]);
                Assert.Equal(0x8B, Data[1]);

                var Cpio = Gunzip(Data);
                Assert.Equal((byte)'0', Cpio[0]);
                Assert.Equal((byte)'7', Cpio[1]);
                Assert.Equal((byte)'0', Cpio[2]);
                Assert.Equal((byte)'7', Cpio[3]);
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }

        [Fact]
        public void Layout_Export_Cpio_Is_Editable_Directory()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "fs-plan-" + Guid.NewGuid().ToString("N"));
            var Root = Path.Combine(Dir, "root");
            Directory.CreateDirectory(Path.Combine(Root, "etc"));

            var Layout = Path.Combine(Dir, "cpio.yaml");
            var Image = Path.Combine(Dir, "root.cpio.gz");
            var Exported = Path.Combine(Dir, "exported.yaml");

            try
            {
                File.WriteAllText(Path.Combine(Root, "etc", "hello"), "hello export");

                File.WriteAllText(Layout,
                    "schema: 1\nformat: cpio\n" +
                    "content: { dir: root, compression: gzip }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc,
                    "build \"" + Layout + "\" \"" + Image + "\"",
                    "layout export \"" + Image + "\" \"" + Exported + "\"");

                var Yaml = File.ReadAllText(Exported);
                Assert.Contains("compression: 'gzip'", Yaml);
                Assert.Contains("dir:", Yaml);
                Assert.DoesNotContain("immutable: true", Yaml);

                var Extracted = Path.Combine(Exported + ".files", "root", "etc", "hello");
                Assert.True(File.Exists(Extracted));
                Assert.Equal("hello export", File.ReadAllText(Extracted));
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }

        [Fact]
        public void Plan_Iso9660_El_Torito_Boot_Catalog()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "iso-plan-" + Guid.NewGuid().ToString("N"));
            var Root = Path.Combine(Dir, "root");
            Directory.CreateDirectory(Path.Combine(Root, "boot"));

            var Layout = Path.Combine(Dir, "iso.yaml");
            var Out = Path.Combine(Dir, "boot.iso");

            var Boot = new byte[4096];
            for (var I = 0; I < Boot.Length; I++)
                Boot[I] = (byte)(I * 13 + 7);

            try
            {
                File.WriteAllBytes(Path.Combine(Root, "boot", "loader.bin"), Boot);

                File.WriteAllText(Layout,
                    "schema: 1\nformat: iso9660\n" +
                    "options: { boot-image: boot/loader.bin, boot-load-size: 8 }\n" +
                    "content: { dir: root }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                var Data = File.ReadAllBytes(Out);

                var BootRecord = 17 * 2048;
                Assert.Equal(0, Data[BootRecord]);
                Assert.Equal("CD001", Encoding.ASCII.GetString(Data, BootRecord + 1, 5));
                Assert.Contains("EL TORITO SPECIFICATION",
                    Encoding.ASCII.GetString(Data, BootRecord + 7, 32));

                var CatalogLba = BitConverter.ToUInt32(Data, BootRecord + 71);
                var Catalog = (int)CatalogLba * 2048;
                Assert.Equal(1, Data[Catalog]);
                Assert.Equal(0x55, Data[Catalog + 30]);
                Assert.Equal(0xAA, Data[Catalog + 31]);

                var Sum = 0;
                for (var I = 0; I < 32; I += 2)
                    Sum += Data[Catalog + I] | (Data[Catalog + I + 1] << 8);
                Assert.Equal(0, Sum & 0xFFFF);

                Assert.Equal(0x88, Data[Catalog + 32]);
                Assert.Equal(8, Data[Catalog + 38] | (Data[Catalog + 39] << 8));

                var BootLba = BitConverter.ToUInt32(Data, Catalog + 40);
                var BootOffset = (int)BootLba * 2048;
                for (var I = 0; I < Boot.Length; I++)
                    Assert.Equal(Boot[I], Data[BootOffset + I]);
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }

        [Fact]
        public void Plan_MkImage_Filesystem_Stores_Cpio()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "fs-plan-" + Guid.NewGuid().ToString("N"));
            var Root = Path.Combine(Dir, "root");
            Directory.CreateDirectory(Path.Combine(Root, "etc"));

            var Layout = Path.Combine(Dir, "cpio.yaml");
            var Out = Path.Combine(Dir, "out.cpio");

            try
            {
                File.WriteAllText(Path.Combine(Root, "etc", "hello"), "mkimage cpio");

                File.WriteAllText(Layout,
                    "schema: 1\nformat: cpio\ncontent: { dir: root }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc,
                    "mkimage cpio \"" + Layout + "\"",
                    "store \"" + Out + "\" ramfs cpio");

                var Data = File.ReadAllBytes(Out);
                Assert.Equal((byte)'0', Data[0]);
                Assert.Equal((byte)'7', Data[1]);
                Assert.Equal((byte)'0', Data[2]);
                Assert.Equal((byte)'7', Data[3]);
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }

        [Fact]
        public void Plan_MultiVolume_Ubi_Builds_Dynamic_And_Static()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "ubi-plan-" + Guid.NewGuid().ToString("N"));
            var Root = Path.Combine(Dir, "root");
            Directory.CreateDirectory(Path.Combine(Root, "etc"));

            var Layout = Path.Combine(Dir, "ubi.yaml");
            var Kernel = Path.Combine(Dir, "kernel.bin");
            var Out = Path.Combine(Dir, "out.ubi");

            var KernelData = Encoding.ASCII.GetBytes("kernel payload for a static volume");

            try
            {
                File.WriteAllText(Path.Combine(Root, "etc", "hello"), "hello ubifs");
                File.WriteAllBytes(Kernel, KernelData);

                File.WriteAllText(Layout,
                    "schema: 1\nformat: ubi\n" +
                    "options: { peb: 131072, page: 2048 }\n" +
                    "children:\n" +
                    "  - name: rootfs\n" +
                    "    kind: filesystem\n" +
                    "    format: ubifs\n" +
                    "    content: { dir: root }\n" +
                    "  - name: kernel\n" +
                    "    kind: raw\n" +
                    "    content: { file: kernel.bin }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc, "build \"" + Layout + "\" \"" + Out + "\"");

                var Ubi = new UbiFsReader(File.ReadAllBytes(Out));
                Assert.Equal(2, Ubi.Volumes.Count);

                var Rootfs = Ubi.FindVolume(0);
                Assert.NotNull(Rootfs);
                Assert.Equal("rootfs", Rootfs.Name);
                Assert.Equal(NyaFs.Filesystem.UbiFs.Types.VolumeType.UBI_VID_DYNAMIC, Rootfs.Type);

                var KernelVol = Ubi.Volumes.Find(V => V.Name == "kernel");
                Assert.NotNull(KernelVol);
                Assert.Equal(NyaFs.Filesystem.UbiFs.Types.VolumeType.UBI_VID_STATIC, KernelVol.Type);

                var KernelLeb = Ubi.ReadLeb(KernelVol.Id, 0);
                Assert.NotNull(KernelLeb);
                for (var I = 0; I < KernelData.Length; I++)
                    Assert.Equal(KernelData[I], KernelLeb[I]);
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }

        private static byte[] Gunzip(byte[] Data)
        {
            using (var Input = new MemoryStream(Data))
            using (var Gzip = new GZipStream(Input, CompressionMode.Decompress))
            using (var Output = new MemoryStream())
            {
                Gzip.CopyTo(Output);
                return Output.ToArray();
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
