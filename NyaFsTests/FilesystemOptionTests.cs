using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.ImageFormat.Elements.Kernel;
using NyaFs.ImageFormat.Elements.Dtb;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Commands;
using Xunit;

namespace NyaFsTests
{
    public class FilesystemOptionTests
    {
        private sealed class OptionPlugin : FilesystemPlugin
        {
            public OptionPlugin() : base("test.options", "optfs", true, true, true) { }

            public override FilesystemOption[] Options => new[]
            {
                new FilesystemOption("enabled", FilesystemOptionType.Bool, "true"),
                new FilesystemOption("block-size", FilesystemOptionType.Size, "4096"),
                new FilesystemOption("mode", FilesystemOptionType.Enum, "fast", null, new[] { "fast", "safe" })
            };
        }

        private sealed class KernelOptionPlugin : KernelPlugin
        {
            public KernelOptionPlugin() : base("kernel.options", "optkernel", true, true, true) { }
            public override FilesystemOption[] Options => new[]
            {
                new FilesystemOption("mode", FilesystemOptionType.Enum, "fast", null, new[] { "fast", "safe" })
            };
        }

        private sealed class DtbOptionPlugin : DevtreePlugin
        {
            public DtbOptionPlugin() : base("dtb.options", "optdtb", true, true, true) { }
            public override FilesystemOption[] Options => new[]
            {
                new FilesystemOption("mode", FilesystemOptionType.Enum, "fast", null, new[] { "fast", "safe" })
            };
        }

        [Fact]
        public void PluginOptions_AreValidatedAndStored()
        {
            var Fs = new LinuxFilesystem();
            var Plugin = new OptionPlugin();

            Assert.True(Plugin.TryApplyOption(Fs, "enabled", "0", out _));
            Assert.True(Plugin.TryApplyOption(Fs, "block-size", "64K", out _));
            Assert.True(Plugin.TryApplyOption(Fs, "mode", "safe", out _));

            Assert.Equal("false", Fs.GetFilesystemOption("optfs", "enabled"));
            Assert.Equal("65536", Fs.GetFilesystemOption("optfs", "block-size"));
            Assert.Equal("safe", Fs.GetFilesystemOption("optfs", "mode"));
        }

        [Fact]
        public void SetCommand_AppliesExternalPluginOption()
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Processor = new ImageProcessor(Parser, new string[0]);
            Processor.Plugins.Load(new OptionPlugin());
            var Fs = new LinuxFilesystem();
            Processor.SetFs(Fs);

            var Script = Parser.ParseScript("test", new[] { "set ramfs optfs.mode safe" });
            Processor.Process(Script);

            Assert.Equal("safe", Fs.GetFilesystemOption("optfs", "mode"));
        }

        [Fact]
        public void SetCommand_AppliesExternalKernelAndDtbOptions()
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Processor = new ImageProcessor(Parser, new string[0]);
            Processor.Plugins.Load(new KernelOptionPlugin());
            Processor.Plugins.Load(new DtbOptionPlugin());
            var Kernel = new LinuxKernel { Image = new byte[] { 1 } };
            var Dtb = new DeviceTree();
            Processor.SetKernel(Kernel);
            Processor.SetDeviceTree(Dtb);

            var Script = Parser.ParseScript("test", new[]
            {
                "set kernel optkernel.mode safe",
                "set devtree optdtb.mode safe"
            });
            Processor.Process(Script);

            Assert.Equal("safe", Kernel.GetKernelOption("optkernel", "mode"));
            Assert.Equal("safe", Dtb.GetDevtreeOption("optdtb", "mode"));
        }

        [Fact]
        public void PluginOptions_RejectBadValues()
        {
            var Fs = new LinuxFilesystem();
            var Plugin = new OptionPlugin();

            Assert.False(Plugin.TryApplyOption(Fs, "enabled", "maybe", out _));
            Assert.False(Plugin.TryApplyOption(Fs, "block-size", "huge", out _));
            Assert.False(Plugin.TryApplyOption(Fs, "mode", "unknown", out _));
            Assert.False(Plugin.TryApplyOption(Fs, "missing", "x", out _));
        }
    }
}
