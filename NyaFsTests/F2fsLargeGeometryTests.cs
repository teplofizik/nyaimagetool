using NyaFs.Filesystem.F2fs;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class F2fsLargeGeometryTests
    {
        [Fact]
        public void F2fs_LargeImageGeometry()
        {
            long Size = 513L * 1024 * 1024;
            var Builder = new F2fsFsBuilder(Size);
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/hello.txt", Encoding.ASCII.GetBytes("hello"), 0, 0, 0x1A4);

            var Image = Builder.GetFilesystemImage();

            var Sb = new F2fsSuperblock(Image, 0);
            Assert.Equal(Size / F2fsFsBuilder.BlockSize, (long)Sb.BlockCount);
            Assert.Equal(Sb.SegmentCountMain, Sb.SectionCount * Sb.SegsPerSec);
            Assert.True(Sb.SegmentCountMain > 0);
            Assert.True(Sb.Segment0Blkaddr <= Sb.CpBlkaddr);
            Assert.True(Sb.CpBlkaddr < Sb.SitBlkaddr);
            Assert.True(Sb.SitBlkaddr < Sb.NatBlkaddr);
            Assert.True(Sb.NatBlkaddr < Sb.SsaBlkaddr);
            Assert.True(Sb.SsaBlkaddr < Sb.MainBlkaddr);

            int Cp = (int)Sb.CpBlkaddr * F2fsFsBuilder.BlockSize;
            uint Rsvd = ReadU32(Image, Cp + 0x18);
            uint Overprov = ReadU32(Image, Cp + 0x1C);
            uint Free = ReadU32(Image, Cp + 0x20);
            ulong UserBlocks = ReadU64(Image, Cp + 0x08);

            Assert.True(Rsvd > 0);
            Assert.True(Overprov >= Rsvd);
            Assert.True(Free > 0);
            Assert.True(UserBlocks > 0);
        }

        private static uint ReadU32(byte[] Data, int Offset) =>
            (uint)(Data[Offset] | (Data[Offset + 1] << 8) | (Data[Offset + 2] << 16) | (Data[Offset + 3] << 24));

        private static ulong ReadU64(byte[] Data, int Offset) =>
            ReadU32(Data, Offset) | ((ulong)ReadU32(Data, Offset + 4) << 32);
    }
}
