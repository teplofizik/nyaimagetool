using NyaFs.Filesystem.Xfs;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class XfsTests
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
        public void Xfs_MinimalDetectionAndRead()
        {
            var Path = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "xfs_min_sample.img");
            if (!File.Exists(Path))
                return;

            var Data = File.ReadAllBytes(Path);
            Assert.True(XfsReader.IsXfs(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Xfs,
                NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));

            var Reader = new XfsReader(Data);
            Assert.Single(Reader.ReadDir("/"));
            Assert.Equal("hello", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
        }

        [Fact]
        public void Xfs_ReadV5()
        {
            var Data = ReadFixture("xfs_v5_sample.img.gz");
            if (Data == null)
                return;

            Assert.True(XfsReader.IsXfs(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Xfs,
                NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));

            var Reader = new XfsReader(Data);
            Assert.Equal("hello", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, Reader.Read("/dir1/data.bin"));
            Assert.Equal("../hello.txt", Reader.ReadLink("/dir1/link"));
            Assert.Equal("nested", Encoding.UTF8.GetString(Reader.Read("/dir2/nested.txt")));

            Assert.Equal(300, Reader.ReadDir("/many").Length);
            Assert.Equal(80, Reader.ReadDir("/blockdir").Length);

            var Big = Reader.Read("/big.bin");
            Assert.Equal(2097152, Big.Length);
            using (var Sha = SHA256.Create())
                Assert.Equal("88C11E1DB6EC80D2EB8F45E3F8F80DF07EB7C164C487649A3B279B04B35B018E",
                    Convert.ToHexString(Sha.ComputeHash(Big)));

            var Sparse = Reader.Read("/sparse.bin");
            Assert.Equal(5242880, Sparse.Length);
            Assert.DoesNotContain(Sparse, B => B != 0);

            var Frag = Reader.Read("/frag.bin");
            Assert.Equal(520192, Frag.Length);
            Assert.Equal(0xAA, Frag[0]);
            Assert.Equal(0, Frag[4096]);
            Assert.Equal(0xAA, Frag[8192]);
            Assert.Equal(0, Frag[Frag.Length - 4096 - 1]);
            Assert.Equal(0xAA, Frag[Frag.Length - 4096]);
            Assert.Equal(0xAA, Frag[Frag.Length - 1]);
        }

        [Fact]
        public void Xfs_ReadV4()
        {
            var Data = ReadFixture("xfs_v4_sample.img.gz");
            if (Data == null)
                return;

            Assert.True(XfsReader.IsXfs(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Xfs,
                NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));

            var Reader = new XfsReader(Data);
            Assert.Equal("hello", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, Reader.Read("/dir1/data.bin"));
            Assert.Equal("../hello.txt", Reader.ReadLink("/dir1/link"));
            Assert.Equal(300, Reader.ReadDir("/many").Length);
        }
    }
}
