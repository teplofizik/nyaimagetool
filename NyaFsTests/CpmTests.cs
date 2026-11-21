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
    public class CpmTests
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
        public void Cpm_ReadSample()
        {
            var Data = ReadFixture("cpm_sample.dsk.gz");
            if (Data == null)
                return;

            Assert.True(CpmReader.TryDetect(Data, "cpm_sample.dsk", out var Geometry));
            Assert.Equal("ibm-3740", Geometry.Name);

            var Reader = new CpmReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Equal(4, Root.Length);
            Assert.Contains(Root, E => E.ShortName == "HELLO.TXT");
            Assert.Contains(Root, E => E.ShortName == "BIG.BIN");
            Assert.Contains(Root, E => E.ShortName == "A.TXT");
            Assert.Contains(Root, E => E.ShortName == "B.TXT");

            Assert.Equal(10, Reader.Read("/HELLO.TXT").Length);
            Assert.Equal(40000, Reader.Read("/BIG.BIN").Length);
            Assert.Equal("aaa\n", System.Text.Encoding.ASCII.GetString(Reader.Read("/A.TXT")));
            Assert.Equal("bbb\n", System.Text.Encoding.ASCII.GetString(Reader.Read("/B.TXT")));
            Assert.Equal("d8aaf2784606d73401b9e21bb7a55d82dda0cee133208b067190f09f4d28969f",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/HELLO.TXT"))).ToLowerInvariant());
            Assert.Equal("09d836824791eca49b73762404f73a3ec5bbd51945a9761f9934cda1a7d64cbc",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/BIG.BIN"))).ToLowerInvariant());
        }

        [Fact]
        public void Cpm_Read2kExtents()
        {
            var Data = ReadFixture("cpm_2k_small.dsk.gz");
            if (Data == null)
                return;

            Assert.True(CpmReader.TryDetect(Data, "cpm_2k_small.dsk", out var Geometry));
            Assert.Equal("cpm-2k-40t", Geometry.Name);

            var Reader = new CpmReader(Data);
            Assert.Equal("hello 2k\n", System.Text.Encoding.ASCII.GetString(Reader.Read("/HELLO.TXT")));
            Assert.Equal("aaa\n", System.Text.Encoding.ASCII.GetString(Reader.Read("/A.TXT")));
            Assert.Equal(40000, Reader.Read("/BIG.BIN").Length);
            Assert.Equal("37779a54e0be19906834b916ec71d90d3ea65a4c968cd710714ecaa096935e58",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/BIG.BIN"))).ToLowerInvariant());
        }

        [Fact]
        public void Cpm_Read16BitPointers()
        {
            var Data = ReadFixture("cpm_16bit.dsk.gz");
            if (Data == null)
                return;

            Assert.True(CpmReader.TryDetect(Data, "cpm_16bit.dsk", out var Geometry));
            Assert.Equal("cpm-2k-210t", Geometry.Name);

            var Reader = new CpmReader(Data);
            Assert.Equal("hello 16\n", System.Text.Encoding.ASCII.GetString(Reader.Read("/HELLO.TXT")));
            Assert.Equal("aaa\n", System.Text.Encoding.ASCII.GetString(Reader.Read("/A.TXT")));
            Assert.Equal(40000, Reader.Read("/BIG.BIN").Length);
            Assert.Equal("525bd7a68af0ae8a0051b7a5eaf68145342a22bfbc2c6506568348d0f20621e8",
                Convert.ToHexString(SHA256.HashData(Reader.Read("/BIG.BIN"))).ToLowerInvariant());
        }

        [Fact]
        public void Cpm_Loads_Via_Pipeline()
        {
            var Data = ReadFixture("cpm_sample.dsk.gz");
            if (Data == null)
                return;

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_cpm_sample.dsk");
            File.WriteAllBytes(Path, Data);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new CpmFilesystemPlugin());

                Proc.Process(Parser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

                var Fs = Proc.GetFs();
                Assert.NotNull(Fs);
                Assert.True(Fs.Loaded);
                Assert.Equal("cpm", Fs.FilesystemFormat);
                Assert.NotNull(Fs.GetElement("HELLO.TXT"));
                Assert.Equal(40000, Fs.GetElement("BIG.BIN").Size);
            }
            finally
            {
                File.Delete(Path);
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }
    }
}
