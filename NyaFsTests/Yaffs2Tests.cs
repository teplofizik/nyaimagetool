using NyaFs.Filesystem.Yaffs2;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class Yaffs2Tests
    {
        [Fact]
        public void Yaffs2_ReadSample()
        {
            var Data = ReadFixture("yaffs2_sample.bin.gz");
            Assert.True(Yaffs2Reader.IsYaffs2(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Yaffs2, NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));

            var Reader = new Yaffs2Reader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Contains(Root, E => E.ShortName == "test1.txt");
            Assert.Contains(Root, E => E.ShortName == "dir1");
            Assert.Equal("test1", Encoding.UTF8.GetString(Reader.Read("/test1.txt")));
            Assert.Equal(300, Reader.Read("/dir1/lorem.txt").Length);
            Assert.Equal("../../../test1.txt", Reader.ReadLink("/dir1/dir2/dir3/link1"));
            Assert.Equal("test2", Encoding.UTF8.GetString(Reader.Read("/dir1/dir41/test2.txt")));
            Assert.Contains(Reader.ReadDir("/dir1"), E => E.ShortName == "dir2");

            var Dst = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
            new NyaFs.ImageFormat.Elements.Fs.Reader.BaseFsReader(
                NyaFs.ImageFormat.Types.FsType.Yaffs2, new Yaffs2Reader(Data)).ReadToFs(Dst);
            Assert.Contains(Dst.Fs.Root.Items, E => E.Filename == "test1.txt");
        }

        [Fact]
        public void Yaffs2_WriteRoundTrip()
        {
            var Builder = new Yaffs2Builder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.Directory("/dir", 1, 2, 0x1ED);
            Builder.File("/hello.txt", Encoding.UTF8.GetBytes("hello"), 1, 2, 0x1A4);
            Builder.File("/dir/data.bin", new byte[] { 1, 2, 3, 4, 5 }, 3, 4, 0x1A0);
            Builder.SymLink("/link", "hello.txt", 1, 2, 0x1FF);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_YAFFS2_WRITE");
            if (!string.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            Assert.True(Yaffs2Reader.IsYaffs2(Image));
            var Reader = new Yaffs2Reader(Image);
            Assert.Equal("hello", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, Reader.Read("/dir/data.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));
            Assert.Equal(1u, Reader.ReadDir("/").First(E => E.ShortName == "hello.txt").User);
        }

        private static byte[] ReadFixture(string Name)
        {
            using (var Stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", Name)))
            using (var Gzip = new GZipStream(Stream, CompressionMode.Decompress))
            using (var Output = new MemoryStream())
            {
                Gzip.CopyTo(Output);
                return Output.ToArray();
            }
        }
    }
}
