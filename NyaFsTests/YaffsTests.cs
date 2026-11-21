using NyaFs.Filesystem.Yaffs;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class YaffsTests
    {
        [Fact]
        public void Yaffs_WriteRoundTrip()
        {
            var Builder = new YaffsBuilder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.Directory("/dir", 1, 2, 0x1ED);
            Builder.File("/hello.txt", Encoding.UTF8.GetBytes("hello"), 1, 2, 0x1A4);
            Builder.File("/dir/data.bin", new byte[] { 1, 2, 3, 4, 5 }, 3, 4, 0x1A0);
            Builder.SymLink("/link", "hello.txt", 1, 2, 0x1FF);
            Builder.Char("/null", 1, 3, 0, 0, 0x1B6);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_YAFFS_WRITE");
            if (!string.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            Assert.True(YaffsReader.IsYaffs(Image));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Yaffs,
                NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Image));

            var Reader = new YaffsReader(Image);
            var Root = Reader.ReadDir("/");
            Assert.Contains(Root, E => E.ShortName == "hello.txt");
            Assert.Contains(Root, E => E.ShortName == "dir");
            Assert.Equal("hello", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, Reader.Read("/dir/data.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));

            var Dev = Reader.ReadDevice("/null");
            Assert.NotNull(Dev);
            Assert.Equal(1u, Dev.Major);
            Assert.Equal(3u, Dev.Minor);
        }

        [Fact]
        public void Yaffs_WriteLargeFile()
        {
            var Builder = new YaffsBuilder();
            Builder.Directory("/", 0, 0, 0x1ED);
            var Payload = Enumerable.Range(0, 3000).Select(I => (byte)(I * 17)).ToArray();
            Builder.File("/large.bin", Payload, 0, 0, 0x1A4);
            Builder.File("/filler.bin", new byte[700], 0, 0, 0x1A4);

            var Image = Builder.GetFilesystemImage();
            var Reader = new YaffsReader(Image);
            Assert.Equal(Payload, Reader.Read("/large.bin"));
        }

        [Fact]
        public void Yaffs2_IsNotDetectedAsYaffs1()
        {
            var Source = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "yaffs2_sample.bin.gz");
            if (!File.Exists(Source))
                return;

            using (var Stream = File.OpenRead(Source))
            using (var Gzip = new System.IO.Compression.GZipStream(Stream, System.IO.Compression.CompressionMode.Decompress))
            using (var Output = new MemoryStream())
            {
                Gzip.CopyTo(Output);
                var Data = Output.ToArray();
                Assert.False(YaffsReader.IsYaffs(Data));
                Assert.Equal(NyaFs.ImageFormat.Types.FsType.Yaffs2,
                    NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));
            }
        }

        [Fact]
        public void Yaffs_ReadSample()
        {
            var Source = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "yaffs_sample.bin");
            if (!File.Exists(Source))
                return;

            var Data = File.ReadAllBytes(Source);
            Assert.True(YaffsReader.IsYaffs(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Yaffs,
                NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));

            var Reader = new YaffsReader(Data);
            Assert.Equal("hello", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, Reader.Read("/dir/data.bin"));
        }
    }
}
