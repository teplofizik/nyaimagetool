using NyaFs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Steps;
using NyaFsBmu.Loader;
using NyaFsBmu.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace NyaFsBmu.Commands
{
    class LoadBmu : ScriptStepGenerator
    {
        public LoadBmu() : base("loadbmu")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new LoadBmuScriptStep(Args.RawArgs[0]);
        }

        public class LoadBmuScriptStep : ImageImportScriptStep
        {
            private string Path;

            public LoadBmuScriptStep(string Path) : base("loadbmu")
            {
                this.Path = Path;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                // Проверим наличие локального файла, содержимое которого надо загрузить в ФС
                if (!System.IO.File.Exists(Path))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found!");

                var Data = File.ReadAllBytes(Path);
                var Image = new BmuImage(Data);

                if (Image.Correct)
                {
                    var FsRes = ReadFs(Processor, Image);
                    if (FsRes.Status == ScriptStepStatus.Error)
                        return FsRes;

                    var KernelRes = ReadKernel(Processor, Image);
                    if (KernelRes.Status == ScriptStepStatus.Error)
                        return KernelRes;

                    var DevtreeRes = ReadDtb(Processor, Image);
                    if (DevtreeRes.Status == ScriptStepStatus.Error)
                        return DevtreeRes;

                    return new ScriptStepResult(ScriptStepStatus.Ok, $"BMU is loaded!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"BMU is invalid image!");
            }

            private ScriptStepResult ReadFs(ImageProcessor Processor, BmuImage Data)
            {
                var OldLoaded = Processor.GetFs()?.Loaded ?? false;
                var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
                var FsReader = new NyaFsBmu.Loader.BmuFsReader(Data);

                FsReader.ReadToFs(Fs);
                if (Fs.Loaded)
                {
                    Processor.SetFs(Fs);
                    AssumeAutoImageParams(Processor);
                    //if (Fs.FilesystemType != ImageFormat.Types.FsType.Cpio)
                    //{
                    // Fs.FilesystemType = ImageFormat.Types.FsType.Cpio;
                    // Log.Warning(0, "Filesystem is switched to cpio as only supported for packing.");
                    // if (Fs.Info.Compression == ImageFormat.Types.CompressionType.IH_COMP_NONE)
                    // {
                    //     Fs.Info.Compression = ImageFormat.Types.CompressionType.IH_COMP_GZIP;
                    //     Log.Warning(0, "Compression is switched to gzip.");
                    // }
                    //}
                    NyaFs.ImageFormat.Helper.LogHelper.RamfsInfo(Fs);

                    if (OldLoaded)
                        return new ScriptStepResult(ScriptStepStatus.Warning, $"BMU image is loaded as filesystem! Old filesystem is replaced by this.");
                    else
                        return new ScriptStepResult(ScriptStepStatus.Ok, $"BMU is loaded as filesystem!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown fs format!");
            }

            private ScriptStepResult ReadDtb(ImageProcessor Processor, BmuImage Data)
            {
                var OldLoaded = Processor.GetDevTree()?.Loaded ?? false;
                var Dtb = new NyaFs.ImageFormat.Elements.Dtb.DeviceTree();
                var Reader = new NyaFsBmu.Loader.BmuDevtreeReader(Data);
                if (Reader != null)
                {
                    Reader.ReadToDevTree(Dtb);
                    if (Dtb.Loaded)
                    {
                        Processor.SetDeviceTree(Dtb);
                        AssumeAutoImageParams(Processor);
                        NyaFs.ImageFormat.Helper.LogHelper.DevtreeInfo(Dtb);
                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Device tree is loaded from BMU file!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Device tree is not loaded!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown devtree format!");
            }

            private ScriptStepResult ReadKernel(ImageProcessor Processor, BmuImage Data)
            {
                var OldLoaded = Processor.GetKernel()?.Loaded ?? false;
                var Kernel = new NyaFs.ImageFormat.Elements.Kernel.LinuxKernel();
                var Reader = new NyaFsBmu.Loader.BmuKernelReader(Data);
                if (Reader != null)
                {
                    Reader.ReadToKernel(Kernel);
                    if (Kernel.Loaded)
                    {
                        Processor.SetKernel(Kernel);
                        AssumeAutoKernelParams(Kernel);
                        AssumeAutoImageParams(Processor);
                        NyaFs.ImageFormat.Helper.LogHelper.KernelInfo(Kernel);

                        if (OldLoaded)
                            return new ScriptStepResult(ScriptStepStatus.Warning, $"BMU kernel image is loaded as kernel! Old kernel is replaced by this.");
                        else
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"BMU kernel image is loaded as kernel!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Kernel image file is not loaded!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown kernel format!");
            }
        }
    }
}
