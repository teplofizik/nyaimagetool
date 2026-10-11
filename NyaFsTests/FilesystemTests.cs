using System;
using System.Linq;
using System.Text;
using NyaFs.Filesystem.CramFs;
using NyaFs.Filesystem.Ext2.Types;
using NyaFs.Filesystem.SquashFs.Types;
using NyaFs.FlattenedDeviceTree.Writer.Types;
using Xunit;

namespace NyaFsTests
{
    public class FilesystemTests
    {
        [Fact]
        public void ExtSuperBlock_WTime_Uses_Offset_0x30()
        {
            var data = new byte[0x40];
            var sb = new ExtSuperBlock(data, 0);

            sb.WTime = 0x11223344;
            sb.MTime = 0xAABBCCDD;

            Assert.Equal(0x11223344u, sb.WTime);
            Assert.Equal(0xAABBCCDDu, sb.MTime);
            Assert.Equal(0x44, data[0x30]);
            Assert.Equal(0xDD, data[0x2C]);
        }

        [Fact]
        public void SqFragmentBlockEntry_Stores_Uncompressed_Flag()
        {
            var c = new SqFragmentBlockEntry("p", 0x2000, 0x123, true);
            Assert.True(c.IsCompressed);
            Assert.Equal(0x123u, c.Size);

            var u = new SqFragmentBlockEntry("p", 0x2000, 0x123, false);
            Assert.False(u.IsCompressed);
            Assert.Equal(0x123u, u.Size);
        }

        [Fact]
        public void SqFragmentBlockEntry_Size_Setter_Preserves_Flag()
        {
            var u = new SqFragmentBlockEntry("p", 0x2000, 0x123, false);
            u.Size = 0x10;
            Assert.Equal(0x10u, u.Size);
            Assert.False(u.IsCompressed);
        }

        [Fact]
        public void FDTNop_Writes_Token_0x04()
        {
            var packet = new FDTNop().getPacket();

            Assert.Equal(4, packet.Length);
            Assert.Equal(0x04, packet[3]);
            Assert.Equal(0x00, packet[0]);
        }

        [Fact]
        public void CramFs_CharDevice_RoundTrip()
        {
            var b = new CramFsBuilder();
            b.Directory("/", 0, 0, 493);
            b.Directory("/dev", 0, 0, 493);
            b.Char("/dev/ttyS0", 4, 64, 0, 0, 438);

            var image = b.GetFilesystemImage();
            var r = new CramFsReader(image);

            var dev = r.ReadDevice("/dev/ttyS0");
            Assert.NotNull(dev);
            Assert.Equal(4u, dev.Major);
            Assert.Equal(64u, dev.Minor);
        }

        [Fact]
        public void CramFs_BlockDevice_RoundTrip()
        {
            var b = new CramFsBuilder();
            b.Directory("/", 0, 0, 493);
            b.Directory("/dev", 0, 0, 493);
            b.Block("/dev/mmcblk0", 179, 0, 0, 0, 438);

            var image = b.GetFilesystemImage();
            var r = new CramFsReader(image);

            var dev = r.ReadDevice("/dev/mmcblk0");
            Assert.NotNull(dev);
            Assert.Equal(179u, dev.Major);
            Assert.Equal(0u, dev.Minor);
        }

        [Fact]
        public void CramFs_Gzip_RoundTrip()
        {
            var data = Encoding.UTF8.GetBytes("compressible payload: " + string.Concat(Enumerable.Repeat("abc", 200)));

            var compressed = NyaFs.Filesystem.CramFs.Compression.Gzip.Compress(data);
            var decompressed = NyaFs.Filesystem.CramFs.Compression.Gzip.Decompress(compressed);

            Assert.Equal(data, decompressed);
        }

        [Fact]
        public void CramFs_FileContent_RoundTrip()
        {
            var content = Encoding.UTF8.GetBytes("hello cramfs content " + string.Concat(Enumerable.Repeat("xyz", 500)));

            var b = new CramFsBuilder();
            b.Directory("/", 0, 0, 493);
            b.File("/test.txt", content, 0, 0, 420);

            var image = b.GetFilesystemImage();
            var r = new CramFsReader(image);

            Assert.Equal(content, r.Read("/test.txt"));
        }

        [Fact]
        public void SquashFs_FileContent_RoundTrip()
        {
            var content = Encoding.UTF8.GetBytes("hello squashfs content " + string.Concat(Enumerable.Repeat("qwerty", 500)));

            var b = new NyaFs.Filesystem.SquashFs.SquashFsBuilder(NyaFs.Filesystem.SquashFs.Types.SqCompressionType.Gzip);
            b.Directory("/", 0, 0, 493);
            b.File("/test.txt", content, 0, 0, 420);

            var image = b.GetFilesystemImage();
            var r = new NyaFs.Filesystem.SquashFs.SquashFsReader(image);

            Assert.Equal(content, r.Read("/test.txt"));
        }
    }
}
