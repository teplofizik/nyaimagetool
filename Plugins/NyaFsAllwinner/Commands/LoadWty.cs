using NyaFs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsAllwinner.Types;
using NyaFsAllwinner.Loader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using NyaFs.Processor.Scripting.Steps;

namespace NyaFsAllwinner.Commands
{
    class LoadWty : ScriptStepGenerator
    {
        public LoadWty() : base("loadwty")
        {
            AddConfig(new ScriptArgsConfig(0, new ScriptArgsParam[] {
                new NyaFs.Processor.Scripting.Params.FsPathScriptArgsParam()
            }));
        }

        public override ScriptStep Get(ScriptArgs Args)
        {
            return new LoadWtyScriptStep(Args.RawArgs[0]);
        }

        public class LoadWtyScriptStep : ImageImportScriptStep
        {
            private string Path;

            public LoadWtyScriptStep(string Path) : base("loadwty")
            {
                this.Path = Path;
            }

            public override ScriptStepResult Exec(ImageProcessor Processor)
            {
                // Проверим наличие локального файла, содержимое которого надо загрузить в ФС
                if (!System.IO.File.Exists(Path))
                    return new ScriptStepResult(ScriptStepStatus.Error, $"{Path} not found!");

                var Data = File.ReadAllBytes(Path);
                var Image = new WtyImage(Data);

                if (Image.Correct)
                {
                    var Files = Image.FileHeaders;

                    // File list
                    for (int i = 0; i < Files.Length; i++)
                    {
                        var Header = Files[i];

                        Log.Write(0, $"{Header.Maintype} ({Header.Subtype}): {Header.Filename}  offset: {Header.Offset:X08} img: {Header.StoredLength:X08} orig: {Header.OriginalLength:X08}");
                    }

                    var FsRes = ReadFs(Processor, Image);
                    if (FsRes.Status == ScriptStepStatus.Error)
                        return FsRes;

                    /*if (FW.Correct)
                    {

                        /*var KernelRes = ReadKernel(Processor, Image);
                        if (FsRes.Status == ScriptStepStatus.Error)
                            return KernelRes;

                        var DevtreeRes = ReadDtb(Processor, Image);
                        if (FsRes.Status == ScriptStepStatus.Error)
                            return DevtreeRes;

                        return new ScriptStepResult(ScriptStepStatus.Ok, $"Rockchip image is loaded!");
                    }*/
                    return new ScriptStepResult(ScriptStepStatus.Ok, $"Allwinner wty image is loaded!");
                }
                
                return new ScriptStepResult(ScriptStepStatus.Error, $"Allwinner wty image is invalid image!");
            }

            private ScriptStepResult ReadFs(ImageProcessor Processor, WtyImage Image)
            {
                var OldLoaded = Processor.GetFs()?.Loaded ?? false;
                var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();

                var Raw = Image.GetPartition(33);
                if (Fs != null)
                {
                    var FsReader = new WtyFsReader(Raw);

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
                else
                    return new ScriptStepResult(ScriptStepStatus.Error, $"No rootfs partition in firmware package!");
            }
        }
    }
}
