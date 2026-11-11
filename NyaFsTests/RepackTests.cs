using NyaFs.Filesystem.ExFat;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Commands;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class RepackTests
    {
        private static string SourceFor(string Relative)
        {
            if (Relative.StartsWith("fixtures/", StringComparison.OrdinalIgnoreCase))
            {
                var Fixture = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures",
                    Relative.Substring("fixtures/".Length));
                return File.Exists(Fixture) ? Fixture : null;
            }

            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                return null;

            var Path = System.IO.Path.Combine(Root, Relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
            return File.Exists(Path) ? Path : null;
        }

        private static string FirstFile(LinuxFilesystem Fs) => FirstFile(Fs.Fs.Root);

        private static string FirstFile(NyaFs.Filesystem.Universal.Items.Dir Dir)
        {
            foreach (var Item in Dir.Items)
            {
                if (Item is NyaFs.Filesystem.Universal.Items.File)
                    return Item.Filename;
                if (Item is NyaFs.Filesystem.Universal.Items.Dir)
                {
                    var Res = FirstFile((NyaFs.Filesystem.Universal.Items.Dir)Item);
                    if (Res != null)
                        return Res;
                }
            }

            return null;
        }

        [Theory]
        [InlineData("ramfs/cpio/overlay.gz")]
        [InlineData("ramfs/tar/test.tar.gz")]
        [InlineData("ramfs/ext2/ramfs.ext2")]
        [InlineData("ramfs/ext3/test.ext3")]
        [InlineData("ramfs/ext4/test.ext4")]
        [InlineData("ramfs/cramfs/test.cramfs")]
        [InlineData("ramfs/romfs/ramfs.romfs")]
        [InlineData("ramfs/squashfs/ramfs.sqfs")]
        [InlineData("ramfs/squashfs/ramfs.gzip.sqfs")]
        [InlineData("ramfs/fat12/sample.fat12")]
        [InlineData("ramfs/fat16/sample.fat16")]
        [InlineData("ramfs/fat32/sample.fat32")]
        [InlineData("fixtures/iso_sample.iso")]
        [InlineData("nand/f2fs/sample.f2fs")]
        [InlineData("fixtures/erofs_flat.img")]
        [InlineData("fixtures/udf_small.img")]
        [InlineData("fixtures/jffs2_sample.img")]
        [InlineData("fixtures/yaffs2_sample.bin.gz")]
        [InlineData("fixtures/ubifs_sample.ubi")]
        [InlineData("fixtures/ubifs_sample.raw")]
        public void Repack_Root_Filesystem(string Relative)
        {
            var Source = SourceFor(Relative);
            if (Source != null)
                RepackSource(Source);
        }

        [Fact]
        public void Repack_ExFat_Generated()
        {
            var Builder = new ExFatBuilder(16L * 1024 * 1024);
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/hello.txt", Encoding.ASCII.GetBytes("hello"), 0, 0, 0x1A4);

            var Source = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs-exfat-" + Guid.NewGuid().ToString("N") + ".exfat");
            File.WriteAllBytes(Source, Builder.GetFilesystemImage());
            RepackSource(Source);
        }

        private static void RepackSource(string Source)
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Source + "\"" }));

            var Fs = Proc.GetFs();
            Assert.NotNull(Fs);
            var Path = FirstFile(Fs);
            Assert.False(string.IsNullOrEmpty(Path));

            var Payload = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs-repack-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(Payload, "repacked");
            var Script = Parser.ParseScript("t", new[]
            {
                "file \"" + Path + "\" \"" + Payload + "\""
            });
            Proc.Process(Script);

            var Extension = System.IO.Path.GetExtension(Source);
            if (String.IsNullOrEmpty(Extension))
                Extension = ".img";
            var Target = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs-repack-" + Guid.NewGuid().ToString("N") + Extension);
            var Res = new Store().Get(new ScriptArgs(3, new[] { Target })).Exec(Proc);
            Assert.Equal(ScriptStepStatus.Ok, Res.Status);

            var ReadParser = new ScriptParser(new ScriptBaseAll());
            var ReadProc = new ImageProcessor(ReadParser, new string[0]);
            ReadProc.Process(ReadParser.ParseScript("t", new[] { "load \"" + Target + "\"" }));

            var Check = ReadProc.GetFs();
            Assert.NotNull(Check);
            var Item = Check.GetElement(Path) as NyaFs.Filesystem.Universal.Items.File;
            Assert.NotNull(Item);
            Assert.Equal(Encoding.UTF8.GetBytes("repacked"), Item.Content);
        }
    }
}
