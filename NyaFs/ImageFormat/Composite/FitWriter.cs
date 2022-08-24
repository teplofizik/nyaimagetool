using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Composite
{
    class FitWriter
    {
        private readonly string Path;

        public FitWriter(string Path)
        {
            this.Path = Path;
        }

        public bool Write(BaseImageBlob Blob)
        {
            if(!CheckFs(Blob.GetFilesystem(0)))
            {
                Log.Write(0, "There is required to set filesystem parameters");
                return false;
            }

            if (!CheckKernel(Blob.GetKernel(0)))
            {
                Log.Write(0, "There is required to set filesystem parameters");
                return false;
            }

            if (!CheckDevTree(Blob.GetDevTree(0)))
            {
                Log.Write(0, "There is required to set device tree parameters");
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
            Config.AddStringValue("kernel", "kernel@default");
            Config.AddStringValue("fdt", "fdt@default");
            Config.AddStringValue("ramdisk", "ramdisk@default");

            Node.Nodes.Add(Config);
            return Node;
        }

        private FlattenedDeviceTree.Types.Node GetImagesNode(BaseImageBlob Blob)
        {
            var Node = new FlattenedDeviceTree.Types.Node("images");
            Node.AddUInt32Value("#address-cells", 1);

            {
                var K = Blob.GetKernel(0);
                var Data = Compressors.Gzip.CompressWithHeader(K.Image);
                var Kernel = new FlattenedDeviceTree.Types.Node("kernel@default");

                Kernel.AddStringValue("description", "Linux kernel");
                Kernel.AddRawValue("data", Data);
                Kernel.AddStringValue("type", Helper.FitHelper.GetType(K.Info.Type));
                Kernel.AddStringValue("arch", Helper.FitHelper.GetCPUArchitecture(K.Info.Architecture));
                Kernel.AddStringValue("os", Helper.FitHelper.GetOperatingSystem(K.Info.OperatingSystem));
                Kernel.AddStringValue("compression", "gzip");
                Kernel.AddUInt32Value("load", K.Info.DataLoadAddress);
                Kernel.AddUInt32Value("entry", K.Info.EntryPointAddress);
                Kernel.Nodes.Add(GetHashNode(Data));

                Node.Nodes.Add(Kernel);
            }

            {
                var DT = Blob.GetDevTree(0);
                var Data = new FlattenedDeviceTree.Writer.FDTWriter(DT.DevTree).GetBinary();
                var DevTree = new FlattenedDeviceTree.Types.Node("fdt@default");

                DevTree.AddStringValue("description", "Flattened Device Tree blob");
                DevTree.AddRawValue("data", Data);
                DevTree.AddStringValue("type", Helper.FitHelper.GetType(DT.Info.Type));
                DevTree.AddStringValue("arch", Helper.FitHelper.GetCPUArchitecture(DT.Info.Architecture));
                DevTree.AddStringValue("compression", "none");
                DevTree.Nodes.Add(GetHashNode(Data));

                Node.Nodes.Add(DevTree);
            }

            {
                var FS = Blob.GetFilesystem(0);
                var Writer = new ImageFormat.Elements.Fs.Writer.GzCpioWriter();
                Writer.WriteFs(FS);
                var Data = Writer.RawStream;
                var RamDisk = new FlattenedDeviceTree.Types.Node("ramdisk@default");

                RamDisk.AddStringValue("description", "Ramdisk");
                RamDisk.AddRawValue("data", Data);
                RamDisk.AddStringValue("type", Helper.FitHelper.GetType(FS.Info.Type));
                RamDisk.AddStringValue("arch", Helper.FitHelper.GetCPUArchitecture(FS.Info.Architecture));
                RamDisk.AddStringValue("os", Helper.FitHelper.GetOperatingSystem(FS.Info.OperatingSystem));
                RamDisk.AddStringValue("compression", "gzip");
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

        private bool CheckFs(Elements.Fs.Filesystem Fs)
        {
            if(Fs.Info.Architecture == Types.CPU.IH_ARCH_INVALID)
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
