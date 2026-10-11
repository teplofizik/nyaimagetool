using System;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Commands.Fs;
using Xunit;

namespace NyaFsTests
{
    public class ScriptingTests
    {
        private class CaptureGenerator : ScriptStepGenerator
        {
            public string[] Captured;

            public CaptureGenerator() : base("cap")
            {
                AddConfig(new ScriptArgsConfig(0, null));
            }

            public override ScriptStep Get(ScriptArgs Args)
            {
                Captured = Args.RawArgs;
                return new NyaFs.Processor.Scripting.Commands.Echo.EchoScriptStep(Args.RawArgs);
            }
        }

        private static ImageProcessor ProcessorWithFile(out LinuxFilesystem fs)
        {
            fs = new LinuxFilesystem();
            fs.Fs.Root.Items.Add(new NyaFs.Filesystem.Universal.Items.File("f", 0, 0, 420, new byte[] { 1, 2, 3 }));

            var parser = new ScriptParser(new ScriptBaseAll());
            var proc = new ImageProcessor(parser, new string[0]);
            proc.SetFs(fs);
            return proc;
        }

        [Fact]
        public void Parser_Accepts_SelfContained_Quoted_Token()
        {
            var b = new ScriptBase();
            var cap = new CaptureGenerator();
            b.Add(cap);
            var parser = new ScriptParser(b);

            var script = parser.ParseScript("t", new[] { "cap \"hello\"" });

            Assert.False(script.HasErrors);
            Assert.Equal(new[] { "hello" }, cap.Captured);
        }

        [Fact]
        public void Parser_Accepts_Multiword_Quoted_Token()
        {
            var b = new ScriptBase();
            var cap = new CaptureGenerator();
            b.Add(cap);
            var parser = new ScriptParser(b);

            var script = parser.ParseScript("t", new[] { "cap \"hello world\"" });

            Assert.False(script.HasErrors);
            Assert.Equal(new[] { "hello world" }, cap.Captured);
        }

        [Fact]
        public void Parser_Rejects_Unterminated_Quote()
        {
            var b = new ScriptBase();
            var cap = new CaptureGenerator();
            b.Add(cap);
            var parser = new ScriptParser(b);

            var script = parser.ParseScript("t", new[] { "cap \"hello" });

            Assert.True(script.HasErrors);
        }

        [Fact]
        public void SLink_TwoArg_Form_Does_Not_Throw()
        {
            var step = new SLink().Get(new ScriptArgs(1, new[] { "link", "target" }));
            Assert.IsType<SLink.SLinkScriptStep>(step);
        }

        [Fact]
        public void SLink_TwoArg_Form_Executes()
        {
            var proc = ProcessorWithFile(out var fs);
            // Two-argument form is an "update" (symlink must already exist).
            fs.Fs.Root.Items.Add(new NyaFs.Filesystem.Universal.Items.SymLink("link", 0, 0, 511, "/old"));

            var step = new SLink().Get(new ScriptArgs(1, new[] { "link", "/target" }));

            var res = step.Exec(proc);

            Assert.Equal(ScriptStepStatus.Ok, res.Status);
            Assert.Equal("/target", ((NyaFs.Filesystem.Universal.Items.SymLink)fs.GetElement("link")).Target);
        }

        [Fact]
        public void SLink_TwoArg_Form_On_Missing_Path_Returns_Error_Not_Exception()
        {
            var proc = ProcessorWithFile(out var fs);
            var step = new SLink().Get(new ScriptArgs(1, new[] { "missing", "/target" }));

            var res = step.Exec(proc);

            Assert.Equal(ScriptStepStatus.Error, res.Status);
        }

        [Fact]
        public void Chown_ThreeArg_Form_Sets_Group_From_Third_Argument()
        {
            var proc = ProcessorWithFile(out var fs);
            var step = new Chown().Get(new ScriptArgs(1, new[] { "f", "5", "9" }));

            var res = step.Exec(proc);

            Assert.Equal(ScriptStepStatus.Ok, res.Status);
            var item = fs.GetElement("f");
            Assert.Equal(5u, item.User);
            Assert.Equal(9u, item.Group);
        }
    }
}
