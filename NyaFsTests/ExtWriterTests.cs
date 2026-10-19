using System;
using NyaFs.Filesystem.Ext2;
using Xunit;

namespace NyaFsTests
{
    public class ExtWriterTests
    {
        private static byte[] Pattern(int Seed, int Length)
        {
            var Data = new byte[Length];
            new Random(Seed).NextBytes(Data);
            return Data;
        }

        [Fact]
        public void Ext3_RoundTrip_File_Content()
        {
            var Content = Pattern(1, 3000);

            var B = new Ext3FsBuilder(8 * 1024 * 1024);
            B.Directory("/", 0, 0, 0x1ED);
            B.File("/test.bin", Content, 0, 0, 0x1A4);

            var Image = B.GetFilesystemImage();
            var R = new Ext3FsReader(Image);

            Assert.Equal(Content, R.Read("/test.bin"));
        }

        [Fact]
        public void Ext4_RoundTrip_File_Content()
        {
            var Content = Pattern(2, 3000);

            var B = new Ext4FsBuilder(8 * 1024 * 1024);
            B.Directory("/", 0, 0, 0x1ED);
            B.File("/test.bin", Content, 0, 0, 0x1A4);

            var Image = B.GetFilesystemImage();
            var R = new Ext4FsReader(Image);

            Assert.Equal(Content, R.Read("/test.bin"));
        }

        [Fact]
        public void Ext4_Large_Fragmented_File_Uses_Index_Block_And_RoundTrips()
        {
            var Content = Pattern(3, 35 * 1024 * 1024);

            var B = new Ext4FsBuilder(48 * 1024 * 1024);
            B.Directory("/", 0, 0, 0x1ED);
            B.File("/big.bin", Content, 0, 0, 0x1A4);

            var Image = B.GetFilesystemImage();

            Assert.True(HasExtentIndexBlock(Image));

            var R = new Ext4FsReader(Image);
            Assert.Equal(Content, R.Read("/big.bin"));
        }

        private static uint LE32(byte[] D, int O) =>
            (uint)(D[O] | (D[O + 1] << 8) | (D[O + 2] << 16) | (D[O + 3] << 24));

        private static int LE16(byte[] D, int O) => D[O] | (D[O + 1] << 8);

        /// <summary>
        /// Scan the inode table for any inode whose extent tree has depth &gt; 0
        /// (i.e. an index block was required).
        /// </summary>
        private static bool HasExtentIndexBlock(byte[] Image)
        {
            uint InodeTableLo = LE32(Image, 0x808);
            uint InodesPerGroup = LE32(Image, 0x428);
            long InodeTable = InodeTableLo * 1024;

            for (uint i = 1; i <= InodesPerGroup; i++)
            {
                long Off = InodeTable + (i - 1) * 128;
                if (Off + 0x64 > Image.Length)
                    break;

                uint Flags = LE32(Image, (int)Off + 0x20);
                if ((Flags & 0x80000) == 0)
                    continue;

                if (LE16(Image, (int)Off + 0x28 + 6) > 0)
                    return true;
            }

            return false;
        }
    }
}
