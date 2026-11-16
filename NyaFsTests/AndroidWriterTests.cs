using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.IO;
using Xunit;

namespace NyaFsTests
{
    public class AndroidWriterTests
    {
        [Fact]
        public void Android_Boot_V3_Can_Be_Stored_And_Loaded()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                Root = @"F:\TEMP\Images";

            var Source = Path.Combine(Root, "android", "boot.fex");
            if (!File.Exists(Source))
                return;

            var Target = Path.Combine(Path.GetTempPath(), "nya-android-v3-" + Guid.NewGuid().ToString("N") + ".img");
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[]
            {
                $"load \"{Source}\"",
                "set container android.header-version 3",
                $"store \"{Target}\" all android"
            }));

            var Data = File.ReadAllBytes(Target);
            Assert.Equal(3u, BitConverter.ToUInt32(Data, 0x28));
            Assert.Equal(1580u, BitConverter.ToUInt32(Data, 0x14));

            var ReadParser = new ScriptParser(new ScriptBaseAll());
            var ReadProc = new ImageProcessor(ReadParser, new string[0]);
            ReadProc.Process(ReadParser.ParseScript("t", new[] { $"load \"{Target}\"" }));
            Assert.True(ReadProc.GetKernel().Loaded);
            Assert.True(ReadProc.GetFs().Loaded);
        }
        [Fact]
        public void Android_Boot_V4_Can_Be_Stored_And_Loaded()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                Root = @"F:\TEMP\Images";

            var Source = Path.Combine(Root, "android", "boot.fex");
            if (!File.Exists(Source))
                return;

            var Target = Path.Combine(Path.GetTempPath(), "nya-android-v4-" + Guid.NewGuid().ToString("N") + ".img");
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[]
            {
                $"load \"{Source}\"",
                "set container android.header-version 4",
                $"store \"{Target}\" all android"
            }));

            var Data = File.ReadAllBytes(Target);
            Assert.Equal(4u, BitConverter.ToUInt32(Data, 0x28));
            Assert.Equal(1584u, BitConverter.ToUInt32(Data, 0x14));
            Assert.Equal(0u, BitConverter.ToUInt32(Data, 1580));

            var ReadParser = new ScriptParser(new ScriptBaseAll());
            var ReadProc = new ImageProcessor(ReadParser, new string[0]);
            ReadProc.Process(ReadParser.ParseScript("t", new[] { $"load \"{Target}\"" }));
            Assert.True(ReadProc.GetKernel().Loaded);
            Assert.True(ReadProc.GetFs().Loaded);
        }

        [Fact]
        public void Android_Boot_V4_Signature_RoundTrips()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                Root = @"F:\TEMP\Images";

            var Source = Path.Combine(Root, "android", "boot.fex");
            if (!File.Exists(Source))
                return;

            var Signature = new byte[4096];
            for (var I = 0; I < Signature.Length; I++)
                Signature[I] = (byte)(I & 0xFF);

            var First = Path.Combine(Path.GetTempPath(), "nya-android-v4sig-a-" + Guid.NewGuid().ToString("N") + ".img");
            var Second = Path.Combine(Path.GetTempPath(), "nya-android-v4sig-b-" + Guid.NewGuid().ToString("N") + ".img");

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[] { $"load \"{Source}\"" }));

            var Blob = Proc.GetBlob();
            Blob.SetContainerOption("android", "header-version", "4");
            var Plan = new NyaFs.ImageFormat.ImageBuildPlan(new NyaFs.ImageFormat.ImageBuildNode
            {
                Name = "android",
                Container = "android",
                Format = "android",
                Kind = NyaFs.ImageFormat.ImageKind.Unknown
            });
            Plan.Root.Children.Add(new NyaFs.ImageFormat.ImageBuildNode
            {
                Name = "boot_signature",
                Path = "boot_signature",
                Kind = NyaFs.ImageFormat.ImageKind.Unknown,
                Raw = Signature
            });
            Blob.BuildPlan = Plan;

            var Written = new NyaFs.ImageFormat.Composite.AndroidImageWriter(First).Build(Blob, out var Error);
            Assert.Null(Error);
            Assert.NotNull(Written);
            File.WriteAllBytes(First, Written);
            Assert.Equal(4u, BitConverter.ToUInt32(Written, 0x28));
            Assert.Equal(1584u, BitConverter.ToUInt32(Written, 0x14));
            Assert.Equal(4096u, BitConverter.ToUInt32(Written, 1580));

            var ReadParser = new ScriptParser(new ScriptBaseAll());
            var ReadProc = new ImageProcessor(ReadParser, new string[0]);
            ReadProc.Process(ReadParser.ParseScript("t", new[]
            {
                $"load \"{First}\"",
                $"store \"{Second}\" all android"
            }));

            Assert.True(ReadProc.GetKernel().Loaded);
            Assert.True(ReadProc.GetFs().Loaded);
            Assert.NotNull(ReadProc.GetBlob().BuildPlan.Find("boot_signature"));

            var Rewritten = File.ReadAllBytes(Second);
            Assert.Equal(4096u, BitConverter.ToUInt32(Rewritten, 1580));
            var KernelSize = (int)BitConverter.ToUInt32(Rewritten, 0x08);
            var RamdiskSize = (int)BitConverter.ToUInt32(Rewritten, 0x0C);
            var RamdiskOffset = ((4096 + KernelSize + 4095) / 4096) * 4096;
            var SignatureOffset = ((RamdiskOffset + RamdiskSize + 4095) / 4096) * 4096;
            Assert.Equal(Signature, Rewritten.AsSpan(SignatureOffset, Signature.Length).ToArray());
        }

        [Fact]
        public void Android_Boot_Image_Can_Be_Stored_And_Loaded()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root))
                Root = @"F:\TEMP\Images";

            var Source = Path.Combine(Root, "android", "boot.fex");
            if (!File.Exists(Source))
                return;

            var Target = Path.Combine(Path.GetTempPath(), "nya-android-" + Guid.NewGuid().ToString("N") + ".img");
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[]
            {
                $"load \"{Source}\"",
                $"store \"{Target}\" all android"
            }));

            Assert.True(File.Exists(Target), "Android image was not written");
            var Data = File.ReadAllBytes(Target);
            Assert.Equal("ANDROID!", System.Text.Encoding.ASCII.GetString(Data, 0, 8));
            Assert.Equal(2048u, BitConverter.ToUInt32(Data, 0x24));

            var ReadParser = new ScriptParser(new ScriptBaseAll());
            var ReadProc = new ImageProcessor(ReadParser, new string[0]);
            ReadProc.Process(ReadParser.ParseScript("t", new[] { $"load \"{Target}\"" }));
            Assert.NotNull(ReadProc.GetKernel());
            Assert.True(ReadProc.GetKernel().Loaded);
            Assert.NotNull(ReadProc.GetFs());
            Assert.True(ReadProc.GetFs().Loaded);
        }
    }
}