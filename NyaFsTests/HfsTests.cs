using NyaFs.Filesystem.Hfs;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace NyaFsTests
{
    public class HfsTests
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

        [Fact]
        public void Hfs_ReadSample()
        {
            var Data = ReadFixture("hfs_sample.img.gz");
            if (Data == null)
                return;

            Assert.True(HfsReader.IsHfs(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Hfs,
                NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));

            var Reader = new HfsReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Equal(3, Root.Length);
            Assert.Contains(Root, E => E.ShortName == "hello.txt");
            Assert.Contains(Root, E => E.ShortName == "big.bin");
            Assert.Contains(Root, E => E.ShortName == "src");

            Assert.Equal("hello hfs\n", System.Text.Encoding.ASCII.GetString(Reader.Read("/hello.txt")));
            Assert.Equal("aaa\n", System.Text.Encoding.ASCII.GetString(Reader.Read("/src/a.txt")));
            Assert.Equal("bbb\n", System.Text.Encoding.ASCII.GetString(Reader.Read("/src/b.txt")));
            Assert.Equal(40000, Reader.Read("/big.bin").Length);
            Assert.Equal("605aa5969e7394e1e390130785f40cd920cb4492f5ba4d0160bd489b8b4337e3",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/big.bin"))).ToLowerInvariant());

            var Src = Reader.ReadDir("/src");
            Assert.Equal(2, Src.Length);
            Assert.Contains(Src, E => E.ShortName == "a.txt");
            Assert.Contains(Src, E => E.ShortName == "b.txt");
        }
    }
}
