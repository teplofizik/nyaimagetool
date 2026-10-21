using NyaFs.Filesystem.CramFs;
using NyaFs.ImageFormat.Elements.Fs;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class CramFsEndianTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CramFs_RoundTrip(bool BigEndian)
        {
            var Builder = new CramFsBuilder(BigEndian);
            Builder.Directory("/", 0, 0, 0x1ED);
            Builder.File("/hello.txt", Encoding.ASCII.GetBytes("hello"), 0, 0, 0x1A4);
            Builder.Directory("/dir", 0, 0, 0x1ED);
            Builder.File("/dir/data.bin", new byte[] { 1, 2, 3, 4 }, 12, 34, 0x1A0);
            Builder.SymLink("/link", "hello.txt", 0, 0, 0x1FF);

            var Large = new byte[10000];
            for (int i = 0; i < Large.Length; i++)
                Large[i] = (byte)(i * 37);
            Builder.File("/large.bin", Large, 0, 0, 0x1A4);

            var Image = Builder.GetFilesystemImage();

            if (BigEndian)
            {
                Assert.Equal(0x28, Image[0]);
                Assert.Equal(0xCD, Image[1]);
                Assert.Equal(0x3D, Image[2]);
                Assert.Equal(0x45, Image[3]);
            }
            else
            {
                Assert.Equal(0x45, Image[0]);
                Assert.Equal(0x3D, Image[1]);
                Assert.Equal(0xCD, Image[2]);
                Assert.Equal(0x28, Image[3]);
            }

            Assert.Equal(NyaFs.ImageFormat.Types.FsType.CramFs, FilesystemDetector.DetectFs(Image));

            var Reader = new CramFsReader(Image);
            Assert.Equal(BigEndian, Reader.BigEndian);

            var Root = Reader.ReadDir("/");
            Assert.Contains(Root, E => E.ShortName == "hello.txt");
            Assert.Contains(Root, E => E.ShortName == "dir");
            Assert.Contains(Root, E => E.ShortName == "link");

            Assert.Equal("hello", Encoding.ASCII.GetString(Reader.Read("/hello.txt")));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, Reader.Read("/dir/data.bin"));
            Assert.Equal(Large, Reader.Read("/large.bin"));
            Assert.Equal("hello.txt", Reader.ReadLink("/link"));

            var Linux = new LinuxFilesystem();
            new NyaFs.ImageFormat.Elements.Fs.Reader.CramFsReader(Image).ReadToFs(Linux);
            Assert.Equal(BigEndian, Linux.CramFsBigEndian);

            var Writer = new NyaFs.ImageFormat.Elements.Fs.Writer.CramFsWriter(Linux.CramFsBigEndian);
            Writer.WriteFs(Linux);
            Assert.True(Writer.HasRawStreamData);
            Assert.Equal(BigEndian ? (byte)0x28 : (byte)0x45, Writer.RawStream[0]);

            var Dir = Reader.ReadDir("/dir");
            Assert.Single(Dir);
            Assert.Equal("data.bin", Dir[0].ShortName);
            Assert.Equal(12u, Dir[0].User);
            Assert.Equal(34u, Dir[0].Group);
        }
    }
}
