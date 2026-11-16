using System;
using System.IO;
using NyaFs.ImageFormat;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsUboot;
using Xunit;

namespace NyaFsTests
{
    public class UbootEnvImageTests
    {
        private static byte[] SampleEnvironment()
        {
            var Env = new UbootEnvironment { RegionSize = 0x1000 };
            Env.Set("bootdelay", "0");
            Env.Set("bootcmd", "run boot_normal");
            return Env.Build(out _);
        }

        private static (ImageProcessor Proc, ScriptParser Parser) CreateProcessor()
        {
            var Base = new ScriptBaseAll();
            Base.Add(new NyaFsUboot.Commands.Env());
            var Parser = new ScriptParser(Base);
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Plugins.Load(new NyaFsUboot.UbootEnvImagePlugin());
            return (Proc, Parser);
        }

        [Fact]
        public void Env_Is_Detected_And_Loaded_As_Image()
        {
            var Data = SampleEnvironment();
            var (Proc, _) = CreateProcessor();

            var Composite = Proc.Plugins.DetectComposite(new ImageData(Data));
            Assert.NotNull(Composite);
            Assert.Equal("env", Composite.Format);

            var Blob = new BaseImageBlob();
            Assert.True(new ImagePipeline(Proc.Plugins, Blob).Load(Data));
            Assert.NotNull(Blob.BuildPlan);
            Assert.Equal("env", Blob.BuildPlan.Root.Format);
        }

        [Fact]
        public void Env_Edit_After_Load_Is_Stored()
        {
            var Source = Path.Combine(Path.GetTempPath(), "nya-env-load-" + Guid.NewGuid().ToString("N") + ".bin");
            var Target = Path.Combine(Path.GetTempPath(), "nya-env-store-" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllBytes(Source, SampleEnvironment());

            var (Proc, Parser) = CreateProcessor();
            Proc.Process(Parser.ParseScript("t", new[]
            {
                $"load \"{Source}\"",
                "env set bootdelay 9",
                $"store \"{Target}\" all env"
            }));

            Assert.True(File.Exists(Target));
            Assert.True(UbootEnvironment.TryParse(File.ReadAllBytes(Target), out var Env, out var Error), Error);
            Assert.Equal("9", Env.Get("bootdelay"));
            Assert.Equal("run boot_normal", Env.Get("bootcmd"));
        }

        [Fact]
        public void Env_Untouched_Load_Store_Is_Byte_Identical()
        {
            var Source = Path.Combine(Path.GetTempPath(), "nya-env-same-" + Guid.NewGuid().ToString("N") + ".bin");
            var Target = Path.Combine(Path.GetTempPath(), "nya-env-same-out-" + Guid.NewGuid().ToString("N") + ".bin");
            var Data = SampleEnvironment();
            File.WriteAllBytes(Source, Data);

            var (Proc, Parser) = CreateProcessor();
            Proc.Process(Parser.ParseScript("t", new[] { $"load \"{Source}\"", $"store \"{Target}\"" }));

            Assert.True(File.Exists(Target));
            Assert.Equal(Data, File.ReadAllBytes(Target));
        }
    }
}
