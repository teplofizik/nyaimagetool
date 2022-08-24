using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Dtb.Reader
{
    class FitReader : Reader
    {
        bool Loaded = false;

        FlattenedDeviceTree.FlattenedDeviceTree Fit;

        FlattenedDeviceTree.Types.Node DevtreeNode = null;

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
            var DevTreeNodeName = Configuration.GetStringValue("fdt");
            if (DevTreeNodeName == null)
            {
                Log.Error(0, $"Invalid FIT image {Filename}: no 'fdt' parameter in 'configuration/{DefaultConfig}' node.");
                return;
            }

            DevtreeNode = Fit.Get($"images/{DevTreeNodeName}");
            if (DevtreeNode == null)
            {
                Log.Error(0, $"Invalid FIT image {Filename}: no '{DevTreeNodeName}' node.");
                return;
            }

            Loaded = true;
        }

        /// <summary>
        /// Читаем в дерево устройств из внешнего источника
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToDevTree(DeviceTree Dst)
        {
            if (!Loaded) return;

            var ImgType = DevtreeNode.GetStringValue("type");
            if (ImgType == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'type' parameter in loaded ramdisk node.");
                return;
            }

            var Arch = DevtreeNode.GetStringValue("arch");
            if (Arch == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'arch' parameter in loaded ramdisk node.");
                return;
            }

            var Compression = DevtreeNode.GetStringValue("compression");
            if (Compression == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'compression' parameter in loaded ramdisk node.");
                return;
            }

            var Data = DevtreeNode.GetValue("data");

            if (Helper.FitHelper.CheckHash(Data, DevtreeNode.GetNode("hash@1")))
            {
                var Dtb = Helper.FitHelper.GetDecompressedData(Data, Compression);
                Dst.Info.Architecture = Helper.FitHelper.GetCPUArchitecture(Arch);
                Dst.Info.Type = Helper.FitHelper.GetType(ImgType);
                Dst.DevTree = new FlattenedDeviceTree.Reader.FDTReader(Dtb).Read();

                Helper.LogHelper.DevtreeInfo(Dst);
            }
            else
            {
                Log.Error(0, $"Invalid FIT image: hash is not equal.");
                return;
            }
        }
    }
}
