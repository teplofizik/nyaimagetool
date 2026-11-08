using NyaFs.Filesystem.Iso9660;
using NyaFs.ImageFormat.Elements.Fs;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class Iso9660Tests
    {
        [Fact]
        public void Iso9660_RoundTrip()
        {
            var Builder = new Iso9660Builder();
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/hello.txt", Encoding.ASCII.GetBytes("hello"), 0, 0, 0x1A4);
            Builder.Directory("/dir", 0, 0, 0x1ED);
            Builder.File("/dir/data.bin", new byte[] { 1, 2, 3, 4 }, 12, 34, 0x1A0);
            Builder.SymLink("/link", "hello.txt", 0, 0, 0x1FF);

            var Image = Builder.GetFilesystemImage();

            Assert.True(Image.Length > 16 * 2048);
            Assert.Equal((byte)'C', Image[16 * 2048 + 1]);
            Assert.Equal((byte)'D', Image[16 * 2048 + 2]);
            Assert.Equal((byte)'0', Image[16 * 2048 + 3]);
            Assert.Equal((byte)'0', Image[16 * 2048 + 4]);
            Assert.Equal((byte)'1', Image[16 * 2048 + 5]);

            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Iso9660, FilesystemDetector.DetectFs(Image));

            var Reader = new NyaFs.Filesystem.Iso9660.Iso9660Reader(Image);
            var Root = Reader.ReadDir("/");

            Assert.Contains(Root, E => E.ShortName == "hello.txt");
            Assert.Contains(Root, E => E.ShortName == "dir");
            Assert.Contains(Root, E => E.ShortName == "link");

            Assert.Equal("hello", Encoding.ASCII.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, Reader.Read("/dir/data.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));

            var Dir = Reader.ReadDir("/dir");
            Assert.Single(Dir);
            Assert.Equal("data.bin", Dir[0].ShortName);
            Assert.Equal(12u, Dir[0].User);
            Assert.Equal(34u, Dir[0].Group);

            var Linux = new LinuxFilesystem();
            new NyaFs.ImageFormat.Elements.Fs.Reader.Iso9660Reader(Image).ReadToFs(Linux);
            Assert.Equal(NyaFs.ImageFormat.Types.FsType.Iso9660, Linux.FilesystemType);
            Assert.NotNull(Linux.Fs.GetElement("/hello.txt"));
            Assert.NotNull(Linux.Fs.GetElement("/dir/data.bin"));
        }
    }
}
