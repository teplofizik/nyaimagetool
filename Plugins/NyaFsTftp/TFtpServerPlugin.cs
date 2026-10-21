using NyaFs.ImageFormat.Elements.Dtb.Writer;
using NyaFs.ImageFormat.Elements.Fs.Writer;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFs.ImageFormat.Types;
using System;
using System.IO;
using System.Linq;
using Tftp.Net;

namespace NyaFsTftp
{
    class TFtpServerPlugin : NyaFs.ImageFormat.Plugins.Base.ServicePlugin
    {
        int Port = 69;
        TftpServer server;
        string ServerDirectory = "./";

        public TFtpServerPlugin() : base("tftp", "TFTP service")
        {
        }

        protected override void OnStart()
        {
            server = new TftpServer(Port);
            server.OnReadRequest += Server_OnReadRequest;
            server.OnWriteRequest += Server_OnWriteRequest;
            NyaFs.Log.Ok(3, $"TFTP server is running on 0.0.0.0:{Port}");

            server.Start();
        }

        private void Server_OnWriteRequest(ITftpTransfer transfer, System.Net.EndPoint client)
        {
            var file = ResolveLocalPath(transfer.Filename);
            transfer.Start(new FileStream(file, FileMode.CreateNew));
        }

        private void Server_OnReadRequest(ITftpTransfer transfer, System.Net.EndPoint client)
        {
            var Image = TftpImageMap.Find(transfer.Filename);
            if (Image != null)
            {
                if (Image.Loaded)
                {
                    var Data = BuildLoadedImage(Image);
                    transfer.Start(new MemoryStream(Data));
                }
                else
                    transfer.Start(new FileStream(Image.Source, FileMode.Open, FileAccess.Read));

                return;
            }

            var File = ResolveLocalPath(transfer.Filename);
            transfer.Start(new FileStream(File, FileMode.Open, FileAccess.Read));
        }

        private string ResolveLocalPath(string Filename)
        {
            var Root = Path.GetFullPath(ServerDirectory);
            var Relative = (Filename ?? "").Replace('\\', '/').TrimStart('/');
            var Full = Path.GetFullPath(Path.Combine(Root, Relative));

            if ((Full != Root) &&
                !Full.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Invalid TFTP path '{Filename}'");

            return Full;
        }

        private byte[] BuildLoadedImage(TftpImageSource Source)
        {
            var Processor = getProcessor();
            if (Processor == null)
                throw new InvalidOperationException("Image processor is not available");

            switch (Source.Component)
            {
                case "kernel":
                    {
                        var Kernel = Processor.GetKernel();
                        if ((Kernel == null) || !Kernel.Loaded)
                            throw new InvalidOperationException("Kernel is not loaded");

                        return Kernel.Image;
                    }

                case "dtb":
                    {
                        var Dtb = Processor.GetDevTree();
                        if ((Dtb == null) || !Dtb.Loaded)
                            throw new InvalidOperationException("Device tree is not loaded");

                        var Writer = new FdtWriter();
                        Writer.WriteDevtree(Dtb);
                        if (!Writer.HasRawStreamData)
                            throw new InvalidOperationException("Cannot build device tree");

                        return Writer.RawStream;
                    }

                case "fs":
                    {
                        var Fs = Processor.GetFs();
                        if (Fs == null)
                            throw new InvalidOperationException("Filesystem is not loaded");

                        var Format = Source.Format;
                        if (String.IsNullOrEmpty(Format))
                            Format = (Fs.FilesystemType == FsType.Unknown)
                                ? "cpio"
                                : Fs.FilesystemType.ToString().ToLowerInvariant();

                        var Writer = CreateFsWriter(Processor, Fs, Format);
                        Writer.WriteFs(Fs);
                        if (!Writer.HasRawStreamData)
                            throw new InvalidOperationException($"Cannot build filesystem as '{Format}'");

                        return Writer.RawStream;
                    }

                default:
                    throw new InvalidOperationException($"Unknown loaded image component '{Source.Component}'");
            }
        }

        private static NyaFs.ImageFormat.Elements.Fs.Writer.Writer CreateFsWriter(
            NyaFs.Processor.ImageProcessor Processor, NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Fs, string Format)
        {
            foreach (var Plugin in Processor.Plugins.GetFilesystems())
            {
                if (Plugin.SupportWrite && Plugin.Format.Equals(Format, StringComparison.OrdinalIgnoreCase))
                {
                    var Writer = Plugin.GetFilesystemWriter(Fs);
                    if (Writer != null)
                        return Writer;
                }
            }

            switch (Format.ToLowerInvariant())
            {
                case "cpio": return new CpioFsWriter();
                case "tar": return new TarFsWriter();
                case "cramfs": return new CramFsWriter();
                case "romfs": return new RomFsWriter();
                case "squashfs": return new SquashFsWriter(Fs.SquashFsCompression);
                case "ext2": return new Ext2FsWriter(ExtSize(Fs));
                default:
                    throw new InvalidOperationException($"Unknown filesystem format '{Format}'");
            }
        }

        private static uint ExtSize(NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Fs)
        {
            ulong Size = (Fs.FsSize > 0)
                ? (ulong)Fs.FsSize
                : (ulong)Math.Max(0x800000L, Fs.GetContentSize() * 2);

            Size = (Size + 0xFFFFFUL) & ~0xFFFFFUL;
            return (uint)Math.Min(Size, UInt32.MaxValue);
        }

        protected override void OnStop()
        {
            server.Dispose();
            server = null;
            NyaFs.Log.Ok(0, $"TFTP server is stopped");
        }

        public override void Info()
        {
            NyaFs.Log.Write(0, "TFTP server");
            NyaFs.Log.Write(0, $"    port: {Port}");
            NyaFs.Log.Write(0, $"     dir: {ServerDirectory}");
            NyaFs.Log.Write(0, $" running: {((server != null) ? "yes" : "no")}");

            foreach (var Image in TftpImageMap.List())
                NyaFs.Log.Write(0, $"   image: {Image.Name} -> {Image.Description}");
        }

        public override void Usage()
        {
            base.Usage();

            NyaFs.Log.Write(0, $"  set port <portnumber> (at now portnumber is {Port})");
            NyaFs.Log.Write(0, $"  set dir  <directory>  (at now directory is '{ServerDirectory}')");
            NyaFs.Log.Write(0, $"  set image <name>=<source> (loaded:fs[:format], loaded:kernel, loaded:dtb or local file)");
            NyaFs.Log.Write(0, "  use tftpimage add/remove/list for image mappings");
        }

        public override void SetOption(string Param, string Value)
        {
            switch (Param)
            {
                case "port":
                    {
                        int Res;
                        if (Int32.TryParse(Value, out Res))
                        {
                            if ((Res > 0) && (Res < 64000))
                            {
                                Port = Res;
                                return;
                            }
                        }
                        NyaFs.Log.Error(0, $"Invalid value {Value} for param {Param}.");
                    }
                    break;
                case "dir":
                    if (System.IO.Directory.Exists(Value))
                        ServerDirectory = Value;
                    else
                        NyaFs.Log.Error(0, $"Invalid path {Value} for param {Param}. Such directory is not found.");
                    break;
                case "image":
                    {
                        int Eq = (Value ?? "").IndexOf('=');
                        if (Eq <= 0)
                        {
                            NyaFs.Log.Error(0, "Use: service tftp set image <name>=<source>");
                            return;
                        }

                        var Name = Value.Substring(0, Eq).Trim();
                        var Source = Value.Substring(Eq + 1).Trim();
                        if (!TftpImageMap.Add(Name, Source, out string Error))
                            NyaFs.Log.Error(0, Error);
                        else
                            NyaFs.Log.Write(0, $"TFTP image '{Name}' -> {Source}");
                    }
                    break;
                default:
                    NyaFs.Log.Error(0, $"Unknown option {Param}");
                    break;
            }
        }
    }
}
