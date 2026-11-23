using NyaFs.ImageFormat;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAncientFs;
using System;
using System.IO;
using System.IO.Compression;
using Xunit;

namespace NyaFsTests
{
    public class DiskCopy42Tests
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

        private static byte[] WrapDiskCopy42(byte[] Raw, string Name)
        {
            const int HeaderSize = 84;
            const int TagSize = 9600;
            var Result = new byte[HeaderSize + Raw.Length + TagSize];

            var NameBytes = System.Text.Encoding.ASCII.GetBytes(Name);
            Result[0] = (byte)Math.Min(NameBytes.Length, 63);
            Array.Copy(NameBytes, 0, Result, 1, Result[0]);

            WriteBe32(Result, 0x40, (uint)Raw.Length);
            WriteBe32(Result, 0x44, TagSize);
            Array.Copy(Raw, 0, Result, HeaderSize, Raw.Length);
            return Result;
        }

        private static void WriteBe32(byte[] Data, int Offset, uint Value)
        {
            Data[Offset] = (byte)(Value >> 24);
            Data[Offset + 1] = (byte)(Value >> 16);
            Data[Offset + 2] = (byte)(Value >> 8);
            Data[Offset + 3] = (byte)Value;
        }

        [Fact]
        public void DiskCopy42_Unwraps_Mfs()
        {
            var Raw = ReadFixture("mfs_sample.img.gz");
            if (Raw == null)
                return;

            var Image = WrapDiskCopy42(Raw, "MFS Test");
            var Plugin = new DiskCopy42ImagePlugin();
            Assert.True(Plugin.DetectImage(new ImageData(Image)));

            var Images = Plugin.GetImages(new ImageData(Image));
            Assert.Single(Images);
            Assert.Equal(Raw.Length, Images[0].Content.Length);
            Assert.Equal(Raw, Images[0].Content);
        }

        [Fact]
        public void DiskCopy42_Loads_Mfs_Via_Pipeline()
        {
            var Raw = ReadFixture("mfs_sample.img.gz");
            if (Raw == null)
                return;

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_mfs_dc42.image");
            File.WriteAllBytes(Path, WrapDiskCopy42(Raw, "MFS Test"));
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new DiskCopy42ImagePlugin());
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
                NyaFs.ImageFormat.Plugins.PluginHost.SetCurrent(Previous);
            }
        }
    }
}
