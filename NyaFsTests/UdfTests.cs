using NyaFs.Filesystem.Udf;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class UdfTests
    {
        [Fact]
        public void Udf_ReadSmallImage()
        {
            var Data = File.ReadAllBytes(Fixture("udf_small.img"));
            Assert.True(UdfReader.IsUdfImage(Data));
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Udf, NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(Data));
            var Reader = new UdfReader(Data);
            var Root = Reader.ReadDir("/");
            Assert.Contains(Root, E => E.ShortName == "hello.txt");
            Assert.Contains(Root, E => E.ShortName == "dir");
            Assert.Equal(Encoding.ASCII.GetBytes("hello"), Reader.Read("/hello.txt"));
            Assert.Equal(Encoding.ASCII.GetBytes("payload"), Reader.Read("/dir/data.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));
        }

        [Fact]
        public void Udf_WriteSmallImage()
        {
            var Builder = new UdfBuilder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.Directory("/dir", 0, 0, 0x1ED);
            Builder.File("/hello.txt", Encoding.ASCII.GetBytes("hello"), 0, 0, 0x1A4);
            Builder.File("/dir/data.bin", Encoding.ASCII.GetBytes("payload"), 0, 0, 0x1A4);
            Builder.SymLink("/link", "hello.txt", 0, 0, 0x1FF);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_UDF_WRITE");
            if (!String.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            var Reader = new UdfReader(Image);
            Assert.Equal(Encoding.ASCII.GetBytes("hello"), Reader.Read("/hello.txt"));
            Assert.Equal(Encoding.ASCII.GetBytes("payload"), Reader.Read("/dir/data.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));
        }

        [Fact]
        public void Udf_WriteLargeDirectory()
        {
            var Builder = new UdfBuilder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.Directory("/many", 0, 0, 0x1ED);
            for (int i = 0; i < 300; i++)
                Builder.File($"/many/file_{i:D3}.txt", Encoding.ASCII.GetBytes($"payload {i}"), 0, 0, 0x1A4);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_UDF_WRITE_LARGE");
            if (!String.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            var Reader = new UdfReader(Image);
            Assert.Equal(300, Reader.ReadDir("/many").Length);
            Assert.Equal("payload 123", Encoding.ASCII.GetString(Reader.Read("/many/file_123.txt")));
        }

        [Fact]
        public void Udf_WriteVatImage()
        {
            var Builder = new UdfBuilder(true);
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.Directory("/dir", 0, 0, 0x1ED);
            Builder.File("/hello.txt", Encoding.ASCII.GetBytes("hello"), 0, 0, 0x1A4);
            Builder.File("/dir/data.bin", Encoding.ASCII.GetBytes("payload"), 0, 0, 0x1A4);
            Builder.SymLink("/link", "hello.txt", 0, 0, 0x1FF);

            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_UDF_WRITE_VAT");
            if (!String.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);

            var Reader = new UdfReader(Image);
            Assert.Equal(Encoding.ASCII.GetBytes("hello"), Reader.Read("/hello.txt"));
            Assert.Equal(Encoding.ASCII.GetBytes("payload"), Reader.Read("/dir/data.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));
        }

        [Fact]
        public void Udf_WriteEmptyVatImage()
        {
            var Image = new UdfBuilder(true).GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_UDF_WRITE_VAT_EMPTY");
            if (!string.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);
            var Reader = new UdfReader(Image);
            Assert.Empty(Reader.ReadDir("/"));
        }

        [Fact]
        public void Udf_WriteSparableImage()
        {
            var Builder = new UdfBuilder(false, true);
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/hello.txt", Encoding.ASCII.GetBytes("hello"), 0, 0, 0x1A4);
            var Image = Builder.GetFilesystemImage();
            var Output = Environment.GetEnvironmentVariable("NYAIMAGETOOL_UDF_WRITE_SPARABLE");
            if (!string.IsNullOrEmpty(Output))
                File.WriteAllBytes(Output, Image);
            var Reader = new UdfReader(Image);
            Assert.Equal(Encoding.ASCII.GetBytes("hello"), Reader.Read("/hello.txt"));
        }

        [Fact]
        public void Udf_ReadVatImage() => ReadFixture("udf_vat.img");

        [Fact]
        public void Udf_ReadSparableImage() => ReadFixture("udf_spar.img");

        private static void ReadFixture(string Name)
        {
            var Reader = new UdfReader(File.ReadAllBytes(Fixture(Name)));
            Assert.Empty(Reader.ReadDir("/"));
        }

        private static string Fixture(string Name) =>
            Path.Combine(AppContext.BaseDirectory, "Fixtures", Name);
    }
}
