using NyaFs.ImageFormat;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using Xunit;

namespace NyaFsTests
{
    /// <summary>
    /// Phase 2 of the plugin layer: image element detection (kernel/filesystem) is provided
    /// by plugins and exposed through the registry (ImageProcessor.Plugins.DetectImage).
    /// </summary>
    public class ImageDetectionTests
    {
        private static ScriptPlugins Plugins()
        {
            var parser = new ScriptParser(new ScriptBaseAll());
            var proc = new ImageProcessor(parser, new string[0]);
            return proc.Plugins;
        }

        private static NyaFs.ImageFormat.ImageData Legacy(byte Type)
        {
            var b = new byte[0x40];
            b[0] = 0x27; b[1] = 0x05; b[2] = 0x19; b[3] = 0x56; // uImage magic (BE 0x27051956)
            b[0x1E] = Type; // ih_type
            return new ImageData(b);
        }

        [Fact]
        public void LegacyKernel_IsComposite_WithKernelChild()
        {
            var p = Plugins().DetectComposite(Legacy(2)); // IH_TYPE_KERNEL

            Assert.NotNull(p);
            Assert.Equal("legacy", p.Format);

            var imgs = p.GetImages(Legacy(2));
            Assert.Single(imgs);
            Assert.Equal("kernel", imgs[0].Name);
            Assert.Equal(ImageKind.Kernel, imgs[0].Kind);
        }

        [Fact]
        public void LegacyRamdisk_IsComposite_WithFilesystemChild()
        {
            var p = Plugins().DetectComposite(Legacy(3)); // IH_TYPE_RAMDISK

            Assert.NotNull(p);

            var imgs = p.GetImages(Legacy(3));
            Assert.Single(imgs);
            Assert.Equal("ramdisk", imgs[0].Name);
            Assert.Equal(ImageKind.Filesystem, imgs[0].Kind);
        }

        [Fact]
        public void ZImage()
        {
            var b = new byte[0x28];
            b[0x24] = 0x18; b[0x25] = 0x28; b[0x26] = 0x6F; b[0x27] = 0x01; // zImage magic (BE)

            var d = Plugins().DetectImage(new ImageData(b));

            Assert.Equal(ImageKind.Kernel, d.Kind);
            Assert.Equal("zimage", d.Format);
        }

        [Fact]
        public void Cpio()
        {
            var b = new byte[] { 0x30, 0x37, 0x30, 0x37, 0x30, 0x31 }; // "070701"

            var d = Plugins().DetectImage(new ImageData(b));

            Assert.Equal(ImageKind.Filesystem, d.Kind);
            Assert.Equal("cpio", d.Format);
        }

        [Fact]
        public void SquashFs()
        {
            var b = new byte[] { 0x68, 0x73, 0x71, 0x73, 0x00, 0x00 }; // "hsqs"

            var d = Plugins().DetectImage(new ImageData(b));

            Assert.Equal(ImageKind.Filesystem, d.Kind);
            Assert.Equal("squashfs", d.Format);
        }

        [Fact]
        public void Unknown()
        {
            var d = Plugins().DetectImage(new ImageData(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 }));

            Assert.Equal(ImageKind.Unknown, d.Kind);
        }
    }
}
