using NyaFs.ImageFormat.Helper;
using NyaFs.ImageFormat.Types;
using Xunit;

namespace NyaFsTests
{
    /// <summary>
    /// Phase 1 of the plugin layer: compression detection is provided by compressor plugins
    /// (FitHelper.DetectCompression delegates to them).
    /// </summary>
    public class CompressorDetectionTests
    {
        [Fact]
        public void Gzip() =>
            Assert.Equal(CompressionType.IH_COMP_GZIP, FitHelper.DetectCompression(new byte[] { 0x1F, 0x8B, 0x08, 0x00 }));

        [Fact]
        public void Bzip2() =>
            Assert.Equal(CompressionType.IH_COMP_BZIP2, FitHelper.DetectCompression(new byte[] { 0x42, 0x5A, 0x68, 0x39 }));

        [Fact]
        public void Lz4() =>
            Assert.Equal(CompressionType.IH_COMP_LZ4, FitHelper.DetectCompression(new byte[] { 0x04, 0x22, 0x4D, 0x18 }));

        [Fact]
        public void Lzma() =>
            Assert.Equal(CompressionType.IH_COMP_LZMA, FitHelper.DetectCompression(new byte[] { 0x5D, 0x00, 0x00, 0x80 }));

        [Fact]
        public void Lzo() =>
            Assert.Equal(CompressionType.IH_COMP_LZO, FitHelper.DetectCompression(new byte[] { 0x89, 0x4C, 0x5A, 0x4F, 0x00, 0x0D, 0x0A, 0x1A, 0x0A }));

        [Fact]
        public void Xz() =>
            Assert.Equal(CompressionType.IH_COMP_XZ, FitHelper.DetectCompression(new byte[] { 0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00 }));

        [Fact]
        public void Zstd() =>
            Assert.Equal(CompressionType.IH_COMP_ZSTD, FitHelper.DetectCompression(new byte[] { 0x28, 0xB5, 0x2F, 0xFD }));

        [Fact]
        public void None() =>
            Assert.Equal(CompressionType.IH_COMP_NONE, FitHelper.DetectCompression(new byte[] { 0x00, 0x01, 0x02, 0x03 }));

        [Fact]
        public void TooShort_IsNone() =>
            Assert.Equal(CompressionType.IH_COMP_NONE, FitHelper.DetectCompression(new byte[] { 0x1F }));
    }
}
