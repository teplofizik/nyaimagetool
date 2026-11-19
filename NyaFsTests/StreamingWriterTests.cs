using System;
using System.IO;
using System.Text;
using NyaFs.Filesystem.EroFs;
using NyaFs.Filesystem.ExFat;
using NyaFs.Filesystem.F2fs;
using NyaFs.Filesystem.Jffs2;
using NyaFs.Filesystem.UbiFs;
using NyaFs.Filesystem.UbiFs.Ubifs;
using NyaFs.Filesystem.Udf;
using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Yaffs2;
using Xunit;

namespace NyaFsTests
{
    public class StreamingWriterTests
    {
        private const int PebSize = 0x20000;
        private const int PageSize = 0x800;
        private const int LebSize = PebSize - 2 * PageSize;

        private static void Fill(IFilesystemBuilder B)
        {
            B.Directory("/", 0, 0, 0755);
            B.Directory("/etc", 100, 200, 0755);
            B.File("/etc/config", Encoding.UTF8.GetBytes("hello streaming " + new string('X', 5000)), 10, 20, 0644);
            B.SymLink("/etc/link", "config", 0, 0, 0777);
        }

        private static byte[] BuildInMemory(IFilesystemBuilder B)
        {
            Fill(B);
            return B.GetFilesystemImage();
        }

        private static byte[] BuildStream(IFilesystemStreamBuilder B, bool Seekable)
        {
            Fill((IFilesystemBuilder)B);

            var Buffer = new MemoryStream();
            if (Seekable)
            {
                B.WriteFilesystemImage(Buffer);
            }
            else
            {
                using (var Sink = new NonSeekableStream(Buffer))
                    B.WriteFilesystemImage(Sink);
            }

            return Buffer.ToArray();
        }

        [Fact]
        public void Ubifs_Raw_StreamMatchesImage()
        {
            var A = BuildInMemory(new UbifsBuilder(LebSize, 1, PageSize));
            var B = BuildStream(new UbifsBuilder(LebSize, 1, PageSize), true);
            var C = BuildStream(new UbifsBuilder(LebSize, 1, PageSize), false);

            Assert.Equal(A, B);
            Assert.Equal(A, C);
        }

        [Fact]
        public void Ubifs_Ubi_StreamMatchesImage()
        {
            var A = BuildInMemory(new UbiFsBuilder(new UbifsBuilder(LebSize, 1, PageSize), PebSize, PageSize));
            var B = BuildStream(new UbiFsBuilder(new UbifsBuilder(LebSize, 1, PageSize), PebSize, PageSize), true);
            var C = BuildStream(new UbiFsBuilder(new UbifsBuilder(LebSize, 1, PageSize), PebSize, PageSize), false);

            Assert.Equal(A, B);
            Assert.Equal(A, C);
        }

        [Fact]
        public void F2fs_StreamMatchesImage()
        {
            const long Size = 64L * 1024 * 1024;

            var A = BuildInMemory(new F2fsFsBuilder(Size));
            var B = BuildStream(new F2fsFsBuilder(Size), true);
            var C = BuildStream(new F2fsFsBuilder(Size), false);

            Assert.Equal(A, B);
            Assert.Equal(A, C);
        }

        [Fact]
        public void EroFs_StreamMatchesImage()
        {
            var A = BuildInMemory(new EroFsBuilder());
            var B = BuildStream(new EroFsBuilder(), true);
            var C = BuildStream(new EroFsBuilder(), false);

            Assert.Equal(A, B);
            Assert.Equal(A, C);
        }

        [Theory]
        [InlineData("exfat")]
        [InlineData("jffs2")]
        [InlineData("udf")]
        [InlineData("yaffs2")]
        public void TimestampedFs_NonSeekableMatchesLength(string Format)
        {
            IFilesystemBuilder A, B;
            switch (Format)
            {
                case "exfat": A = new ExFatBuilder(64L * 1024 * 1024); B = new ExFatBuilder(64L * 1024 * 1024); break;
                case "jffs2": A = new Jffs2Builder(); B = new Jffs2Builder(); break;
                case "udf": A = new UdfBuilder(); B = new UdfBuilder(); break;
                default: A = new Yaffs2Builder(); B = new Yaffs2Builder(); break;
            }

            var Image = BuildInMemory(A);
            var Stream = BuildStream((IFilesystemStreamBuilder)B, false);

            Assert.NotEmpty(Image);
            Assert.Equal(Image.Length, Stream.Length);
        }

        /// <summary>A write-only stream that reports CanSeek=false (like a pipe/socket).</summary>
        private sealed class NonSeekableStream : Stream
        {
            private readonly Stream Target;

            public NonSeekableStream(Stream Target) => this.Target = Target;

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => Target.Flush();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => Target.Write(buffer, offset, count);
        }
    }
}
