using NyaFs.Filesystem.Minix;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class MinixTests
    {
        private static byte[] ReadFixture(string Name)
        {
            var Path = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", Name);
            if (!File.Exists(Path))
                return null;

            using (var Stream = File.OpenRead(Path))
            using (var Gzip = new GZipStream(Stream, CompressionMode.Decompress))
            using (var Output = new MemoryStream())
            {
                Gzip.CopyTo(Output);
                return Output.ToArray();
            }
        }

        [Theory]
        [InlineData("minix_v1_sample.img.gz", "abcdefghijklm")]
        [InlineData("minix_v1_30_sample.img.gz", "abcdefghijklmnopqrstuvwxyz123")]
        [InlineData("minix_v2_sample.img.gz", "abcdefghijklm")]
        [InlineData("minix_v2_30_sample.img.gz", "abcdefghijklmnopqrstuvwxyz123")]
        [InlineData("minix_v3_sample.img.gz", "abcdefghijklmnopqrstuvwxyz1234567890")]
        public void Minix_ReadSample(string Fixture, string LongName)
        {
            var Data = ReadFixture(Fixture);
            if (Data == null)
                return;

            Assert.True(MinixReader.IsMinix(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Minix,
                NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));

            var Reader = new MinixReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Equal(4, Root.Length);
            Assert.Contains(Root, E => E.ShortName == "hello.txt");
            Assert.Contains(Root, E => E.ShortName == "dir");
            Assert.Contains(Root, E => E.ShortName == "link");
            Assert.Contains(Root, E => E.ShortName == LongName);

            Assert.Equal("hello", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
            Assert.Equal("nested", Encoding.UTF8.GetString(Reader.Read("/dir/nested.txt")));
            Assert.Equal("long", Encoding.UTF8.GetString(Reader.Read("/" + LongName)));
            Assert.Equal("../hello.txt", Reader.ReadLink("/link"));
            Assert.Single(Reader.ReadDir("/dir"));
        }
    }
}
