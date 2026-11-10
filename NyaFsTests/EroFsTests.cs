using NyaFs.Filesystem.EroFs;
using NyaFs.ImageFormat.Elements.Fs;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class EroFsTests
    {
        [Fact]
        public void EroFs_ReadSmallInlineImage()
        {
            var Path = Fixture("erofs_small.img");
            var Data = File.ReadAllBytes(Path);
            Assert.True(EroFsShared.IsEroFs(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.EroFs, FilesystemDetector.DetectFs(Data));

            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Contains(Root, E => E.ShortName == "hello.txt");
            Assert.Contains(Root, E => E.ShortName == "dir");
            Assert.Contains(Root, E => E.ShortName == "link");
            Assert.Equal("hello", Encoding.ASCII.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(Encoding.ASCII.GetBytes("1234"), Reader.Read("/dir/data.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));

            var Linux = new LinuxFilesystem();
            new NyaFs.ImageFormat.Elements.Fs.Reader.EroFsReader(Data).ReadToFs(Linux);
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.EroFs, Linux.FilesystemType);
            Assert.NotNull(Linux.Fs.GetElement("/hello.txt"));
        }

        [Fact]
        public void EroFs_ReadFlatInlineFile()
        {
            var Data = File.ReadAllBytes(Fixture("erofs_flat.img"));
            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Data);
            var Content = Reader.Read("/big.bin");
            Assert.NotNull(Content);
            Assert.Equal(100000, Content.Length);
        }

        [Fact]
        public void EroFs_WriteUncompressedImage()
        {
            var Builder = new NyaFs.Filesystem.EroFs.EroFsBuilder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.Directory("/dir", 1000, 1000, 0x1ED);
            Builder.File("/hello.txt", Encoding.ASCII.GetBytes("hello"), 0, 0, 0x1A4);
            var Data = new byte[100000];
            for (int i = 0; i < Data.Length; i++)
                Data[i] = (byte)(i * 37);
            Builder.File("/dir/data.bin", Data, 1000, 1000, 0x1A4);

            var Mix = new byte[220000];
            for (int i = 0; i < 100000; i++)
                Mix[i] = (byte)(i % 251);
            var RandomPart = new byte[20000];
            new Random(123).NextBytes(RandomPart);
            Array.Copy(RandomPart, 0, Mix, 100000, RandomPart.Length);
            for (int i = 120000; i < Mix.Length; i++)
                Mix[i] = (byte)(i % 251);
            Builder.File("/mix.bin", Mix, 0, 0, 0x1A4);
            Builder.File("/empty", Array.Empty<byte>(), 0, 0, 0x1A4);
            Builder.SymLink("/link", "hello.txt", 0, 0, 0x1FF);
            Builder.Char("/null", 1, 3, 0, 0, 0x1B6);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_EROFS_WRITE");
            if (!String.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            Assert.Equal(NyaFs.ImageFormat.Types.FsType.EroFs, FilesystemDetector.DetectFs(Image));

            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Image);
            Assert.Equal(Encoding.ASCII.GetBytes("hello"), Reader.Read("/hello.txt"));
            Assert.Equal(Data, Reader.Read("/dir/data.bin"));
            Assert.Equal(Mix, Reader.Read("/mix.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));

            var Device = Reader.ReadDevice("/null");
            Assert.NotNull(Device);
            Assert.Equal(1u, Device.Major);
            Assert.Equal(3u, Device.Minor);

            var Root = Reader.ReadDir("/");
            Assert.Contains(Root, E => E.ShortName == "dir");
            Assert.Contains(Root, E => E.ShortName == "hello.txt");
        }

        [Fact]
        public void EroFs_WriteLargeDirectory()
        {
            var Builder = new NyaFs.Filesystem.EroFs.EroFsBuilder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.Directory("/many", 0, 0, 0x1ED);
            for (int i = 0; i < 600; i++)
                Builder.File($"/many/file_{i:D3}.txt", Encoding.ASCII.GetBytes($"payload {i}"), 0, 0, 0x1A4);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_EROFS_WRITE_LARGE");
            if (!String.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Image);
            var Entries = Reader.ReadDir("/many");
            Assert.Equal(600, Entries.Length);
            Assert.Equal("payload 321", Encoding.ASCII.GetString(Reader.Read("/many/file_321.txt")));
        }

        [Fact]
        public void EroFs_WriteLargeCompressedFile()
        {
            var Content = new byte[1024 * 1024];
            for (int i = 0; i < Content.Length; i++)
                Content[i] = (byte)(i % 251);

            var Builder = new NyaFs.Filesystem.EroFs.EroFsBuilder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/large.bin", Content, 0, 0, 0x1A4);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_EROFS_WRITE_COMPRESSED");
            if (!String.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Image);
            Assert.Equal(Content, Reader.Read("/large.bin"));
            Assert.True(Image.Length < Content.Length);
        }

        [Fact]
        public void EroFs_WriteLongXattrPrefix()
        {
            var Builder = new NyaFs.Filesystem.EroFs.EroFsBuilder();
            Builder.AddXattrPrefix("security.custom.");
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/file.bin", Encoding.ASCII.GetBytes("data"), 0, 0, 0x1A4);
            Builder.Xattr("/file.bin", "security.custom.foo", Encoding.ASCII.GetBytes("long-prefix"));

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_EROFS_WRITE_LONG_XATTR");
            if (!String.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Image);
            var Entry = Array.Find(Reader.ReadDir("/"), E => E.ShortName == "file.bin");
            Assert.Equal("long-prefix", Encoding.ASCII.GetString(Entry.Xattrs["security.custom.foo"]));
        }

        [Fact]
        public void EroFs_WriteSharedXattrs()
        {
            var Builder = new NyaFs.Filesystem.EroFs.EroFsBuilder { SharedXattrs = true };
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/a.bin", Encoding.ASCII.GetBytes("a"), 0, 0, 0x1A4);
            Builder.File("/b.bin", Encoding.ASCII.GetBytes("b"), 0, 0, 0x1A4);
            Builder.Xattr("/a.bin", "security.selinux", Encoding.ASCII.GetBytes("u:object_r:foo:s0"));
            Builder.Xattr("/b.bin", "security.selinux", Encoding.ASCII.GetBytes("u:object_r:foo:s0"));
            Builder.Xattr("/a.bin", "user.test", Encoding.ASCII.GetBytes("v1"));

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_EROFS_WRITE_SHARED_XATTR");
            if (!String.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Image);
            var Entries = Reader.ReadDir("/");
            var A = Array.Find(Entries, E => E.ShortName == "a.bin");
            var B = Array.Find(Entries, E => E.ShortName == "b.bin");
            Assert.Equal("u:object_r:foo:s0", Encoding.ASCII.GetString(A.Xattrs["security.selinux"]));
            Assert.Equal("u:object_r:foo:s0", Encoding.ASCII.GetString(B.Xattrs["security.selinux"]));
            Assert.Equal("v1", Encoding.ASCII.GetString(A.Xattrs["user.test"]));
        }

        [Fact]
        public void EroFs_WriteTailFragments()
        {
            var Builder = new NyaFs.Filesystem.EroFs.EroFsBuilder { TailFragments = true };
            Builder.Directory("/", 0, 0, 0x1ED);
            var Data = new byte[2 * 1024 * 1024];
            for (int i = 0; i < Data.Length; i++)
                Data[i] = (byte)(i % 251);
            Builder.File("/data.bin", Data, 0, 0, 0x1A4);
            Builder.File("/other.bin", Data, 0, 0, 0x1A4);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_EROFS_WRITE_TAILFRAG");
            if (!String.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Image);
            Assert.Equal(Data, Reader.Read("/data.bin"));
            Assert.Equal(Data, Reader.Read("/other.bin"));
        }

        [Fact]
        public void EroFs_WriteXattrs()
        {
            var Builder = new NyaFs.Filesystem.EroFs.EroFsBuilder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/file.bin", Encoding.ASCII.GetBytes("xattr payload"), 0, 0, 0x1A4);
            Builder.Xattr("/", "security.root", Encoding.ASCII.GetBytes("rootvalue"));
            Builder.Xattr("/file.bin", "user.test", Encoding.ASCII.GetBytes("v1"));
            Builder.Xattr("/file.bin", "security.selinux", Encoding.ASCII.GetBytes("u:object_r:foo:s0"));

            var Compressed = new byte[100000];
            for (int i = 0; i < Compressed.Length; i++)
                Compressed[i] = (byte)(i * 37);
            Builder.File("/compressed.bin", Compressed, 0, 0, 0x1A4);
            Builder.Xattr("/compressed.bin", "user.big", Encoding.ASCII.GetBytes("compressed-xattr"));

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_EROFS_WRITE_XATTR");
            if (!String.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Image);
            Assert.Equal("xattr payload", Encoding.ASCII.GetString(Reader.Read("/file.bin")));
            Assert.Equal(Compressed, Reader.Read("/compressed.bin"));
        }

        [Fact]
        public void EroFs_WriteFragments()
        {
            var Builder = new NyaFs.Filesystem.EroFs.EroFsBuilder { Fragments = true };
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/hello.txt", Encoding.ASCII.GetBytes("hello"), 0, 0, 0x1A4);
            var Data = new byte[100000];
            for (int i = 0; i < Data.Length; i++)
                Data[i] = (byte)(i * 37);
            Builder.File("/data.bin", Data, 0, 0, 0x1A4);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_EROFS_WRITE_FRAGMENTS");
            if (!String.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Image);
            Assert.Equal(Encoding.ASCII.GetBytes("hello"), Reader.Read("/hello.txt"));
            Assert.Equal(Data, Reader.Read("/data.bin"));
        }

        [Fact]
        public void EroFs_WriteBigPcluster()
        {
            var Chunk = new byte[16 * 1024];
            new Random(7).NextBytes(Chunk);
            var Content = new byte[Chunk.Length * 8];
            for (int i = 0; i < 8; i++)
                Array.Copy(Chunk, 0, Content, i * Chunk.Length, Chunk.Length);

            var Builder = new NyaFs.Filesystem.EroFs.EroFsBuilder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/big.bin", Content, 0, 0, 0x1A4);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_EROFS_WRITE_BIG");
            if (!String.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Image);
            Assert.Equal(Content, Reader.Read("/big.bin"));
            Assert.True(Image.Length < Content.Length);
        }

        [Fact]
        public void EroFs_WriteThroughFilesystem()
        {
            var Fs = new LinuxFilesystem
            {
                FilesystemType = NyaFs.ImageFormat.Types.FsType.EroFs
            };
            Fs.Fs.Root.Items.Add(new NyaFs.Filesystem.Universal.Items.File(
                "hello.txt", 0, 0, 0x1A4, Encoding.ASCII.GetBytes("hello")));
            Fs.Fs.Root.Items.Add(new NyaFs.Filesystem.Universal.Items.SymLink(
                "link", 0, 0, 0x1FF, "hello.txt"));

            var Writer = new NyaFs.ImageFormat.Elements.Fs.Writer.EroFsWriter();
            Writer.WriteFs(Fs);
            Assert.True(Writer.HasRawStreamData);

            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Writer.RawStream);
            Assert.Equal(Encoding.ASCII.GetBytes("hello"), Reader.Read("/hello.txt"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));
        }

        [Theory]
        [InlineData("erofs_compact.img")]
        [InlineData("erofs_full.img")]
        [InlineData("erofs_bigpcl.img")]
        [InlineData("erofs_ztail.img")]
        [InlineData("erofs_frag.img")]
        [InlineData("erofs_fragtail.img")]
        [InlineData("erofs_dircomp.img")]
        [InlineData("erofs_full_bigpcl.img")]
        [InlineData("erofs_deflate.img")]
        [InlineData("erofs_zstd.img")]
        [InlineData("erofs_lzma.img")]
        public void EroFs_ReadCompressedFile(string FixtureName)
        {
            var Data = File.ReadAllBytes(Fixture(FixtureName));
            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Data);

            Assert.Equal(Encoding.ASCII.GetBytes("hello"), Reader.Read("/hello.txt"));
            Assert.Equal(ExpectedBigText(), Reader.Read("/big.txt"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));
        }

        [Theory]
        [InlineData("erofs_chunk.img")]
        [InlineData("erofs_chunk_idx.img")]
        public void EroFs_ReadChunkBasedFile(string FixtureName)
        {
            var Data = File.ReadAllBytes(Fixture(FixtureName));
            var Reader = new NyaFs.Filesystem.EroFs.EroFsReader(Data);

            Assert.Equal(ExpectedBigText(), Reader.Read("/big.txt"));
            Assert.Equal(Encoding.ASCII.GetBytes("hello"), Reader.Read("/hello.txt"));
        }

        private static byte[] ExpectedBigText()
        {
            var Text = new StringBuilder();
            for (int i = 0; i < 1500; i++)
                Text.Append($"line {i:D6} abcdefghijklmnopqrstuvwxyz0123456789 {new string('x', 20)}\n");

            return Encoding.ASCII.GetBytes(Text.ToString());
        }
        private static string Fixture(string Name) =>
            Path.Combine(AppContext.BaseDirectory, "Fixtures", Name);
    }
}
