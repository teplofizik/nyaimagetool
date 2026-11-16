using System;
using System.IO;
using System.Text;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Types;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsUboot;
using Xunit;

namespace NyaFsTests
{
    public class UbootFormatTests
    {
        private static ScriptPlugins Plugins()
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            return Proc.Plugins;
        }

        private static void WriteU64LE(byte[] Data, int Offset, ulong Value)
        {
            for (var I = 0; I < 8; I++)
                Data[Offset + I] = (byte)(Value >> (8 * I));
        }

        private static byte[] Arm64Image(byte[] Payload, ulong Flags = 0x0A)
        {
            var Data = new byte[0x40 + Payload.Length];
            Data[0] = 0x1F; Data[1] = 0x20; Data[2] = 0x03; Data[3] = 0xD5;
            WriteU64LE(Data, 0x10, (ulong)Payload.Length);
            WriteU64LE(Data, 0x18, Flags);
            Data[0x38] = 0x41; Data[0x39] = 0x52; Data[0x3A] = 0x4D; Data[0x3B] = 0x64;
            Buffer.BlockCopy(Payload, 0, Data, 0x40, Payload.Length);
            return Data;
        }

        private static byte[] Legacy(ImageType Type, byte[] Payload)
        {
            var Info = new ImageInfo
            {
                Type = Type,
                Architecture = CPU.IH_ARCH_ARM,
                OperatingSystem = OS.IH_OS_LINUX,
                Name = "test"
            };
            return new LegacyImage(Info, CompressionType.IH_COMP_NONE, Payload).getPacket();
        }

        [Fact]
        public void LinuxArm64Image_IsDetected_As_Kernel()
        {
            var Data = Arm64Image(new byte[] { 1, 2, 3, 4 });

            var Detected = Plugins().DetectImage(new ImageData(Data));

            Assert.Equal(ImageKind.Kernel, Detected.Kind);
            Assert.Equal("image", Detected.Format);
        }

        [Fact]
        public void LinuxArm64Image_Load_Preserves_Payload()
        {
            var Payload = Encoding.ASCII.GetBytes("kernel payload");
            var Data = Arm64Image(Payload);

            var Blob = new BaseImageBlob();
            var Loaded = new ImagePipeline(Plugins(), Blob).Load(Data);

            Assert.True(Loaded);
            Assert.NotNull(Blob.GetKernel(0));
            Assert.Equal(Data.Length, Blob.GetKernel(0).Image.Length);
            Assert.Equal("image", Blob.GetKernel(0).KernelFormat);
            Assert.Equal(CPU.IH_ARCH_ARM64, Blob.GetKernel(0).Info.Architecture);
        }

        [Theory]
        [InlineData(ImageType.IH_TYPE_STANDALONE)]
        [InlineData(ImageType.IH_TYPE_FIRMWARE)]
        [InlineData(ImageType.IH_TYPE_SCRIPT)]
        public void LegacyOpaqueType_IsLoaded_And_Preserved(ImageType Type)
        {
            var Data = Legacy(Type, Encoding.ASCII.GetBytes("hello"));
            var Reg = Plugins();

            var Composite = Reg.DetectComposite(new ImageData(Data));
            Assert.NotNull(Composite);
            var Images = Composite.GetImages(new ImageData(Data));
            Assert.Single(Images);
            Assert.Equal(ImageKind.Unknown, Images[0].Kind);

            var Blob = new BaseImageBlob();
            var Loaded = new ImagePipeline(Reg, Blob).Load(Data);
            Assert.True(Loaded);
            Assert.Single(Blob.Unknown);
        }

        [Fact]
        public void LegacyKernelNoLoad_Is_Loaded_As_Kernel()
        {
            var Data = Legacy(ImageType.IH_TYPE_KERNEL_NOLOAD, new byte[] { 1, 2, 3 });
            var Reg = Plugins();

            var Composite = Reg.DetectComposite(new ImageData(Data));
            Assert.NotNull(Composite);
            Assert.NotNull(Composite.GetKernelReader(Data));

            var Blob = new BaseImageBlob();
            Assert.True(new ImagePipeline(Reg, Blob).Load(Data));
            Assert.NotNull(Blob.GetKernel(0));
        }

        [Fact]
        public void LegacyFilesystemType_Is_Loaded_As_Filesystem()
        {
            var Data = Legacy(ImageType.IH_TYPE_FILESYSTEM, new byte[] { 1, 2, 3 });
            var Reg = Plugins();

            var Composite = Reg.DetectComposite(new ImageData(Data));
            Assert.NotNull(Composite);
            Assert.NotNull(Composite.GetFilesystemReader(Data));
        }

        [Fact]
        public void UbootEnv_Crc32_Matches_Standard_Vector()
        {
            var Data = Encoding.ASCII.GetBytes("abc");
            Assert.Equal(0x352441C2u, UbootEnvironment.Crc32(Data, 0, Data.Length));
        }

        [Fact]
        public void UbootEnv_Build_Parse_RoundTrip()
        {
            var Env = new UbootEnvironment { RegionSize = 0x1000 };
            Env.Set("bootdelay", "0");
            Env.Set("bootcmd", "run boot_normal");
            Env.Set("empty", "");

            var Raw = Env.Build(out var Error);
            Assert.Null(Error);

            Assert.True(UbootEnvironment.TryParse(Raw, out var Parsed, out Error), Error);
            Assert.Equal(0x1000, Parsed.RegionSize);
            Assert.Equal(1, Parsed.Copies);
            Assert.Equal(4, Parsed.DataOffset);
            Assert.Equal("0", Parsed.Get("bootdelay"));
            Assert.Equal("run boot_normal", Parsed.Get("bootcmd"));
            Assert.Equal("", Parsed.Get("empty"));
        }

        [Fact]
        public void UbootEnv_Parses_FlagsByte_Layout()
        {
            var Env = new UbootEnvironment { RegionSize = 0x1000, DataOffset = 5, Flags = 0 };
            Env.Set("earlyprintk", "sunxi-uart,0x05000000");

            var Raw = Env.Build(out var Error);
            Assert.Null(Error);

            Assert.True(UbootEnvironment.TryParse(Raw, out var Parsed, out Error), Error);
            Assert.Equal(5, Parsed.DataOffset);
            Assert.Equal(0u, Parsed.Flags);
            Assert.Equal("sunxi-uart,0x05000000", Parsed.Get("earlyprintk"));
        }

        [Fact]
        public void UbootEnv_Parses_FlagsU32_Layout()
        {
            var Env = new UbootEnvironment { RegionSize = 0x1000, DataOffset = 8, Flags = 1 };
            Env.Set("bootcmd", "run boot");

            var Raw = Env.Build(out var Error);
            Assert.Null(Error);

            Assert.True(UbootEnvironment.TryParse(Raw, out var Parsed, out Error), Error);
            Assert.Equal(8, Parsed.DataOffset);
            Assert.Equal(1u, Parsed.Flags);
            Assert.Equal("run boot", Parsed.Get("bootcmd"));
        }

        [Fact]
        public void UbootEnv_Parses_Redundant_Pair()
        {
            var Env = new UbootEnvironment
            {
                RegionSize = 0x1000,
                Copies = 2,
                DataOffset = 5,
                CopyFlags = new uint[] { 1, 0 }
            };
            Env.Set("bootdelay", "2");

            var Raw = Env.Build(out var Error);
            Assert.Null(Error);
            Assert.Equal(0x2000, Raw.Length);

            Assert.True(UbootEnvironment.TryParse(Raw, out var Parsed, out Error), Error);
            Assert.Equal(0x1000, Parsed.RegionSize);
            Assert.Equal(2, Parsed.Copies);
            Assert.Equal(1u, Parsed.CopyFlags[0]);
            Assert.Equal(0u, Parsed.CopyFlags[1]);
            Assert.Equal("2", Parsed.Get("bootdelay"));
        }

        [Fact]
        public void UbootEnv_Text_Import_Export_RoundTrip()
        {
            var Env = new UbootEnvironment();
            Assert.True(Env.TryParseText("# comment\nbootdelay=1\nbootcmd=run boot\n", out var Error), Error);
            Assert.Equal(2, Env.Count);

            var Text = Env.ToText();
            var Again = new UbootEnvironment();
            Assert.True(Again.TryParseText(Text, out Error), Error);
            Assert.Equal("run boot", Again.Get("bootcmd"));
        }

        [Fact]
        public void EnvCommands_Create_Edit_And_Save()
        {
            var Base = new ScriptBaseAll();
            Base.Add(new NyaFsUboot.Commands.Env());
            var Parser = new ScriptParser(Base);
            var Proc = new ImageProcessor(Parser, new string[0]);

            var Target = Path.Combine(Path.GetTempPath(), "nya-env-" + Guid.NewGuid().ToString("N") + ".bin");
            try
            {
                Proc.Process(Parser.ParseScript("t", new[]
                {
                    "env new 4K",
                    "env set bootdelay 0",
                    "env set bootcmd \"run boot_normal\"",
                    "env save " + Target
                }));

                Assert.True(File.Exists(Target));
                Assert.True(UbootEnvironment.TryParse(File.ReadAllBytes(Target), out var Env, out var Error), Error);
                Assert.Equal(0x1000, Env.RegionSize);
                Assert.Equal("run boot_normal", Env.Get("bootcmd"));
            }
            finally
            {
                if (File.Exists(Target))
                    File.Delete(Target);
            }
        }
    }
}

