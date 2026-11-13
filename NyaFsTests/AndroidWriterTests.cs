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