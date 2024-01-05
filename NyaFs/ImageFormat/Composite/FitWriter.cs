using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Composite
{
    class FitWriter
    {
        private readonly string Path;

        private bool KernelAvailable = false;
        private bool FsAvailable = false;
        private bool DevtreeAvailable = false;

        public FitWriter(string Path)
        {
            this.Path = Path;
        }

        public bool Write(BaseImageBlob Blob)
        {
            if (!CheckFs(Blob.GetFilesystem(0)))
                Log.Warning(0, "No filesystem!");
            else
                FsAvailable = true;

            if (!CheckKernel(Blob.GetKernel(0)))
                Log.Warning(0, "No kernel!");
            else
                KernelAvailable = true;

            if (!CheckDevTree(Blob.GetDevTree(0)))
                Log.Warning(0, "No device tree!");
            else
                DevtreeAvailable = true;

            if(!DevtreeAvailable && !KernelAvailable && !FsAvailable)
            {
                Log.Error(0, "No images available for writing");
                return false;
            }

            var FitImage = new FlattenedDeviceTree.FlattenedDeviceTree();
            FitImage.Root.AddStringValue("description", "U-Boot uImage FDT blobs");
            FitImage.Root.Nodes.Add(GetImagesNode(Blob));
            FitImage.Root.Nodes.Add(GetConfiguratiosnNode(Blob));

            var Writer = new FlattenedDeviceTree.Writer.FDTWriter(FitImage);
            var Data = Writer.GetBinary();
            System.IO.File.WriteAllBytes(Path, Data);

            return true;
        }

        private FlattenedDeviceTree.Types.Node GetConfiguratiosnNode(BaseImageBlob Blob)
        {
            var Node = new FlattenedDeviceTree.Types.Node("configurations");
            Node.AddStringValue("default", "config@default");

            var Config = new FlattenedDeviceTree.Types.Node("config@default");
            Config.AddStringValue("description", "Image package");
            if(KernelAvailable) Config.AddStringValue("kernel", "kernel@default");
            if(DevtreeAvailable) Config.AddStringValue("fdt", "fdt@default");
            if(FsAvailable) Config.AddStringValue("ramdisk", "ramdisk@default");

            Node.Nodes.Add(Config);
            return Node;
        }

        private FlattenedDeviceTree.Types.Node GetImagesNode(BaseImageBlob Blob)
        {
            var Node = new FlattenedDeviceTree.Types.Node("images");
            Node.AddUInt32Value("#address-cells", 1);

            if(KernelAvailable)
            {
                var K = Blob.GetKernel(0);
                var Data = Helper.FitHelper.GetCompressedData(K.Image, K.Info.Compression);
                var Kernel = new FlattenedDeviceTree.Types.Node("kernel@default");

                Kernel.AddStringValue("description", "Linux kernel");
                Kernel.AddRawValue("data", Data);
                Kernel.AddStringValue("type", Helper.FitHelper.GetType(K.Info.Type));
                Kernel.AddStringValue("arch", Helper.FitHelper.GetCPUArchitecture(K.Info.Architecture));
                Kernel.AddStringValue("os", Helper.FitHelper.GetOperatingSystem(K.Info.OperatingSystem));
                Kernel.AddStringValue("compression", Helper.FitHelper.GetCompression(K.Info.Compression));
                if (Helper.FitHelper.Is64BitArchitecture(K.Info.Architecture))
                {
                    Kernel.AddUInt64Value("load", K.Info.DataLoadAddress);
                    Kernel.AddUInt64Value("entry", K.Info.EntryPointAddress);
                }
                else
                { 
                    Kernel.AddUInt32Value("load", Convert.ToUInt32(K.Info.DataLoadAddress & 0xFFFFFFFFUL));
                    Kernel.AddUInt32Value("entry", Convert.ToUInt32(K.Info.EntryPointAddress & 0xFFFFFFFFUL));
                }
                Kernel.Nodes.Add(GetHashNode(Data));

                Node.Nodes.Add(Kernel);
            }

            if(DevtreeAvailable)
            {
                var DT = Blob.GetDevTree(0);
                var Raw = new FlattenedDeviceTree.Writer.FDTWriter(DT.DevTree).GetBinary();
                var Data = Helper.FitHelper.GetCompressedData(Raw, DT.Info.Compression);
                var DevTree = new FlattenedDeviceTree.Types.Node("fdt@default");

                DevTree.AddStringValue("description", "Flattened Device Tree blob");
                DevTree.AddRawValue("data", Data);
                DevTree.AddStringValue("type", Helper.FitHelper.GetType(DT.Info.Type));
                DevTree.AddStringValue("arch", Helper.FitHelper.GetCPUArchitecture(DT.Info.Architecture));
                DevTree.AddStringValue("compression", Helper.FitHelper.GetCompression(DT.Info.Compression));
                DevTree.Nodes.Add(GetHashNode(Data));

                Node.Nodes.Add(DevTree);
            }

            if(FsAvailable)
            {
                var FS = Blob.GetFilesystem(0);
                var FsWriter = Elements.Fs.Writer.Writer.GetRawFilesystemWriter(FS);
                FsWriter.WriteFs(FS);

                var Raw = FsWriter.RawStream;
                var Data = Helper.FitHelper.GetCompressedData(Raw, FS.Info.Compression);
                var RamDisk = new FlattenedDeviceTree.Types.Node("ramdisk@default");

                RamDisk.AddStringValue("description", "Ramdisk");
                RamDisk.AddRawValue("data", Data);
                RamDisk.AddStringValue("type", Helper.FitHelper.GetType(FS.Info.Type));
                RamDisk.AddStringValue("arch", Helper.FitHelper.GetCPUArchitecture(FS.Info.Architecture));
                RamDisk.AddStringValue("os", Helper.FitHelper.GetOperatingSystem(FS.Info.OperatingSystem));
                RamDisk.AddStringValue("compression", Helper.FitHelper.GetCompression(FS.Info.Compression));
                RamDisk.AddUInt32Value("load", Convert.ToUInt32(FS.Info.DataLoadAddress & 0xFFFFFFFFUL));
                RamDisk.AddUInt32Value("entry", Convert.ToUInt32(FS.Info.EntryPointAddress & 0xFFFFFFFFUL));

                RamDisk.Nodes.Add(GetHashNode(Data));

                Node.Nodes.Add(RamDisk);
            }

            return Node;
        }

        private FlattenedDeviceTree.Types.Node GetHashNode(byte[] Data)
        {
            var Hash = new FlattenedDeviceTree.Types.Node("hash@1");
            Hash.AddStringValue("algo", "sha1");
            Hash.AddRawValue("value", Helper.FitHelper.CalcSHA1Hash(Data));
            return Hash;
        }

        private bool CheckFs(Elements.Fs.LinuxFilesystem Fs)
        {
            if(Fs == null)
                return false;

            if (Fs.Info.Architecture == Types.CPU.IH_ARCH_INVALID)
            {
                Log.Write(0, "Filesystem architecture is not setted.");
                return false;
            }
            if (Fs.Info.OperatingSystem == Types.OS.IH_OS_INVALID)
            {
                Log.Write(0, "Filesystem operating system is not setted.");
                return false;
            }
            if (Fs.Info.Type != Types.ImageType.IH_TYPE_RAMDISK)
            {
                Log.Write(0, "Filesystem image type must be 'ramdisk'");
                return false;
            }

            return true;
        }

        private bool CheckKernel(Elements.Kernel.LinuxKernel Kernel)
        {
            if (Kernel == null)
                return false;

            if (Kernel.Info.Architecture == Types.CPU.IH_ARCH_INVALID)
            {
                Log.Write(0, "Filesystem architecture is not setted.");
                return false;
            }
            if (Kernel.Info.OperatingSystem == Types.OS.IH_OS_INVALID)
            {
                Log.Write(0, "Filesystem operating system is not setted.");
                return false;
            }
            if (Kernel.Info.Type != Types.ImageType.IH_TYPE_KERNEL)
            {
                Log.Write(0, "Kernel image type must be 'kernel'");
                return false;
            }

            return true;
        }

        private bool CheckDevTree(Elements.Dtb.DeviceTree DevTree)
        {
            if (DevTree == null)
                return false;

            if (DevTree.Info.Architecture == Types.CPU.IH_ARCH_INVALID)
            {
                Log.Write(0, "Device tree architecture is not setted.");
                return false;
            }
            if (DevTree.Info.Type != Types.ImageType.IH_TYPE_FLATDT)
            {
                Log.Write(0, "Kernel image type must be 'flat_dt'");
                return false;
            }
            return true;
        }
    }
}
