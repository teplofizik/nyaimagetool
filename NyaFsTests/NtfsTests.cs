using NyaFs.Filesystem.Ntfs;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class NtfsTests
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
        public void Ntfs_ReadSample()
        {
            var Data = ReadFixture("ntfs_sample.img.gz");
            if (Data == null)
                return;

            Assert.True(NtfsReader.IsNtfs(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Ntfs,
                NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));

            var Reader = new NtfsReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Equal(5, Root.Length);
            Assert.Contains(Root, E => E.ShortName == "hello.txt");
            Assert.Contains(Root, E => E.ShortName == "dir1");
            Assert.Contains(Root, E => E.ShortName == "dir2");
            Assert.Contains(Root, E => E.ShortName == "big.bin");

            Assert.Equal("hello", Encoding.UTF8.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, Reader.Read("/dir1/data.bin"));
            Assert.Equal("nested", Encoding.UTF8.GetString(Reader.Read("/dir2/nested.txt")));
            Assert.Single(Reader.ReadDir("/dir1"));
            Assert.Single(Reader.ReadDir("/dir2"));

            var Big = Reader.Read("/big.bin");
            Assert.Equal(2097152, Big.Length);
            using (var Sha = SHA256.Create())
                Assert.Equal("DC7AAEF5DBB7BBF3DF17A0600397A560A61D63FC1DD9C3582F6EEE1A97A7DDE8",
                    Convert.ToHexString(Sha.ComputeHash(Big)));
        }
    }
}
