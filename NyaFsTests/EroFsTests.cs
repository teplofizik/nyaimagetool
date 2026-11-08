using NyaFs.Filesystem.EroFs;
using NyaFs.ImageFormat.Elements.Fs;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class EroFsTests
    {
        [Fact]
        public void EroFs_ReadSmallInlineImage()
        {
            var Path = Fixture("erofs_small.img");
            var Data = File.ReadAllBytes(Path);
            Assert.True(EroFsShared.IsEroFs(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.EroFs, FilesystemDetector.DetectFs(Data));

            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Contains(Root, E => E.ShortName == "hello.txt");
            Assert.Contains(Root, E => E.ShortName == "dir");
            Assert.Contains(Root, E => E.ShortName == "link");
            Assert.Equal("hello", Encoding.ASCII.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(Encoding.ASCII.GetBytes("1234"), Reader.Read("/dir/data.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));

            var Linux = new LinuxFilesystem();
            new NyaFs.ImageFormat.Elements.Fs.Reader.EroFsReader(Data).ReadToFs(Linux);
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.EroFs, Linux.FilesystemType);
            Assert.NotNull(Linux.Fs.GetElement("/hello.txt"));
        }

        [Fact]
        public void EroFs_ReadFlatInlineFile()
        {
            var Data = File.ReadAllBytes(Fixture("erofs_flat.img"));
            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Data);
            var Content = Reader.Read("/big.bin");
            Assert.NotNull(Content);
            Assert.Equal(100000, Content.Length);
        }

        [Theory]
        [InlineData("erofs_compact.img")]
        [InlineData("erofs_full.img")]
        [InlineData("erofs_bigpcl.img")]
        [InlineData("erofs_ztail.img")]
        [InlineData("erofs_frag.img")]
        [InlineData("erofs_dircomp.img")]
        [InlineData("erofs_full_bigpcl.img")]
        [InlineData("erofs_deflate.img")]
        [InlineData("erofs_zstd.img")]
        [InlineData("erofs_lzma.img")]
        public void EroFs_ReadCompressedFile(string FixtureName)
        {
            var Data = File.ReadAllBytes(Fixture(FixtureName));
            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Data);

            Assert.Equal(Encoding.ASCII.GetBytes("hello"), Reader.Read("/hello.txt"));
            Assert.Equal(ExpectedBigText(), Reader.Read("/big.txt"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));
        }

        [Theory]
        [InlineData("erofs_chunk.img")]
        [InlineData("erofs_chunk_idx.img")]
        public void EroFs_ReadChunkBasedFile(string FixtureName)
        {
            var Data = File.ReadAllBytes(Fixture(FixtureName));
            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Data);

            Assert.Equal(ExpectedBigText(), Reader.Read("/big.txt"));
            Assert.Equal(Encoding.ASCII.GetBytes("hello"), Reader.Read("/hello.txt"));
        }

        private static byte[] ExpectedBigText()
        {
            var Text = new StringBuilder();
            for (int i = 0; i < 1500; i++)
                Text.Append($"line {i:D6} abcdefghijklmnopqrstuvwxyz0123456789 {new string('x', 20)}\n");

            return Encoding.ASCII.GetBytes(Text.ToString());
        }
        private static string Fixture(string Name) =>
            Path.Combine(AppContext.BaseDirectory, "Fixtures", Name);
    }
}
