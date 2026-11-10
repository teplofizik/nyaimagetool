using NyaFs.Filesystem.Jffs2;
using System;
using System.IO;
using System.Text;
using System.Linq;
using Xunit;

namespace NyaFsTests
{
    public class Jffs2Tests
    {
        private static string Fixture(string Name) =>
            Path.Combine(AppContext.BaseDirectory, "Fixtures", Name);

        [Fact]
        public void Jffs2_WriteRoundTrip()
        {
            var Builder = new NyaFs.Filesystem.Jffs2.Jffs2Builder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.Directory("/dir", 1, 2, 0x1ED);
            Builder.File("/hello.txt", Encoding.UTF8.GetBytes("hello"), 1, 2, 0x1A4);
            Builder.File("/dir/data.bin", new byte[] { 1, 2, 3, 4, 5 }, 3, 4, 0x1A0);
            Builder.SymLink("/link", "hello.txt", 1, 2, 0x1FF);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_JFFS2_WRITE");
            if (!string.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            Assert.True(Jffs2Reader.IsJffs2(Image));
            var Reader = new Jffs2Reader(Image);
            Assert.Equal("hello", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, Reader.Read("/dir/data.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));
        }

        [Fact]
        public void Jffs2_WriteXattrs()
        {
            var Builder = new NyaFs.Filesystem.Jffs2.Jffs2Builder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/hello.txt", Encoding.ASCII.GetBytes("hello"), 0, 0, 0x1A4);
            Builder.Xattr("/", "user.root", new byte[] { 1, 2 });
            Builder.Xattr("/hello.txt", "user.test", new byte[] { 3, 4, 5 });

            var Image = Builder.GetFilesystemImage();
            var Reader = new Jffs2Reader(Image);
            var Root = Reader.ReadDir("/");
            var File = Root.First(E => E.ShortName == "hello.txt");
            Assert.Equal(new byte[] { 3, 4, 5 }, File.Xattrs["user.test"]);
            Assert.Equal(new byte[] { 1, 2 }, Reader.GetRootXattrs()["user.root"]);
        }

        [Theory]
        [InlineData(NyaFs.Filesystem.Jffs2.Jffs2Compression.Zlib)]
        [InlineData(NyaFs.Filesystem.Jffs2.Jffs2Compression.Rtime)]
        [InlineData(NyaFs.Filesystem.Jffs2.Jffs2Compression.Lzo)]
        public void Jffs2_WriteCompressed(NyaFs.Filesystem.Jffs2.Jffs2Compression Compression)
        {
            var Builder = new NyaFs.Filesystem.Jffs2.Jffs2Builder(0x10000, 2048, Compression);
            Builder.Directory("/", 0, 0, 0x1ED);
            var Payload = Encoding.ASCII.GetBytes(String.Concat(Enumerable.Repeat("abcde", 20000)));
            Builder.File("/large.txt", Payload, 0, 0, 0x1A4);

            var Image = Builder.GetFilesystemImage();
            Assert.True(Jffs2Reader.IsJffs2(Image));
            var Reader = new Jffs2Reader(Image);
            Assert.Equal(Payload, Reader.Read("/large.txt"));
        }

        [Fact]
        public void Jffs2_ReadCompressed()
        {
            var Data = File.ReadAllBytes(Fixture("jffs2_compress.img"));
            var Reader = new Jffs2Reader(Data);
            var Large = Reader.Read("/large.txt");
            Assert.Equal(10001, Large.Length);
            Assert.Equal(Encoding.ASCII.GetBytes("abcdeabcde"), Large.Take(10).ToArray());
            Assert.Equal(5000, Reader.Read("/zero.bin").Length);
        }

        [Fact]
        public void Jffs2_ReadRtime()
        {
            var Reader = new Jffs2Reader(File.ReadAllBytes(Fixture("jffs2_rtime.img")));
            var Large = Reader.Read("/large.txt");
            Assert.Equal(10001, Large.Length);
            Assert.Equal(Encoding.ASCII.GetBytes("abcdeabcde"), Large.Take(10).ToArray());
            Assert.Equal(5000, Reader.Read("/zero.bin").Length);
        }

        [Fact]
        public void Jffs2_ReadSample()
        {
            var Data = File.ReadAllBytes(Fixture("jffs2_sample.img"));
            Assert.True(Jffs2Reader.IsJffs2(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Jffs2,
                NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));

            var Reader = new Jffs2Reader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Contains(Root, E => E.ShortName == "hello.txt");
            Assert.Contains(Root, E => E.ShortName == "dir1");
            Assert.Equal("hello jffs2\n", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
            Assert.Equal("payload from jffs2\n", Encoding.UTF8.GetString(Reader.Read("/dir1/data.bin")));
            Assert.Equal("nested\n", Encoding.UTF8.GetString(Reader.Read("/dir1/dir2/nested.txt")));
            Assert.Equal("../hello.txt", Reader.ReadLink("/dir1/link"));
        }
    }
}
