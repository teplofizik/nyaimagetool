using System;
using System.IO;
using System.Text;
using NyaFs.FlattenedDeviceTree;
using NyaFs.FlattenedDeviceTree.Types;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Composite;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using Xunit;

namespace NyaFsTests
{
    public class FitScriptTests
    {
        private static byte[] BuildScriptFit(string ImageName, string ScriptText)
        {
            var Fit = new NyaFs.FlattenedDeviceTree.FlattenedDeviceTree();
            Fit.Root.AddStringValue("description", "script test");

            var Images = new Node("images");
            var Script = new Node(ImageName);
            Script.AddStringValue("description", "U-Boot script");
            Script.AddRawValue("data", Encoding.ASCII.GetBytes(ScriptText));
            Script.AddStringValue("type", "script");
            Script.AddStringValue("compression", "none");
            Script.AddStringValue("arch", "arm");
            Script.AddStringValue("os", "linux");
            Images.Nodes.Add(Script);

            var Configurations = new Node("configurations");
            Configurations.AddStringValue("default", "conf-1");
            var Config = new Node("conf-1");
            Config.AddStringValue("description", "script config");
            Config.AddStringValue("script", ImageName);
            Configurations.Nodes.Add(Config);

            Fit.Root.Nodes.Add(Images);
            Fit.Root.Nodes.Add(Configurations);
            return new NyaFs.FlattenedDeviceTree.Writer.FDTWriter(Fit).GetBinary();
        }

        private static ScriptPlugins Plugins()
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            return Proc.Plugins;
        }

        [Fact]
        public void FitScript_IsDetected_And_Listed()
        {
            var Data = BuildScriptFit("script-1", "echo hello\n");

            var Composite = Plugins().DetectComposite(new ImageData(Data));
            Assert.NotNull(Composite);
            Assert.Equal("fit", Composite.Format);

            var Images = Composite.GetImages(new ImageData(Data));
            Assert.Single(Images);
            Assert.Equal("script-1", Images[0].Name);
            Assert.Equal(ImageKind.Unknown, Images[0].Kind);
            Assert.Equal("script", Images[0].Format);
            Assert.Equal("echo hello\n", Encoding.ASCII.GetString(Images[0].Content));
        }

        [Fact]
        public void FitScript_StoreAllFit_PreservesScript()
        {
            var Data = BuildScriptFit("script-1", "echo hello\n");
            var Source = Path.Combine(Path.GetTempPath(), "nyafs-fit-script-in-" + Guid.NewGuid().ToString("N") + ".itb");
            var Target = Path.Combine(Path.GetTempPath(), "nyafs-fit-script-out-" + Guid.NewGuid().ToString("N") + ".itb");
            File.WriteAllBytes(Source, Data);

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Process(Parser.ParseScript("t", new[]
            {
                $"load \"{Source}\"",
                $"store \"{Target}\" all fit"
            }));

            Assert.True(File.Exists(Target));
            var Rebuilt = File.ReadAllBytes(Target);
            var Composite = Plugins().DetectComposite(new ImageData(Rebuilt));
            Assert.NotNull(Composite);
            var Images = Composite.GetImages(new ImageData(Rebuilt));
            Assert.Single(Images);
            Assert.Equal("script-1", Images[0].Name);
            Assert.Equal("script", Images[0].Format);
            Assert.Equal("echo hello\n", Encoding.ASCII.GetString(Images[0].Content));
        }
        [Fact]
        public void FitScript_IsLoaded_And_RoundTripped()
        {
            var Data = BuildScriptFit("script-1", "echo hello\n");

            var Blob = new BaseImageBlob();
            Assert.True(new ImagePipeline(Plugins(), Blob).Load(Data));
            Assert.NotNull(Blob.BuildPlan);
            Assert.NotNull(Blob.BuildPlan.Find("script-1"));

            var Rebuilt = new ImageBuildPlanWriter(Blob.BuildPlan).Build(out var Error);
            Assert.Null(Error);
            Assert.NotNull(Rebuilt);

            var Composite = Plugins().DetectComposite(new ImageData(Rebuilt));
            Assert.NotNull(Composite);
            var Images = Composite.GetImages(new ImageData(Rebuilt));
            Assert.Single(Images);
            Assert.Equal("script-1", Images[0].Name);
            Assert.Equal("script", Images[0].Format);
            Assert.Equal("echo hello\n", Encoding.ASCII.GetString(Images[0].Content));
        }
    }
}
