using System;
using NyaFs.ImageFormat.Helper;
using NyaFs.Processor;
using Xunit;

namespace NyaFsTests
{
    public class FormatTests
    {
        [Theory]
        [InlineData("rwxr-xr-x")]
        [InlineData("rw-r--r--")]
        [InlineData("rwxrwxrwx")]
        public void ModeString_RoundTrip(string text)
        {
            var mode = Utils.ConvertMode(text);
            Assert.Equal(text, Utils.ConvertModeToString(mode));
        }

        [Fact]
        public void ModeString_755_Numeric_Form()
        {
            // 0755 octal == 0x1ED
            Assert.Equal("rwxr-xr-x", Utils.ConvertModeToString(0x1ED));
        }

        [Fact]
        public void DetectCompression_Xz()
        {
            var data = new byte[] { 0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00 };
            Assert.Equal(NyaFs.ImageFormat.Types.CompressionType.IH_COMP_XZ, FitHelper.DetectCompression(data));
        }

        [Fact]
        public void DetectCompression_Gzip()
        {
            var data = new byte[] { 0x1F, 0x8B, 0x08, 0x00 };
            Assert.Equal(NyaFs.ImageFormat.Types.CompressionType.IH_COMP_GZIP, FitHelper.DetectCompression(data));
        }

        [Fact]
        public void GetCompression_Xz_String_Is_Supported()
        {
            Assert.Equal(NyaFs.ImageFormat.Types.CompressionType.IH_COMP_XZ, FitHelper.GetCompression("xz"));
        }

        [Fact]
        public void FitPlugin_Requires_Real_Fit_Not_Bare_Fdt_Magic()
        {
            var plugin = new NyaFs.ImageFormat.Plugins.ImagePlugins.CompositeImagePlugins.FitPlugin();

            // A bare FDT magic is a DTB, not a FIT (no 'configurations'/'images' nodes).
            Assert.False(plugin.DetectImage(new NyaFs.ImageFormat.ImageData(new byte[] { 0xD0, 0x0D, 0xFE, 0xED })));
            Assert.False(plugin.DetectImage(new NyaFs.ImageFormat.ImageData(System.Text.Encoding.ASCII.GetBytes("ANDROID!"))));
        }
    }
}
