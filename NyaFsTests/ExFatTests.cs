using NyaFs.Filesystem.ExFat;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class ExFatTests
    {
        [Fact]
        public void ExFat_RoundTrip()
        {
            var Builder = new ExFatBuilder(64L * 1024 * 1024);
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/hello.txt", Encoding.ASCII.GetBytes("hello"), 0, 0, 0x1A4);
            Builder.Directory("/dir", 0, 0, 0x1ED);
            Builder.File("/dir/data.bin", new byte[] { 1, 2, 3, 4 }, 0, 0, 0x1A4);

            var Image = Builder.GetFilesystemImage();

            Assert.True(ExFatShared.IsExFat(Image));

            var Reader = new ExFatReader(Image);
            var Root = Reader.ReadDir("/");
            Assert.Contains(Root, E => E.ShortName == "hello.txt");
            Assert.Contains(Root, E => E.ShortName == "dir");

            Assert.Equal("hello", Encoding.ASCII.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, Reader.Read("/dir/data.bin"));
            Assert.Single(Reader.ReadDir("/dir"));
        }
    }
}
