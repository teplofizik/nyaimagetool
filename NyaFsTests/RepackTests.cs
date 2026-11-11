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
        private static string Corpus(string Relative)
        {
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
        [InlineData("ramfs/ext4/test.ext4")]
        [InlineData("ramfs/ext2/ramfs.ext2")]
        public void Repack_Root_Filesystem(string Relative)
        {
            var Source = Corpus(Relative);
            if (Source == null)
                return;

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

            var Target = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs-repack-" + Guid.NewGuid().ToString("N") + System.IO.Path.GetExtension(Source));
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
