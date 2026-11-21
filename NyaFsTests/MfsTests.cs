using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAncientFs;
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using Xunit;

namespace NyaFsTests
{
    public class MfsTests
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
        public void Mfs_ReadSample()
        {
            var Data = ReadFixture("mfs_sample.img.gz");
            if (Data == null)
                return;

            Assert.True(MfsReader.IsMfs(Data));
            var Reader = new MfsReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Equal(3, Root.Length);
            Assert.Contains(Root, E => E.ShortName == "HELLO.TXT");
            Assert.Contains(Root, E => E.ShortName == "BIG.BIN");
            Assert.Contains(Root, E => E.ShortName == "A.TXT");

            Assert.Equal("hello mfs\n", System.Text.Encoding.ASCII.GetString(Reader.Read("/HELLO.TXT")));
            Assert.Equal("aaa\n", System.Text.Encoding.ASCII.GetString(Reader.Read("/A.TXT")));
            Assert.Equal(4000, Reader.Read("/BIG.BIN").Length);
            Assert.Equal("3933f303ec65329941f26857de97b8ef7362018a5c7641c0d0386c03900d3b9f",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/BIG.BIN"))).ToLowerInvariant());
        }

        [Fact]
        public void Mfs_Loads_Via_Pipeline()
        {
            var Data = ReadFixture("mfs_sample.img.gz");
            if (Data == null)
                return;

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_mfs_sample.mfs");
            File.WriteAllBytes(Path, Data);
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new MfsFilesystemPlugin());

                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Assert.True(Fs.Loaded);
                Assert.Equal("mfs", Fs.FilesystemFormat);
                Assert.NotNull(Fs.GetElement("BIG.BIN"));
            }
            finally
            {
                File.Delete(Path);
            }
        }
    }
}
