using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Kernel.Reader
{
    class FitReader : Reader
    {
        bool Loaded = false;

        FlattenedDeviceTree.FlattenedDeviceTree Fit;

        FlattenedDeviceTree.Types.Node KernelNode = null;

        // https://github.com/siemens/u-boot/blob/master/common/image.c
        public FitReader(string Filename)
        {
            Fit = new NyaFs.FlattenedDeviceTree.Reader.FDTReader(Filename).Read();

            if (Fit.Root.Nodes.Count == 0)
            {
                Log.Error(0, $"Could not load FIT image from file {Filename}");
                return;
            }

            var Configurations = Fit.Get("configurations");
            if (Configurations == null)
            {
                Log.Error(0, $"Invalid FIT image {Filename}: no 'configuration' node.");
                return;
            }
            var DefaultConfig = Configurations.GetStringValue("default");

            if (DefaultConfig == null)
            {
                Log.Error(0, $"Invalid FIT image {Filename}: no 'default' parameter in 'configuration' node.");
                return;
            }

            var Configuration = Fit.Get("configurations/" + DefaultConfig);

            if (Configuration == null)
            {
                Log.Error(0, $"Invalid FIT image {Filename}: no 'configuration/{DefaultConfig}' node.");
                return;
            }

            // kernel fdt ramdisk
            var KernelNodeName = Configuration.GetStringValue("kernel");
            if (KernelNodeName == null)
            {
                Log.Error(0, $"Invalid FIT image {Filename}: no 'kernel' parameter in 'configuration/{DefaultConfig}' node.");
                return;
            }

            KernelNode = Fit.Get($"images/{KernelNodeName}");
            if (KernelNode == null)
            {
                Log.Error(0, $"Invalid FIT image {Filename}: no '{KernelNodeName}' node.");
                return;
            }

            Loaded = true;
        }

        /// <summary>
        /// Читаем в дерево устройств из внешнего источника
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToKernel(LinuxKernel Dst)
        {
            if (!Loaded) return;

            var ImgType = KernelNode.GetStringValue("type");
            if (ImgType == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'type' parameter in loaded ramdisk node.");
                return;
            }


            var Os = KernelNode.GetStringValue("os");
            if (Os == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'os' parameter in loaded ramdisk node.");
                return;
            }

            var Arch = KernelNode.GetStringValue("arch");
            if (Arch == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'arch' parameter in loaded ramdisk node.");
                return;
            }

            var Compression = KernelNode.GetStringValue("compression");
            if (Compression == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'compression' parameter in loaded ramdisk node.");
                return;
            }

            var Load = KernelNode.GetValue("load");
            if (Load == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'load' parameter in loaded ramdisk node.");
                return;
            }

            var Entry = KernelNode.GetValue("entry");
            if (Entry == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'entry' parameter in loaded ramdisk node.");
                return;
            }

            UInt32 EntryAddress = Entry.ReadUInt32BE(0);
            UInt32 LoadAddress  = Load.ReadUInt32BE(0);

            var Data = KernelNode.GetValue("data");

            if (Helper.FitHelper.CheckHash(Data, KernelNode.GetNode("hash@1")))
            {
                var Kernel = Helper.FitHelper.GetDecompressedData(Data, Compression);
                Dst.Info.Architecture = Helper.FitHelper.GetCPUArchitecture(Arch);
                Dst.Info.Type = Helper.FitHelper.GetType(ImgType);
                Dst.Info.OperatingSystem = Helper.FitHelper.GetOperatingSystem(Os);
                Dst.Info.DataLoadAddress = LoadAddress;
                Dst.Info.EntryPointAddress = EntryAddress;
                Dst.Info.Compression = Helper.FitHelper.GetCompression(Compression);
                Dst.Image = Kernel;

                Helper.LogHelper.KernelInfo(Dst);
            }
            else
            {
                Log.Error(0, $"Invalid FIT image: hash is not equal.");
                return;
            }
        }
    }
}
