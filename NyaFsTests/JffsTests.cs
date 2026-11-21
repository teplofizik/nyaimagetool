using NyaFs.Filesystem.Jffs;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class JffsTests
    {
        private static string Fixture(string Name) =>
            Path.Combine(AppContext.BaseDirectory, "Fixtures", Name);

        [Fact]
        public void Jffs_WriteRoundTrip()
        {
            var Builder = new JffsBuilder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.Directory("/dir", 1, 2, 0x1ED);
            Builder.File("/hello.txt", Encoding.UTF8.GetBytes("hello"), 1, 2, 0x1A4);
            Builder.File("/empty.bin", Array.Empty<byte>(), 3, 4, 0x1A0);
            Builder.File("/dir/data.bin", new byte[] { 1, 2, 3, 4, 5 }, 3, 4, 0x1A0);
            Builder.SymLink("/link", "hello.txt", 1, 2, 0x1FF);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_JFFS_WRITE");
            if (!string.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            Assert.True(JffsReader.IsJffs(Image));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Jffs,
                NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Image));

            var Reader = new JffsReader(Image);
            Assert.Equal("hello", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
            Assert.Empty(Reader.Read("/empty.bin"));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, Reader.Read("/dir/data.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));
            Assert.Contains(Reader.ReadDir("/"), E => E.ShortName == "dir");
        }

        [Fact]
        public void Jffs_WriteLargeFile()
        {
            var Builder = new JffsBuilder(0x10000, 1024);
            Builder.Directory("/", 0, 0, 0x1ED);
            var Payload = Enumerable.Range(0, 5000).Select(I => (byte)(I * 31)).ToArray();
            Builder.File("/large.bin", Payload, 0, 0, 0x1A4);

            var Image = Builder.GetFilesystemImage();
            var Reader = new JffsReader(Image);
            Assert.Equal(Payload, Reader.Read("/large.bin"));
        }

        [Fact]
        public void Jffs_ReadBigEndian()
        {
            var Path = Fixture("jffs_be_sample.img");
            if (!File.Exists(Path))
                return;

            var Data = File.ReadAllBytes(Path);
            Assert.True(JffsReader.IsJffs(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Jffs,
                NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));

            var Reader = new JffsReader(Data);
            Assert.Equal("big endian", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(new byte[] { 9, 8, 7 }, Reader.Read("/dir/data.bin"));
        }

        [Fact]
        public void Jffs_ReadSample()
        {
            var Path = Fixture("jffs_sample.img");
            if (!File.Exists(Path))
                return;

            var Data = File.ReadAllBytes(Path);
            Assert.True(JffsReader.IsJffs(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Jffs,
                NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));

            var Reader = new JffsReader(Data);
            Assert.Equal("hello", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, Reader.Read("/dir/data.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));
        }
    }
}
