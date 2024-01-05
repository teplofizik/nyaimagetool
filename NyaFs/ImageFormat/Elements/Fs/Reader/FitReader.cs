using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class FitReader : Reader
    {
        bool Loaded = false;

        FlattenedDeviceTree.FlattenedDeviceTree Fit;
        FlattenedDeviceTree.Types.Node RamdiskNode = null;

        // https://github.com/siemens/u-boot/blob/master/common/image.c
        public FitReader(string Filename, string Config)
        {
            Fit = new NyaFs.FlattenedDeviceTree.Reader.FDTReader(Filename).Read();

            if(Fit.Root.Nodes.Count == 0)
            {
                Log.Error(0, $"Could not load FIT image from file {Filename}");
                return;
            }

            var Configurations = Fit.Get("configurations");
            if(Configurations == null)
            {
                Log.Error(0, $"Invalid FIT image {Filename}: no 'configuration' node.");
                return;
            }
            var DefaultConfig = (Config != null) ? Config : Configurations.GetStringValue("default");

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
            var RamdiskNodeName = Configuration.GetStringValue("ramdisk");
            if (RamdiskNodeName == null)
            {
                Log.Error(0, $"Invalid FIT image {Filename}: no 'ramdisk' parameter in 'configuration/{DefaultConfig}' node.");
                return;
            }

            RamdiskNode = Fit.Get($"images/{RamdiskNodeName}");
            if (RamdiskNode == null)
            {
                Log.Error(0, $"Invalid FIT image {Filename}: no '{RamdiskNodeName}' node.");
                return;
            }

            Loaded = true;
        }

        /// <summary>
        /// Читаем в файловую систему из cpio-файла
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(LinuxFilesystem Dst)
        {
            if (!Loaded) return;

            var ImgType = RamdiskNode.GetStringValue("type");
            if (ImgType == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'type' parameter in loaded ramdisk node.");
                return;
            }

            var Arch = RamdiskNode.GetStringValue("arch");
            if (Arch == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'arch' parameter in loaded ramdisk node.");
                return;
            }

            var Os = RamdiskNode.GetStringValue("os");
            if (Os == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'os' parameter in loaded ramdisk node.");
                return;
            }

            var CompressedData = RamdiskNode.GetValue("data");
            if(CompressedData == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'data' parameter in loaded ramdisk node.");
                return;
            }

            var Compression = RamdiskNode.GetStringValue("compression");
            if (Compression == null)
            {
                Log.Error(0, $"Invalid FIT image: no 'compression' parameter in loaded ramdisk node.");
                return;
            }

            var HashNode = Helper.FitHelper.GetHashNode(RamdiskNode);
            if (HashNode != null)
            {
                if (Helper.FitHelper.CheckHash(CompressedData, HashNode))
                {
                    var Data = Helper.FitHelper.GetDecompressedData(CompressedData, Compression);

                    Dst.Info.Architecture = Helper.FitHelper.GetCPUArchitecture(Arch);
                    Dst.Info.OperatingSystem = Helper.FitHelper.GetOperatingSystem(Os);
                    Dst.Info.Name = null;
                    Dst.Info.DataLoadAddress = 0;
                    Dst.Info.EntryPointAddress = 0;
                    Dst.Info.Type = Helper.FitHelper.GetType(ImgType);
                    Dst.Info.Compression = Helper.FitHelper.GetCompression(Compression);

                    if (!DetectAndRead(Dst, Data))
                        Log.Error(0, "Unsupported filesystem...");
                }
                else
                {
                    Log.Error(0, $"Invalid FIT image: hash is not equal.");
                    return;
                }
            }
            else
            {
                Log.Warning(0, $"No hash node in ramdisk image node!");

                var Data = Helper.FitHelper.GetDecompressedData(CompressedData, Compression);

                Dst.Info.Architecture = Helper.FitHelper.GetCPUArchitecture(Arch);
                Dst.Info.OperatingSystem = Helper.FitHelper.GetOperatingSystem(Os);
                Dst.Info.Name = null;
                Dst.Info.DataLoadAddress = 0;
                Dst.Info.EntryPointAddress = 0;
                Dst.Info.Type = Helper.FitHelper.GetType(ImgType);
                Dst.Info.Compression = Helper.FitHelper.GetCompression(Compression);

                if (!DetectAndRead(Dst, Data))
                    Log.Error(0, "Unsupported filesystem...");
            }
        }



    }
}
