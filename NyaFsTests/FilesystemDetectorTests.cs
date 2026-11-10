using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.ImageFormat.Plugins;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFs.ImageFormat.Types;
using Xunit;

namespace NyaFsTests
{
    [CollectionDefinition("PluginHost", DisableParallelization = true)]
    public class PluginHostCollection { }

    [Collection("PluginHost")]
    public class FilesystemDetectorTests
    {
        private sealed class TestPlugin : FilesystemPlugin
        {
            private readonly bool Matches;
            private readonly int PluginPriority;

            public TestPlugin(string Name, FsType Type, bool Matches, int Priority = 0)
                : base(Name, Name, true, false, false)
            {
                FsTypeOverride = Type;
                this.Matches = Matches;
                PluginPriority = Priority;
            }

            private readonly FsType FsTypeOverride;

            public override FsType FsType => FsTypeOverride;
            public override int Priority => PluginPriority;
            public override bool DetectFilesystem(ImageData Image) => Matches;
        }

        [Fact]
        public void DetectFs_UsesExplicitPluginList()
        {
            var Plugins = new[] { new TestPlugin("test-romfs", FsType.RomFs, true) };

            Assert.Equal(FsType.RomFs, FilesystemDetector.DetectFs(new byte[] { 1, 2, 3 }, Plugins));
        }

        [Fact]
        public void DetectFs_ProbesHigherPriorityFirst()
        {
            var Plugins = new[]
            {
                new TestPlugin("low", FsType.Ext2, true, 0),
                new TestPlugin("high", FsType.Ext4, true, 100)
            };

            Assert.Equal(FsType.Ext4, FilesystemDetector.DetectFs(new byte[] { 1, 2, 3 }, Plugins));
        }

        [Fact]
        public void DetectFs_UsesPluginHostRegistry()
        {
            var Previous = PluginHost.Current;
            try
            {
                var Registry = new PluginRegistry();
                Registry.Load(new TestPlugin("host-test", FsType.Udf, true));
                PluginHost.SetCurrent(Registry);

                Assert.Equal(FsType.Udf, FilesystemDetector.DetectFs(new byte[] { 1, 2, 3 }));
            }
            finally
            {
                PluginHost.SetCurrent(Previous);
            }
        }

        [Fact]
        public void DetectPlugin_ReturnsFirstMatch()
        {
            var Plugin = new TestPlugin("test-udf", FsType.Udf, true);
            var Found = FilesystemDetector.DetectFilesystemPlugin(new byte[] { 1, 2, 3 }, new[] { Plugin });

            Assert.Same(Plugin, Found);
        }
    }
}
