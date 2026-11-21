using NyaFs.ImageFormat;
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
    public class StuffItTests
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

        private static string Corpus(string Rel)
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\Images";
            var Relative = Rel.Replace('/', System.IO.Path.DirectorySeparatorChar);
            var Path = System.IO.Path.Combine(Root, Relative);
            if (File.Exists(Path))
                return Path;

            var Local = System.IO.Path.Combine(@"F:\TEMP\mac_samples",
                Relative.StartsWith("mac" + System.IO.Path.DirectorySeparatorChar)
                    ? Relative.Substring(4)
                    : Relative);
            return File.Exists(Local) ? Local : null;
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

        private static byte[] WrapStuffIt(byte[] Content, string Name)
        {
            const int HeaderSize = 22;
            const int EntrySize = 112;
            var Result = new byte[HeaderSize + EntrySize + Content.Length];
            WriteBe32(Result, 0, 0x53495421u); // SIT!
            WriteBe16(Result, 4, 1);
            WriteBe32(Result, 6, (uint)Result.Length);
            WriteBe32(Result, 10, 0x724C6175u); // rLau
            Result[20] = 1;

            int E = HeaderSize;
            var NameBytes = System.Text.Encoding.ASCII.GetBytes(Name);
            Result[E] = 0; // resource method
            Result[E + 1] = 0; // data method: stored
            Result[E + 2] = (byte)Math.Min(NameBytes.Length, 31);
            Array.Copy(NameBytes, 0, Result, E + 3, Result[E + 2]);
            WriteBe32(Result, E + 84, 0);
            WriteBe32(Result, E + 88, (uint)Content.Length);
            WriteBe32(Result, E + 92, 0);
            WriteBe32(Result, E + 96, (uint)Content.Length);
            Array.Copy(Content, 0, Result, HeaderSize + EntrySize, Content.Length);
            return Result;
        }

        private static void WriteBe16(byte[] Data, int Offset, int Value)
        {
            Data[Offset] = (byte)(Value >> 8);
            Data[Offset + 1] = (byte)Value;
        }

        private static void WriteBe32(byte[] Data, int Offset, uint Value)
        {
            Data[Offset] = (byte)(Value >> 24);
            Data[Offset + 1] = (byte)(Value >> 16);
            Data[Offset + 2] = (byte)(Value >> 8);
            Data[Offset + 3] = (byte)Value;
        }

        [Fact]
        public void StuffIt_ReadRealSystemDisk()
        {
            var Path = Corpus("mac/System11.sit");
            if (Path == null)
                return;

            var Items = new StuffItArchive(File.ReadAllBytes(Path)).ReadItems();
            Assert.Single(Items);
            Assert.Equal(419284, Items[0].Data.Length);
            Assert.Equal("db478efb823691ed4826792709fb923003f42ba797b5d5b5ec6444df19f754ac",
                Convert.ToHexString(SHA256.HashData(Items[0].Data)).ToLowerInvariant());
        }

        [Fact]
        public void StuffIt_StoredArchive_Loads_DiskCopy_Mfs()
        {
            var Raw = ReadFixture("mfs_sample.img.gz");
            if (Raw == null)
                return;

            var Archive = WrapStuffIt(WrapDiskCopy42(Raw, "MFS Test"), "MFS Test.image");
            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs_mfs_sit.sit");
            File.WriteAllBytes(Path, Archive);
            var Previous = NyaFs.ImageFormat.Plugins.PluginHost.Current;
            try
            {
                var Parser = new ScriptParser(new ScriptBaseAll());
                var Proc = new ImageProcessor(Parser, new string[0]);
                Proc.Plugins.Load(new StuffItImagePlugin());
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
