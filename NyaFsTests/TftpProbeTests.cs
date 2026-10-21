using NyaFs.ImageFormat.Elements.Kernel;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.IO;
using System.Text;
using System.Threading;
using NyaFsTftp;
using Tftp.Net;
using Xunit;
using FileItem = NyaFs.Filesystem.Universal.Items.File;

namespace NyaFsTests
{
    public class TftpProbeTests
    {
        [Fact]
        public void TftpServer_ServesLoadedAndLocalImages()
        {
            TftpImageMap.Clear();

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Processor = new ImageProcessor(Parser);
            var Fs = Processor.GetFs(true);
            Fs.Fs.Root.Items.Add(new FileItem("hello.txt", 0, 0, 0x1A4, Encoding.ASCII.GetBytes("hello")));
            Processor.SetKernel(new LinuxKernel { Image = Encoding.ASCII.GetBytes("kernel") });

            var Plugin = new NyaFsTftp.TFtpServerPlugin();
            Plugin.SetOption("port", "22069");
            Plugin.SetOption("dir", Path.GetTempPath());
            Plugin.Run(Processor);

            var Local = Path.Combine(Path.GetTempPath(), "nyafs_tftp_" + Guid.NewGuid().ToString("N") + ".bin");

            try
            {
                File.WriteAllBytes(Local, Encoding.ASCII.GetBytes("local file"));

                Assert.True(TftpImageMap.Add("rootfs.cpio", "loaded:fs:cpio", out string Error), Error);
                Assert.True(TftpImageMap.Add("local.bin", Local, out Error), Error);
                Assert.True(TftpImageMap.Add("zImage", "loaded:kernel", out Error), Error);

                Thread.Sleep(500);

                var Cpio = Download("rootfs.cpio", 22069);
                Assert.True(Cpio.Length > 0);
                Assert.Equal("070701", Encoding.ASCII.GetString(Cpio, 0, 6));

                var LocalData = Download("local.bin", 22069);
                Assert.Equal("local file", Encoding.ASCII.GetString(LocalData));

                var Kernel = Download("zImage", 22069);
                Assert.Equal("kernel", Encoding.ASCII.GetString(Kernel));
            }
            finally
            {
                Plugin.Stop();
                TftpImageMap.Clear();

                if (File.Exists(Local))
                    File.Delete(Local);
            }
        }

        private static byte[] Download(string Name, int Port)
        {
            var Client = new TftpClient("127.0.0.1", Port);
            var Transfer = Client.Download(Name);
            var Done = new AutoResetEvent(false);
            var Ok = false;
            string Error = null;

            Transfer.OnFinished += (T) =>
            {
                Ok = true;
                Done.Set();
            };

            Transfer.OnError += (T, E) =>
            {
                Error = E.ToString();
                Done.Set();
            };

            using (var Output = new MemoryStream())
            {
                Transfer.Start(Output);
                Assert.True(Done.WaitOne(10000), "TFTP transfer timed out");
                Assert.True(Ok, Error);
                return Output.ToArray();
            }
        }

        private static uint ReadUInt32BE(byte[] Data, int Offset) =>
            ((uint)Data[Offset] << 24) | ((uint)Data[Offset + 1] << 16) |
            ((uint)Data[Offset + 2] << 8) | Data[Offset + 3];
    }
}
