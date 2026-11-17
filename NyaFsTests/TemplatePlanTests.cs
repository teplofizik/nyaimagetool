using System;
using System.IO;
using System.Linq;
using System.Text;
using NyaFs.Filesystem.Cpio;
using NyaFs.Filesystem.Cpio.Types.Nodes;
using NyaFs.ImageFormat;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsRockchip;
using NyaFsSwupdate;
using NyaFsSwupdate.Types;
using Xunit;

namespace NyaFsTests
{
    public class TemplatePlanTests
    {
        private static ImageProcessor Processor()
        {
            var Proc = new ImageProcessor(new ScriptParser(new ScriptBaseAll()), new string[0]);
            Proc.Plugins.Load(new NyaFsSwupdateCompositeImagePlugin());
            Proc.Plugins.Load(new NyaFsRockchipCompositeImagePlugin());
            return Proc;
        }

        private static void Run(ImageProcessor Proc, params string[] Lines) =>
            Proc.Process(new ScriptParser(new ScriptBaseAll()).ParseScript("t", Lines));

        private static byte[] BuildSwuTemplate(byte[] Payload)
        {
            var Hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Payload)).ToLowerInvariant();
            var Description = "software =\n{\n  version = \"1.0\";\n  images: (\n  {\n" +
                "    filename = \"payload.bin\";\n    sha256 = \"" + Hash + "\";\n  });\n};\n";

            var Builder = new CpioFsBuilder("070702");

            var DescriptionNode = new CpioFile("sw-description", 0x1A4, 0, 0, Encoding.UTF8.GetBytes(Description));
            DescriptionNode.SetMagic("070702");
            DescriptionNode.UpdateCheck();
            Builder.Raw(DescriptionNode);

            var PayloadNode = new CpioFile("payload.bin", 0x1A4, 0, 0, Payload);
            PayloadNode.SetMagic("070702");
            PayloadNode.UpdateCheck();
            Builder.Raw(PayloadNode);

            return Builder.GetFilesystemImage();
        }

        [Fact]
        public void Plan_Swu_Template_Replaces_Entry_And_Hashes()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "swu-plan-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Dir);

            var Template = Path.Combine(Dir, "template.swu");
            var Payload = Path.Combine(Dir, "payload.bin");
            var Extra = Path.Combine(Dir, "extra.bin");
            var Layout = Path.Combine(Dir, "swu.yaml");
            var Out = Path.Combine(Dir, "out.swu");

            var OldData = Encoding.UTF8.GetBytes("old payload");
            var NewData = Encoding.UTF8.GetBytes("new payload from layout");

            try
            {
                File.WriteAllBytes(Template, BuildSwuTemplate(OldData));
                File.WriteAllBytes(Payload, NewData);
                File.WriteAllBytes(Extra, Encoding.UTF8.GetBytes("extra file"));

                File.WriteAllText(Layout,
                    "schema: 1\nformat: swu\n" +
                    "options: { template: template.swu }\n" +
                    "children:\n" +
                    "  - { name: payload.bin, kind: raw, content: { file: payload.bin } }\n" +
                    "  - { name: extra.bin, kind: raw, content: { file: extra.bin } }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc,
                    "mkimage swu \"" + Layout + "\"",
                    "store \"" + Out + "\" all swu");

                var Archive = new SwuArchive(File.ReadAllBytes(Out));
                Assert.True(Archive.Correct);
                Assert.Equal(NewData, Archive.Read("payload.bin"));

                var Description = Encoding.UTF8.GetString(Archive.Read("sw-description"));
                var NewHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(NewData)).ToLowerInvariant();
                Assert.Contains("sha256 = \"" + NewHash + "\"", Description);
                Assert.Equal(Encoding.UTF8.GetBytes("extra file"), Archive.Read("extra.bin"));
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }

        [Fact]
        public void Plan_Rkfw_Template_Replaces_Partition()
        {
            var Root = Environment.GetEnvironmentVariable("NYAIMAGETOOL_IMAGES");
            if (string.IsNullOrEmpty(Root)) Root = @"F:\TEMP\Images";
            var Template = Path.Combine(Root, "vendor-raw", "update.img");
            if (!File.Exists(Template))
                return;

            var Dir = Path.Combine(Path.GetTempPath(), "rkfw-plan-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Dir);

            var Payload = Path.Combine(Dir, "payload.bin");
            var Layout = Path.Combine(Dir, "rkfw.yaml");
            var Out = Path.Combine(Dir, "out.img");

            var NewData = Encoding.ASCII.GetBytes("rkfw layout payload");

            try
            {
                File.WriteAllBytes(Payload, NewData);

                File.WriteAllText(Layout,
                    "schema: 1\nformat: rkfw\n" +
                    "options: { template: \"" + Template.Replace("\\", "/") + "\" }\n" +
                    "children:\n" +
                    "  - { name: rootfs, kind: raw, content: { file: payload.bin } }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc,
                    "mkimage rkfw \"" + Layout + "\"",
                    "store \"" + Out + "\" all rkfw");

                var Data = File.ReadAllBytes(Out);
                Assert.Equal((byte)'R', Data[0]);
                Assert.Equal((byte)'K', Data[1]);
                Assert.Equal((byte)'F', Data[2]);
                Assert.Equal((byte)'W', Data[3]);

                var Plugin = new NyaFsRockchipCompositeImagePlugin();
                Assert.True(Plugin.DetectImage(new ImageData(Data)));

                var Images = Plugin.GetImages(new ImageData(Data));
                var Rootfs = Images.FirstOrDefault(I => string.Equals(I.Name, "rootfs", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(Rootfs);
                Assert.Equal(NewData, Rootfs.Content);
                Assert.Contains(Images, I => I.Name == "MiniLoaderAll.bin");
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }
    }
}
