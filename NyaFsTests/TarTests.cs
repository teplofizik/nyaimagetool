using System;
using System.Linq;
using System.Text;
using NyaFs.Filesystem.Tar;
using NyaFs.ImageFormat.Types;
using Xunit;

namespace NyaFsTests
{
    public class TarTests
    {
        [Fact]
        public void Tar_FileContent_RoundTrip()
        {
            var content = Encoding.UTF8.GetBytes("hello tar content " + string.Concat(Enumerable.Repeat("xyz", 500)));

            var b = new TarFsBuilder();
            b.Directory("/etc", 0, 0, 493);
            b.File("/etc/test.txt", content, 0, 0, 420);

            var r = new TarFsReader(b.GetFilesystemImage());

            Assert.Equal(content, r.Read("/etc/test.txt"));
        }

        [Fact]
        public void Tar_Detected_As_Tar()
        {
            var b = new TarFsBuilder();
            b.File("/a", new byte[] { 1, 2, 3 }, 0, 0, 420);

            var image = b.GetFilesystemImage();

            Assert.Equal(FsType.Tar, NyaFs.ImageFormat.Elements.Fs.FilesystemDetector.DetectFs(image));
        }

        [Fact]
        public void Tar_Ustar_Magic_At_Offset_257()
        {
            var b = new TarFsBuilder();
            b.File("/a", new byte[] { 1 }, 0, 0, 420);

            var image = b.GetFilesystemImage();

            Assert.Equal((byte)'u', image[257]);
            Assert.Equal((byte)'s', image[258]);
            Assert.Equal((byte)'t', image[259]);
            Assert.Equal((byte)'a', image[260]);
            Assert.Equal((byte)'r', image[261]);
        }

        [Fact]
        public void Tar_SymLink_RoundTrip()
        {
            var b = new TarFsBuilder();
            b.SymLink("/usr/lib/libfoo.so", "libfoo.so.1", 0, 0, 511);

            var r = new TarFsReader(b.GetFilesystemImage());

            Assert.Equal("libfoo.so.1", r.ReadLink("/usr/lib/libfoo.so"));
        }

        [Fact]
        public void Tar_CharDevice_RoundTrip()
        {
            var b = new TarFsBuilder();
            b.Directory("/dev", 0, 0, 493);
            b.Char("/dev/ttyS0", 4, 64, 0, 0, 438);

            var r = new TarFsReader(b.GetFilesystemImage());

            var dev = r.ReadDevice("/dev/ttyS0");
            Assert.NotNull(dev);
            Assert.Equal(4u, dev.Major);
            Assert.Equal(64u, dev.Minor);
        }

        [Fact]
        public void Tar_BlockDevice_RoundTrip()
        {
            var b = new TarFsBuilder();
            b.Directory("/dev", 0, 0, 493);
            b.Block("/dev/mmcblk0", 179, 0, 0, 0, 438);

            var r = new TarFsReader(b.GetFilesystemImage());

            var dev = r.ReadDevice("/dev/mmcblk0");
            Assert.NotNull(dev);
            Assert.Equal(179u, dev.Major);
            Assert.Equal(0u, dev.Minor);
        }

        [Fact]
        public void Tar_LongName_RoundTrip()
        {
            var longDir = string.Join("/", Enumerable.Repeat("verylongdirectoryname", 8));
            var path = "/" + longDir + "/file.txt";
            Assert.True(path.Length > 100);

            var content = Encoding.ASCII.GetBytes("long path payload");

            var b = new TarFsBuilder();
            b.File(path, content, 0, 0, 420);

            var r = new TarFsReader(b.GetFilesystemImage());

            Assert.Equal(content, r.Read(path));
        }

        [Fact]
        public void Tar_ReadDir_Lists_Direct_Children()
        {
            var b = new TarFsBuilder();
            b.Directory("/etc", 0, 0, 493);
            b.File("/etc/a.conf", new byte[] { 1 }, 0, 0, 420);
            b.File("/etc/b.conf", new byte[] { 2 }, 0, 0, 420);
            b.File("/top.txt", new byte[] { 3 }, 0, 0, 420);

            var r = new TarFsReader(b.GetFilesystemImage());

            var root = r.ReadDir("/").Select(e => e.Path).OrderBy(p => p).ToArray();
            Assert.Equal(new[] { "etc", "top.txt" }, root);

            var etc = r.ReadDir("/etc").Select(e => e.Path).OrderBy(p => p).ToArray();
            Assert.Equal(new[] { "etc/a.conf", "etc/b.conf" }, etc);
        }

        [Fact]
        public void Tar_Mode_And_Owner_RoundTrip()
        {
            var b = new TarFsBuilder();
            b.File("/f", new byte[] { 1 }, 7, 9, 0x1A4);

            var r = new TarFsReader(b.GetFilesystemImage());
            var e = r.ReadDir("/").Single();

            Assert.Equal(7u, e.User);
            Assert.Equal(9u, e.Group);
            Assert.Equal(0x1A4u, e.HexMode);
        }

        [Fact]
        public void Tar_Full_Filesystem_RoundTrip()
        {
            var src = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
            src.Fs.Root.Items.Add(new NyaFs.Filesystem.Universal.Items.Dir("etc", 0, 0, 493));
            src.Fs.GetDirectory("etc").Items.Add(
                new NyaFs.Filesystem.Universal.Items.File("etc/hostname", 0, 0, 420, Encoding.ASCII.GetBytes("nyafs")));
            src.Fs.Root.Items.Add(new NyaFs.Filesystem.Universal.Items.SymLink("link", 0, 0, 511, "etc/hostname"));

            var writer = new NyaFs.ImageFormat.Elements.Fs.Writer.BaseFsWriter(new TarFsBuilder());
            writer.WriteFs(src);
            var image = writer.RawStream;

            var dst = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
            var reader = new NyaFs.ImageFormat.Elements.Fs.Reader.BaseFsReader(FsType.Tar, new TarFsReader(image));
            reader.ReadToFs(dst);

            Assert.Equal(FsType.Tar, dst.FilesystemType);
            Assert.Equal(Encoding.ASCII.GetBytes("nyafs"),
                ((NyaFs.Filesystem.Universal.Items.File)dst.GetElement("etc/hostname")).Content);
            Assert.Equal("etc/hostname",
                ((NyaFs.Filesystem.Universal.Items.SymLink)dst.GetElement("link")).Target);
        }
    }
}
