using NyaFs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Steps;
using NyaFsInnosilicon.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace NyaFsInnosilicon.Commands
{
    class LoadSwu : ScriptStepGenerator
    {
        public LoadSwu() : base("loadswu")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new LoadSwuScriptStep(Args.RawArgs[0]);
        }

        public class LoadSwuScriptStep : ImageImportScriptStep
        {
            private string Path;

            public LoadSwuScriptStep(string Path) : base("loadswu")
            {
                this.Path = Path;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                // Проверим наличие локального файла, содержимое которого надо загрузить в ФС
                if (!System.IO.File.Exists(Path))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found!");

                var Data = File.ReadAllBytes(Path);
                var Fw = new InnoFirmwareUpdate(Data);
                if (Fw.Correct)
                {
                    int Count = 0;

                    var DevtreeRes = ReadDtb(Processor, Fw);
                    if (DevtreeRes.Status == ScriptStepStatus.Error)
                        Log.Error(0, DevtreeRes.Text);
                    else
                        Count++;

                    var KernelRes = ReadKernel(Processor, Fw);
                    if (KernelRes.Status == ScriptStepStatus.Error)
                        Log.Error(0, KernelRes.Text);
                    else
                        Count++;

                    var FsRes = ReadFs(Processor, Fw);
                    if (FsRes.Status == ScriptStepStatus.Error)
                        Log.Error(0, FsRes.Text);
                    else
                        Count++;

                    if (Count == 3)
                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Innosilicon image is loaded!");
                    else if(Count > 0)
                        return new ScriptStepResult(ScriptStepStatus.Warning, $"Innosilicon image is partially loaded!");
                    else
                       return new ScriptStepResult(ScriptStepStatus.Error, $"Innosilicon image is invalid image!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Innosilicon image is invalid image!");
            }

            private ScriptStepResult ReadFs(ImageProcessor Processor, InnoFirmwareUpdate Data)
            {
                var OldLoaded = Processor.GetFs()?.Loaded ?? false;
                var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
                var FsReader = new NyaFsInnosilicon.Loader.SwuFsReader(Data.Rootfs);

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

            private ScriptStepResult ReadDtb(ImageProcessor Processor, InnoFirmwareUpdate Data)
            {
                var OldLoaded = Processor.GetDevTree()?.Loaded ?? false;
                var Dtb = new NyaFs.ImageFormat.Elements.Dtb.DeviceTree();
                var Reader = new NyaFsInnosilicon.Loader.SwuDevtreeReader(Data);
                if (Reader != null)
                {
                    Reader.ReadToDevTree(Dtb);
                    if (Dtb.Loaded)
                    {
                        Processor.SetDeviceTree(Dtb);
                        AssumeAutoImageParams(Processor);
                        NyaFs.ImageFormat.Helper.LogHelper.DevtreeInfo(Dtb);
                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Device tree is loaded from Innosilicon file!");
                    }
                    else
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Device tree is not loaded!");
                }
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Unknown devtree format!");
            }

            private ScriptStepResult ReadKernel(ImageProcessor Processor, InnoFirmwareUpdate Data)
            {
                var OldLoaded = Processor.GetKernel()?.Loaded ?? false;
                var Kernel = new NyaFs.ImageFormat.Elements.Kernel.LinuxKernel();
                var Reader = new NyaFsInnosilicon.Loader.SwuKernelReader(Data);
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
                            return new ScriptStepResult(ScriptStepStatus.Warning, $"Innosilicon kernel image is loaded as kernel! Old kernel is replaced by this.");
                        else
                            return new ScriptStepResult(ScriptStepStatus.Ok, $"Innosilicon kernel image is loaded as kernel!");
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
